using System.IO;

namespace Joonaxii.IO.CFG
{
    internal class ConfigComment
    {
        public string comment;
        public int commentLocation;
        public int commentType;

        public LineBreakFlags LineBreakFlags { get => (LineBreakFlags)(_linePadding & 0xFF_FF); }
        public byte PaddingUp { get => (byte)((_linePadding >> 16) & 0xFF); }
        public byte PaddingDown { get => (byte)((_linePadding >> 24) & 0xFF); }

        private int _linePadding;

        public ConfigComment(string comment, int type, int location, LineBreakFlags flags, byte paddingUp, byte paddingDown)
        {
            this.commentLocation = location;
            _linePadding = (int)flags | (paddingUp << 16) | (paddingDown << 24);

            commentType = type;
            this.comment = comment;
        }

        public void Write(StreamWriter writer, ref int line)
        {
            if(line > 0 | !LineBreakFlags.HasFlag(LineBreakFlags.OnlyUpIfNotAtTop))
            {
                for (int i = 0; i < PaddingUp; i++)
                {
                    writer.Write('\n');
                    line++;
                }
            }
            string commentChar = ConfigGroup.COMMENT_STRINGS[commentType % ConfigGroup.COMMENT_STRINGS.Length];
            writer.Write(commentChar);
            for (int i = 0; i < comment.Length; i++)
            {
                var c = comment[i];
                if (c == '\n')
                {
                    writer.Write('\n');
                    writer.Write(commentChar);
                    continue;
                }
                writer.Write(c);
            }
            writer.Write('\n');
            line++;

            for (int i = 0; i < PaddingDown; i++)
            {
                writer.Write('\n');
                line++;
            }
        }
    }
}