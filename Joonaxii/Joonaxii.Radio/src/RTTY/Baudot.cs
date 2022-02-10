using System;

namespace Joonaxii.Radio.RTTY
{
    public class Baudot
    {
        public const byte FIGS = 0x1B;
        public const byte LTRS = 0x1F;

        public static bool IsControl(char c) => IsControl(ToBaudot(c));
        public static bool IsControl(byte v)
        {
            switch (v)
            {
                default: return false;

                //Null
                case 0x00:
                case 0x80:

                //Line break
                case 0x01:
                case 0x81:

                //Space
                case 0x04:
                case 0x84:

                //Carriage return
                case 0x11:
                case 0x91:
                    return true;
            }
        }

        public static byte ToBaudot(char c)
        {
            c = ConvertToValid(c);
            switch (c)
            {
                default: return 0;

                case 'E': return 0x01;
                case '3': return 0x81;

                case '\n': return 0x02;

                case 'A': return 0x03;
                case '-': return 0x83;

                case ' ': return 0x04;

                case 'S': return 0x05;
                case '\'': return 0x85;

                case 'I': return 0x06;
                case '8': return 0x86;

                case 'U': return 0x07;
                case '7': return 0x87;

                case '\r': return 0x08;

                case 'D': return 0x09;
                case '\x05': return 0x89;

                case 'R': return 0x0A;
                case '4': return 0x8A;

                case 'J': return 0x0B;
                case '\a': return 0x8B;

                case 'N': return 0x0C;
                case ',': return 0x8C;

                case 'F': return 0x0D;
                case '!': return 0x8D;

                case 'C': return 0x0E;
                case ':': return 0x8E;

                case 'K': return 0x0F;
                case '(': return 0x8F;

                case 'T': return 0x10;
                case '5': return 0x90;

                case 'Z': return 0x11;
                case '+': return 0x91;

                case 'L': return 0x12;
                case ')': return 0x92;

                case 'W': return 0x13;
                case '2': return 0x93;

                case 'H': return 0x14;
                case '$': return 0x94;

                case 'Y': return 0x15;
                case '6': return 0x95;

                case 'P': return 0x16;
                case '0': return 0x96;

                case 'Q': return 0x17;
                case '1': return 0x97;

                case 'O': return 0x18;
                case '9': return 0x98;

                case 'B': return 0x19;
                case '?': return 0x99;

                case 'G': return 0x1A;
                case '&': return 0x9A;

                case 'M': return 0x1C;
                case '.': return 0x9C;

                case 'X': return 0x1D;
                case '/': return 0x9D;

                case 'V': return 0x1E;
                case ';': return 0x9E;
            }
        }

        public static char ConvertToValid(char c)
        {
            switch (c)
            {
                default: return c;
                case '_': return '-';
                case '\\': return '/';
            }
        }

        public static bool IsValidChar(char c) => c == '\0' || ToBaudot(c) > 0;
        public static char ToChar(byte value, bool isSymbol)
        {
            switch (value)
            {
                default: return '\0';
                case 0x01: return isSymbol ? '3' : 'E';
                case 0x02: return '\n';
                case 0x03: return isSymbol ? '-' : 'A';
                case 0x04: return ' ';
                case 0x05: return isSymbol ? '\'' : 'S';
                case 0x06: return isSymbol ? '8' : 'I';
                case 0x07: return isSymbol ? '7' : 'U';
                case 0x08: return '\r';
                case 0x09: return isSymbol ? '\x05' : 'D';
                case 0x0A: return isSymbol ? '4' : 'R';
                case 0x0B: return isSymbol ? '\a' : 'J';
                case 0x0C: return isSymbol ? ',' : 'N';
                case 0x0D: return isSymbol ? '!' : 'F';
                case 0x0E: return isSymbol ? ':' : 'C';
                case 0x0F: return isSymbol ? '(' : 'K';
                case 0x10: return isSymbol ? '5' : 'T';
                case 0x11: return isSymbol ? '+' : 'Z';
                case 0x12: return isSymbol ? ')' : 'L';
                case 0x13: return isSymbol ? '2' : 'W';
                case 0x14: return isSymbol ? '$' : 'H';
                case 0x15: return isSymbol ? '6' : 'Y';
                case 0x16: return isSymbol ? '0' : 'P';
                case 0x17: return isSymbol ? '1' : 'Q';
                case 0x18: return isSymbol ? '9' : 'O';
                case 0x19: return isSymbol ? '?' : 'B';
                case 0x1A: return isSymbol ? '&' : 'G';

                case 0x1C: return isSymbol ? '.' : 'M';
                case 0x1D: return isSymbol ? '/' : 'X';
                case 0x1E: return isSymbol ? ';' : 'V';
            }
        }

        public static char ToChar(byte value)
        {
            bool isSymbol = (value & 0x80) != 1;
            value = (byte)(value & 0x1F);
            return ToChar(value, isSymbol);
        }
    }
}
