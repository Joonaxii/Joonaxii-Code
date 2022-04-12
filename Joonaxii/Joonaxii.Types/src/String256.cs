using Joonaxii.MathJX;
using System.Runtime.InteropServices;
using System.Text;

namespace Joonaxii.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 256 * sizeof(ushort))]
    public unsafe struct String256
    {
        public static String256 Empty { get; } = new String256();

        public byte Length
        {
            get
            {
                byte len = 0;
                fixed (char* ptr = _chars)
                {
                    char* p = ptr;
                    while (*p++ != '\0')
                    {
                        if (++len > 254) { return len; }
                    }
                }
                return len;
            }
        }

        public char this[int i]
        {
            get => i < 0 | i > 255 ? '\0' : _chars[i];

            set
            {
                if (i < 0 | i > 255) { return; }
                _chars[i] = value;
            }
        }

        private fixed char _chars[256];

        public String256(string str) : this()
        {
            fixed (char* sPtr = str)
            fixed (char* cPtr = _chars)
            {
                int len = str.Length < 255 ? str.Length : 255;
                BufferUtils.Memcpy(sPtr, cPtr, len);
            }
        }

        public String256(char* str) : this()
        {
            fixed (char* cPtr = _chars)
            {
                char* ptr = cPtr;
                int len = 0;
                while (true)
                {
                    char c = *str++;
                    if (c == '\0') { break; }
                    *ptr++ = c;
                    if (++len >= 255) { break; }
                }
            }
        }

        public String256(char* str, int count) : this()
        {
            fixed (char* cPtr = _chars)
            {
                int len = count < 255 ? count : 255;
                BufferUtils.Memcpy(str, cPtr, len);
            }
        }

        public String256(char* str, int start, int count) : this()
        {
            fixed (char* cPtr = _chars)
            {
                int len = count < 255 ? count : 255;
                BufferUtils.Memcpy(str + start, cPtr, len);
            }
        }

        public String256(char v, int len) : this()
        {
            fixed (char* cPtr = _chars)
            {
                len = len < 255 ? len : 255;
                BufferUtils.Memset(cPtr, v, 0, len);
            }
        }

        public static implicit operator String256(string str) => new String256(str);
        public static explicit operator string(String256 str) => new string(str._chars);
        public static implicit operator char*(String256 str) => str._chars;

        public void Set(string str)
        {
            Clear();
            fixed (char* sPtr = str)
            fixed (char* cPtr = _chars)
            {
                int len = str.Length < 255 ? str.Length : 255;
                BufferUtils.Memcpy(sPtr, cPtr, len);
            }
        }

        public String256 Append(string str)
        {
            fixed (char* mPtr = _chars)
            fixed (char* sPtr = str)
            {
                char* ptrM = mPtr;
                char* ptrS = sPtr;

                int pos = 0;
                int strP = 0;
                while (pos < 255)
                {
                    if (*ptrM == '\0')
                    {
                        *ptrM++ = *ptrS++;
                        if (++strP >= str.Length) { break; }
                    }
                    pos++;
                }
            }
            return this;
        }

        public String256 Append(int start, string str) => Append(start, str, 0);
        public String256 Append(int start, string str, int startStr)
        {
            if (start > 254 | startStr >= str.Length) { return this; }

            startStr = startStr < 0 ? 0 : startStr;
            start = start < 0 ? 0 : start;
            fixed (char* mPtr = _chars)
            fixed (char* sPtr = str)
            {
                char* ptrM = mPtr + start;
                char* ptrS = sPtr + startStr;

                int pos = start;
                int strP = startStr;
                while (pos < 255)
                {
                    *ptrM++ = *ptrS++;
                    if (++strP >= str.Length) { break; }
                    pos++;
                }
            }
            return this;
        }

        public int IndexOf(char c, int start, int length)
        {
            start = start < 0 ? 0 : start;
            length = length < 0 | length > 255 ? 255 - start : length;

            fixed (char* mPtr = _chars)
            {
                char* ptrM = mPtr + start;

                int pos = start;
                while ((pos < 255) & (length-- > 0))
                {
                    if (*ptrM++ == c) { return pos; }
                    pos++;
                }
            }
            return -1;
        }

        public int LastIndexOf(char c, int start, int length)
        {
            start = start < 0 ? 0 : start;
            length = length < 0 | length > 255 ? 255 - start : length;

            fixed (char* mPtr = _chars)
            {
                int pos = start + length;
                pos = pos > 255 ? 255 : pos;

                char* ptrM = mPtr + pos;
                while ((pos > -1) & (length-- > 0))
                {
                    if (*ptrM-- == c) { return pos; }
                    pos--;
                }
            }
            return -1;
        }

        public String256 Substring(int length)
        {
            if (length < 1) { return String256.Empty; }
            length = length > 255 ? 255 : length;

            char* temp = stackalloc char[length];
            BufferUtils.Memset(temp, '\0', length);

            int tmp = 0;
            fixed (char* ptr = _chars)
            {
                char* tPtr = temp;
                char* cPtr = ptr;
                while (tmp < length)
                {
                    char c = *cPtr++;
                    if (c == '\0') { break; }
                    *tPtr++ = c;
                    tmp++;
                }
            }
            return *(String256*)temp;
        }

        public String256 Substring(int start, int length)
        {
            if (length < 1 | start > 254) { return String256.Empty; }
            start = start < 0 ? 0 : start;
            length = length > 255 ? 255 : length;

            char* temp = stackalloc char[length];
            BufferUtils.Memset(temp, '\0', length);

            int tmp = 0;
            fixed (char* ptr = _chars)
            {
                char* tPtr = temp;
                char* cPtr = ptr + start;
                while (tmp < length)
                {
                    char c = *cPtr++;
                    if (c == '\0') { break; }
                    *tPtr++ = c;
                    tmp++;
                }
            }
            return *(String256*)temp;
        }

        public void Clear()
        {
            fixed (char* cPtr = _chars)
            {
                BufferUtils.Memset(cPtr, '\0', 0, 256);
            }
        }

        public override string ToString()
        {
            fixed (char* ptr = _chars)
            {
                return new string(ptr);
            }
        }

        public string ToString(Encoding enc)
        {
            int len = Length;
            int len2 = len << 1;
            byte* bytes = stackalloc byte[len2];

            fixed (char* ptr = _chars)
            {
                enc.GetBytes(ptr, len, bytes, len2);
                return enc.GetString(bytes, len);
            }
        }
    }
}
