using System.IO;
using System.Text;

namespace Joonaxii.Types
{
    public static class TypeExtensions
    {

        public static void Append(this StringBuilder sb, String256 str)
        {
            unsafe
            {
                sb.Append(str, str.Length);
            }
        }

        public static void AppendLine(this StringBuilder sb, String256 str)
        {
            unsafe
            {
                sb.Append(str, str.Length);
                sb.Append(System.Environment.NewLine);
            }
        }

        public static void Write(this TextWriter tw, String256 str)
        {
            unsafe
            {
                char* ptr = str;
                while (true)
                {
                    char c = *ptr++;
                    if (c == '\0') { break; }
                    tw.Write(c);
                }
            }
        }

        public static void Write(this BinaryWriter bw, String256 str)
        {
            unsafe
            {
                byte len = str.Length;
                bw.Write(len);
                char* ptr = str;
                while (len-- > 0)
                {
                    bw.Write(*ptr++);
                }
            }
        }

        public static unsafe void Write(this BinaryWriter bw, String256* str)
        {
            byte len = str->Length;
            bw.Write(len);
            char* ptr = (char*)str;
            while (len-- > 0)
            {
                bw.Write(*ptr++);
            }
        }

        public static unsafe void Write(this BinaryWriter bw, String256* str, int len)
        {
            bw.Write(len);
            char* ptr = (char*)str;
            while (len-- > 0)
            {
                bw.Write(*ptr++);
            }
        }

        public static String256 ReadString256(this BinaryReader br)
        {
            unsafe
            {
                int len = br.ReadByte();
                if(len < 1) { return String256.Empty; }

                char[] stack = br.ReadChars(len);
                fixed(char* ptr = stack)
                {
                    return new String256(ptr, len);
                }
            }
        }

        public static unsafe void ReadString256(this BinaryReader br, String256* target)
        {
            unsafe
            {
                int len = br.ReadByte();
                target->Clear();
                if (len < 1) { return; }

                char[] stack = br.ReadChars(len);
                char* tgPtr = (char*)target;
                fixed (char* ptr = stack)
                {
                    char* ptrC = ptr;
                    while(len-- > 0)
                    {
                        *tgPtr++ = *ptrC++;
                    }
                }
            }
        }

        public static void WriteLine(this TextWriter tw, String256 str)
        {
            Write(tw, str);
            tw.Write(tw.NewLine);
        }
    }
}
