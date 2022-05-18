using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Joonaxii.IO.CFG
{
    public class ConfigValue
    {
        public string Name { get => _name; }
        private string _name;

        public ConfigValueType ValueType { get => _type; }
        private ConfigValueType _type;

        public bool IsArray { get; private set; }

        public object Value { get => _value; }
        private object _value;

        private List<object> _buffer = new List<object>();

        public ConfigValue() { }
        public ConfigValue(string name, object value)
        {
            _name = name;
            SetValue(value);
        }

        public void SetValue(object value)
        {
            if (value == null) { return; }
            Type type = value.GetType();
            Type genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;

            bool isArray = genericType != null && typeof(IList<>).IsAssignableFrom(genericType);
            if (isArray)
            {
                type = genericType;
            }

            var typeC = GetValueType(type);
            if (typeC == ConfigValueType.Unknown) { return; }

            IsArray = isArray;
            _type = typeC;
            _value = value;
        }

        public void Parse(string line)
        {
            _name = string.Empty;
            _type = ConfigValueType.Unknown;
            _value = null;
            IsArray = false;

            //Look for name
            int sI = line.IndexOf('\"');
            if (sI < 0) { return; }

            int eI = line.IndexOf("\":", sI);
            if (eI < 0) { return; }

            sI++;
            _name = line.Substring(sI, eI - sI);

            eI += 2;
            sI = FirstNonEmpty(line, eI, line.Length - eI, out char c);
            if (sI < 0) { return; }

            IsArray = c == '[';
            if (IsArray)
            {
                _buffer.Clear();
                sI++;

                ParseArray(_buffer, line, sI, out _type);
                if (_type != ConfigValueType.Unknown)
                {
                    _value = _buffer.ToArray();
                }
                return;
            }

            eI = line.LastIndexOf(',');
            eI = eI < 0 ? line.Length : eI;

            _value = ParseType(line, sI, eI - sI, out _type);
        }

        private void ParseArray(List<object> objs, string input, int start, out ConfigValueType type)
        {
            int sPoint = start;
            bool isInString = false;
            bool isInChar = false;
            char p = '\0';

            type = ConfigValueType.Unknown;
            ConfigValueType oType;
            for (int i = start; i < input.Length; i++)
            {
                var c = input[i];
                switch (c)
                {
                    default:
                        if(i >= input.Length - 1)
                        {
                            objs.Add(ParseType(input, sPoint, i - sPoint, out oType));
                            if (type == ConfigValueType.Unknown)
                            {
                                type = oType;
                            }
                            sPoint = i + 1;

                            isInString = false;
                            isInChar = false;
                        }
                        break;
                    case '\'':
                        if (isInString | p == '\\') { break; }
                        isInChar = !isInChar;
                        break;
                    case '\"':
                        if (!isInChar && p != '\\')
                        {
                            isInString = !isInString;
                        }
                        break;
                    case ',':
                        objs.Add(ParseType(input, sPoint, i - sPoint, out oType));
                        if (type == ConfigValueType.Unknown)
                        {
                            type = oType;
                        }
                        sPoint = i + 1;

                        isInString = false;
                        isInChar = false;
                        break;
                }
                p = c;
            }
        }

        private object ParseType(string input, int start, int len, out ConfigValueType type)
        {
            int st = FirstNonEmpty(input, start, len, out char c);
            len -= (st - start);

            int next;
            type = ConfigValueType.Unknown;
            switch (c)
            {
                case '\'':
                    next = FindEndOf(input, st);
                    if (next < 0) { return null; }

                    type = ConfigValueType.Char;
                    string str = Regex.Unescape(input.Substring(st + 1, next - st - 1));
                    return (Char.TryParse(str, out var ch) ? ch : '\0');

                case '\"':
                    next = FindEndOf(input, st);
                    if (next < 0) { return null; }

                    type = ConfigValueType.String;
                    try
                    {
                        string strVal = input.Substring(st + 1, next - st - 1);
                        return Regex.Unescape(strVal);
                    }
                    catch
                    {
                        return null;
                    }

                default:
                    next = input.IndexOf(',', st)/* NextWhiteSpace(input, st, len)*/;
                    len = next < 0 ? len : next - st;

                    var val = ParseValue(input, st, len, out var tp);
                    if (val != null & tp != ConfigValueType.Unknown)
                    {
                        type = tp;
                        return val;
                    }

                    type = ConfigValueType.Unknown;
                    return null;
            }
        }

        private int FindEndOf(string input, int start)
        {
            bool inChar = false;
            bool inString = false;

            char p = '\0';
            for (int i = start; i < input.Length; i++)
            {
                var c = input[i];
                switch (c)
                {
                    case '\'':
                        if (inString | p == '\\') { break; }
                        inChar = !inChar;

                        if (!inChar) { return i; }
                        break;
                    case '\"':
                        if (!inChar && p != '\\')
                        {
                            inString = !inString;
                            if (!inString) { return i; }
                        }
                        break;
                }
                p = c;
            }
            return -1;
        }

        private object ParseValue(string input, int start, int len, out ConfigValueType type)
        {
            int buf = 0b100;

            type = ConfigValueType.Unknown;
            for (int i = start; i < start + len; i++)
            {
                char c = input[i];
                if (c == '-')
                {
                    buf |= 0x2;
                    continue;
                }

                if (c == '.')
                {
                    buf |= 0x1;
                    continue;
                }
                if (!Char.IsNumber(c)) { buf &= 0x01; break; }
            }

            string val = input.Substring(start, len);
            switch (buf)
            {
                case 0b000:
                case 0b010: //Possibly bool or enum
                    if (val.IndexOf('|') < 0 && bool.TryParse(val, out bool bVal))
                    {
                        type = ConfigValueType.Bool;
                        return bVal;
                    }

                    type = ConfigValueType.Enum;
                    return val;
                case 0b100: //Unsigned Integer
                    if (ulong.TryParse(val, out ulong uLVal))
                    {
                        type = ConfigValueType.Int;
                        return uLVal;
                    }
                    return null;
                case 0b110: //Signed Integer
                    if (long.TryParse(val, out long sLVal))
                    {
                        type = ConfigValueType.Int;
                        return sLVal;
                    }
                    return null;
                case 0b101:
                case 0b111: //Floating Point
                    if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double dVal))
                    {
                        type = ConfigValueType.Float;
                        return dVal;
                    }
                    return null;
            }
            return null;
        }

        private int NextWhiteSpace(string input, int start, int len)
        {
            for (int i = start; i < start + len; i++)
            {
                if (Char.IsWhiteSpace(input[i])) { return i; }
            }
            return -1;
        }

        private int FirstNonEmpty(string input, int start, int len, out char c)
        {
            for (int i = start; i < start + len; i++)
            {
                c = input[i];
                if (!Char.IsWhiteSpace(c)) { return i; }
            }
            c = '\0';
            return -1;
        }

        public void Write(StreamWriter writer)
        {
            if (_type == ConfigValueType.Unknown) { return; }
            writer.Write($"\"{_name}\": ");

            if (IsArray)
            {
                writer.Write("[");
                if (_value is IList list)
                {
                    int ii = 0;
                    foreach (var item in list)
                    {
                        WriteValue(writer, item);
                        if (++ii >= list.Count) { break; }
                        writer.Write(", ");
                    }
                }
                writer.Write("]");
                return;
            }

            WriteValue(writer, _value);
            writer.Write(", ");
        }

        private void WriteValue(StreamWriter writer, object obj)
        {
            switch (_type)
            {
                case ConfigValueType.Int:
                    writer.Write(Convert.ToInt64(obj));
                    break;
                case ConfigValueType.Float:
                    writer.Write(string.Format(CultureInfo.InvariantCulture, "{0:0.0#########}", Convert.ToDouble(obj)));
                    break;
                case ConfigValueType.Char:
                    writer.Write($"'{Convert.ToChar(obj, CultureInfo.InvariantCulture)}'");
                    break;
                case ConfigValueType.String:
                    writer.Write($"\"{Regex.Escape(obj.ToString())}\"");
                    break;
                case ConfigValueType.Bool:
                    writer.Write($"{obj}");
                    break;
                case ConfigValueType.Enum:
                    writer.Write($"{obj.ToString().Replace(",", " |")}");
                    break;
            }
        }

        private ConfigValueType GetValueType(Type type)
        {
            if (type.IsEnum)
            {
                return ConfigValueType.Enum;
            }

            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                default: return ConfigValueType.Unknown;
                case TypeCode.String: return ConfigValueType.String;
                case TypeCode.Char: return ConfigValueType.Char;

                case TypeCode.SByte:
                case TypeCode.Byte:

                case TypeCode.Int16:
                case TypeCode.UInt16:

                case TypeCode.Int32:
                case TypeCode.UInt32:

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return ConfigValueType.Int;

                case TypeCode.Single:
                case TypeCode.Double:
                    return ConfigValueType.Float;

                case TypeCode.Boolean: return ConfigValueType.Bool;
            }
        }
    }
}