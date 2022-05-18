using System.Collections.Generic;
using System.IO;

namespace Joonaxii.IO.CFG
{
    public class ConfigGroup
    {
        public static readonly string[] COMMENT_STRINGS = new string[]
        {
            "#",
            "//",
        };

        public int Count { get => _values.Count; }

        public string Name { get => _name; }
        private string _name;

        public ConfigValue this[int i]
        {
            get => _values[i];
        }

        private List<ConfigValue> _values;

        public ConfigGroup(string name)
        {
            _name = name;
            _values = new List<ConfigValue>();
        }

        public void Clear()
        {
            _values.Clear();
        }

        public void AddOrUpdateValue(string name, object value)
        {
            int id = IndexOf(name);
            if (id < 0) 
            {
                _values.Add(new ConfigValue(name, value));
                return;
            }
            _values[id].SetValue(value);
        }

        public bool TryFindValue(string name, out ConfigValue value)
        {
            int id = IndexOf(name);
            if(id < 0) { value = null;  return false; }
 
            value = _values[id];
            return true;
        }

        public void Write(StreamWriter writer)
        {
            if (!string.IsNullOrEmpty(_name))
            {
                writer.WriteLine($"[{_name}]");
            }

            foreach (var value in _values)
            {
                value.Write(writer);
                writer.Write('\n');
            }
        }

        public void RemoveValue(string name)
        {
            int i = IndexOf(name);
            if(i < 0) { return; }
            _values.RemoveAt(i);
        }

        public bool Read(StreamReader reader, out string nextGroup)
        {
            _values.Clear();
            nextGroup = "";
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) { continue; }

                int s = -1;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] != ' ') { s = i; break; }
                }
                if (s < 0) { continue; }

                bool isComment = false;
                for (int i = 0; i < COMMENT_STRINGS.Length; i++)
                {
                    int idOf = line.IndexOf(COMMENT_STRINGS[i], s);
                    if(idOf < 0) { continue; }

                    if(idOf - s == 0) { isComment = true; break; }
                }
                if (isComment) { continue; }

                char c = line[s];
                if (c == '[')
                {
                    int end = line.LastIndexOf(']');
                    end = end < 0 ? line.Length : end;
                    s++;
                    nextGroup = line.Substring(s, end - s);
                    return true;
                }

                var val = new ConfigValue();
                val.Parse(line);
                _values.Add(val);
            }
            return false;
        }

        private int IndexOf(string name)
        {
            for (int i = 0; i < _values.Count; i++)
            {
                if (_values[i].Name.Equals(name)) { return i; }
            }
            return -1;
        }
    }
}