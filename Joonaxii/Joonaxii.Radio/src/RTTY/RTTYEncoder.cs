using Joonaxii.Audio;
using Joonaxii.Data;
using Joonaxii.Data.Coding;
using System;
using System.Text;

namespace Joonaxii.Radio.RTTY
{
    public class RTTYEncoder : CodecBase
    {
        public const double DEFAULT_CENTER_FREQUENCY = 1000.0;

        private AudioEncoderBase _audioEnc;
        private ToneGenerator _toneGen;
        private bool _leaveOpen;

        private bool _isSymbol = true;
        private double _rate;
        private double _startFreq;
        private double _endFreq;

        private double _ratePerBit;

        private double _volume = 0.75;
        private ProgressData _prgData;

        public RTTYEncoder(AudioEncoderBase encoder, double rate, double shift, double centerFrequency = DEFAULT_CENTER_FREQUENCY) : this (encoder, rate, shift, false, centerFrequency) { }

        public RTTYEncoder(AudioEncoderBase encoder, double rate, double shift, bool leaveOpen, double centerFrequency = DEFAULT_CENTER_FREQUENCY)
        {
            _leaveOpen = leaveOpen;
            _audioEnc = encoder;

            _rate = rate;
            _ratePerBit = (1.0 / rate) * 1000.0;

            _endFreq = centerFrequency - shift * 0.5;
            _startFreq = _endFreq + shift;
            _toneGen = new ToneGenerator(encoder.BitsPerSample < 16, _audioEnc.Samples, _audioEnc.NumChannels > 1, _audioEnc.SampleRate, _audioEnc.FlushData, 8192);
        }

        public override void Dispose()
        {
            if (_leaveOpen) { return; }

            _audioEnc.Dispose();
        }

        public RTTYEncodeResult Encode(string str)
        {
            RaiseOnBegin();
            _prgData.title = $"RTTY Encode";
            _prgData.mainProgress = 0.0f;
            _prgData.curMain = 0;

            //Make sure we only have valid chars
            var sb = ValidateString(str);
            if(sb.Length < 1)
            {
                _prgData.maxMain = 0;
                RaiseOnProgress(ref _prgData);

                RaiseOnFinished();
                return RTTYEncodeResult.InputStringEmpty; 
            }

            _prgData.maxMain = sb.Length;
            RaiseOnProgress(ref _prgData);
            for (int i = 0; i < sb.Length; i++)
            {
                var c = sb[i];
                var rtty = new BaudotChar(c);

                //Add Figure/Letter char if we're at index 0 or if the current char has a different
                //state than what we're currently on
                if((rtty.IsSymbol != _isSymbol && !rtty.IsControl) | i == 0)
                {
                    _isSymbol = rtty.IsSymbol;

                    //Writes start bit, FIGS/LTRS and end bit
                    _toneGen.Generate(_startFreq, _volume, _ratePerBit);
                    WriteBits(_isSymbol ? Baudot.FIGS : Baudot.LTRS, 5);
                    _toneGen.Generate(_endFreq, _volume, _ratePerBit);
                }

                //Writes start bit, 5 bit Baudot char and end bit
                _toneGen.Generate(_startFreq, _volume, _ratePerBit);
                WriteBits(rtty.GetValue, 5);
                _toneGen.Generate(_endFreq, _volume, _ratePerBit);

                if (TriggerProgress())
                {
                    _prgData.mainProgress = i / (sb.Length < 2.0f ? 1.0f : sb.Length - 1.0f);
                    _prgData.curMain = i + 1;
                    RaiseOnProgress(ref _prgData);
                }
            }

            _audioEnc.FlushData();
            _audioEnc.WritePostData();

            RaiseOnFinished();
            return RTTYEncodeResult.Success;
        }

        private void WriteBits(byte b, int bitCount)
        {
            for (int i = 0; i < bitCount; i++)
            {
                _toneGen.Generate((b & (1 << i)) != 0 ? _endFreq : _startFreq, _volume, _ratePerBit);
            }
        }

        private StringBuilder ValidateString(string str)
        {
            str = str.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                var c = Char.ToUpper(str[i]);
                if (Baudot.IsValidChar(c))
                {
                    sb.Append(c);
                }
            }
            return sb;
        }

    }
}
