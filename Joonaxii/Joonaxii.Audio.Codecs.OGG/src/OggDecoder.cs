using Joonaxii.Collections;
using Joonaxii.Data.Coding;
using System;
using System.IO;

namespace Joonaxii.Audio.Codecs.OGG
{
    public class OggDecoder : AudioDecoderBase
    {
        public override byte BitDepth => 16;
        public override uint SampleRate => _idHeader.sampleRate;
        public override uint Channels => _idHeader.audioChannels;

        private PinnableList<OggPage> _pages;
        private IdentificationHeader _idHeader;

        public OggDecoder(Stream stream) : this(stream, false) { }
        public OggDecoder(Stream stream, bool leaveOpen) : base(stream, leaveOpen) 
        {
            _pages = new PinnableList<OggPage>();
        }

        public override AudioDecodeResult Decode()
        {
            long pos = _stream.Position;
            using(BinaryReader br = new BinaryReader(_stream, System.Text.Encoding.UTF8, true))
            {
                if(_pages.Count < 1)
                {
                    ReadAllPages(br);
                }
            }
            return _pages.Count < 1 ? AudioDecodeResult.InvalidFormat : AudioDecodeResult.Success;
        }

        public override void LoadGeneralInformation(long pos)
        {
            if (_pages.Count > 0) { return; }

            long posST = _stream.Position;
            _stream.Seek(pos, SeekOrigin.Begin);
            using (BinaryReader br = new BinaryReader(_stream, System.Text.Encoding.UTF8, true))
            {
                ReadAllPages(br);
            }
            _stream.Seek(posST, SeekOrigin.Begin);
        }

        public override int GetDataCRC(long pos)
        {
            PinnableList<uint> crcList = new PinnableList<uint>(16);
            if (_pages.Count < 1) 
            {
                long posST = _stream.Position;
                _stream.Seek(pos, SeekOrigin.Begin);
                using (BinaryReader br = new BinaryReader(_stream, System.Text.Encoding.UTF8, true))
                {
                    ReadAllPages(br);
                }
                _stream.Seek(posST, SeekOrigin.Begin);

            }

            foreach (var item in _pages)
            {
                crcList.Add(item.checksum);
            }

            int crc;
            unsafe
            {
                byte* b = (byte*)crcList.Pin();
                crc = (int)CRC.Calculate(b, 0, crcList.Count * sizeof(uint));
                crcList.UnPin();
            }
            return crc;
        }

        private CommonHeaderType SkipCommonHeader()
        {
            CommonHeaderType type = (CommonHeaderType)_stream.ReadByte();
            _stream.Seek(6, SeekOrigin.Current);
            return type;
        }

        private void ReadAllPages(BinaryReader br)
        {
            while (_stream.Position < _stream.Length)
            {
                long pos = _stream.Position;
                OggPattern patt = (OggPattern)br.ReadInt32();
                switch (patt)
                {
                    default:
                        System.Diagnostics.Debug.Print($"Unknown Ogg Pattern: 0x{Convert.ToString((int)patt, 16).PadLeft(8, '0')}");
                        _stream.Seek(-3, SeekOrigin.Current);
                        break;
                    case OggPattern.OggS:
                        OggPage page = new OggPage(pos, patt, br);
                        _pages.Add(page);

                        if(page.pageSegments == 1)
                        {
                            long posA = _stream.Position;
                            int len = _stream.ReadByte();
        
                            if(len > 6)
                            {
                                var hdr = SkipCommonHeader();
                                switch (hdr)
                                {
                                    case CommonHeaderType.IDHeader:
                                        _idHeader = new IdentificationHeader(br);
                                        System.Diagnostics.Debug.Print($"Found ID Header!\n -Sample Rate: {_idHeader.sampleRate}\n -Audio Channels: {_idHeader.audioChannels}");
                                        break;
                                    case CommonHeaderType.CommentHeader:
                                        System.Diagnostics.Debug.Print($"Found Comment Header!");
                                        break;
                                    case CommonHeaderType.SetupHeader:
                                        System.Diagnostics.Debug.Print($"Found Setup Header!");
                                        break;
                                }
                            }                   
                            _stream.Seek(posA, SeekOrigin.Begin);
                        }

                        page.SkipSegmentTable(_stream);
                        break;
                }
            }
        }

        protected override void Dispose(bool disposed)
        {
            base.Dispose(disposed);
            _pages.Clear();
        }
    }
}
