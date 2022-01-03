using System.IO;

namespace Joonaxii.Data.Image.GIFDecoder
{
    public struct PlainTextData
    {
        public ushort xPos;
        public ushort yPos;

        public ushort width;
        public ushort height;

        public byte cellWidth;
        public byte cellHeight;

        public byte bgIndex;
        public byte fgIndex;

        public string data;

        public static PlainTextData ReadPlainTextData(BinaryReader br)
        {
            PlainTextData data = new PlainTextData()
            {
                xPos = br.ReadUInt16(),
                yPos = br.ReadUInt16(),
                width = br.ReadUInt16(),
                height = br.ReadUInt16(),

                cellWidth = br.ReadByte(),
                cellHeight = br.ReadByte(),

                bgIndex = br.ReadByte(),
                fgIndex = br.ReadByte(),
            };

            data.data = "";
            byte b = br.ReadByte();
            while(b != 0)
            {
                for (int i = 0; i < b; i++)
                {
                    data.data += (char)br.ReadByte();
                }
                b = br.ReadByte();
            }
            return data;
        }
    }
}