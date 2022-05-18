using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Joonaxii.IO.CFG
{
    public class ConfigStream : IDisposable
    {
        private static char[] FLAG_SEPARATOR = new char[] { '|' };

        private ConfigGroup _topGroup;
        private List<ConfigGroup> _groups;

        private Stream _stream;

        private bool _flushPending;
        private List<ConfigComment> _comments;

        public ConfigStream(Stream stream)
        {
            _comments = new List<ConfigComment>();
            _groups = new List<ConfigGroup>();
            _topGroup = new ConfigGroup("");

            SetStream(stream, true, true);
        }

        public void SetStream(Stream stream, bool load, bool clear)
        {
            _stream = stream;
            if (load && _stream != null && _stream.CanRead)
            {
                ReadFromStream(_stream, clear);
            }
        }

        public void ReadFromStream(Stream stream, bool clear)
        {
            if (clear)
            {
                Clear();
            }

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 8192, true))
            {
                var topG = _topGroup;
                while (topG.Read(reader, out string nxt))
                {
                    topG = new ConfigGroup(nxt);
                    _groups.Add(topG);
                }
            }        
        }

        public void AddOrUpdateField(string groupName, string fieldName, object value)
        {
            var group = GetOrAddGroup(groupName);
            group.AddOrUpdateValue(fieldName, value);
            _flushPending |= true;
        }

        public void WriteFrom(IConfigSource source) => source.Write(this);
        public void ReadTo(IConfigSource source) => source.Read(this);

        private void WriteComments(StreamWriter writer, int location, ref int line)
        {
            foreach (var item in _comments)
            {
                if (item.commentLocation == location)
                {
                    item.Write(writer, ref line);
                }
            }
        }

        public T[] GetArrayValue<T>(string groupName, string fieldName)
        {
            var groupI = IndexOfGroup(groupName);
            ConfigGroup group;

            if (groupI == null)
            {
                group = _topGroup;
            }
            else
            {
                if (groupI.Value < 0) { return null; }
                group = _groups[groupI.Value];
            }

            string[] names = null;
            if (group.TryFindValue(fieldName, out var val))
            {
                if (val.IsArray && val.Value is IList list)
                {
                    bool isEnum = typeof(T).IsEnum;
                    T[] array = new T[list.Count];
                    if (isEnum)
                    {
                        var type = typeof(T);
                        string tgt;
                        for (int i = 0; i < array.Length; i++)
                        {
                            tgt = array[i].ToString();
                            if (tgt.IndexOf('|') > -1)
                            {
                                array[i] = (T)Enum.ToObject(type, GetEnumFlagsAsValue(type, ref names, tgt.Split(FLAG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)));
                                continue;
                            }

                            if (IsDefined(tgt, ref names, type))
                            {
                                array[i] = (T)Enum.Parse(type, tgt, true);
                                continue;
                            }
                            array[i] = ulong.TryParse(tgt, out var valO) ? (T)Enum.ToObject(type, valO) : default;
                        }
                        return array;
                    }

                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = (T)Convert.ChangeType(list[i], typeof(T));
                    }
                    return array;
                }
            }
            return null;
        }

        private ulong GetEnumFlagsAsValue(Type t, ref string[] names, string[] flags)
        {
            ulong v = 0;
            Type udType = Enum.GetUnderlyingType(t);
            for (int i = 0; i < flags.Length; i++)
            {
                if(!IsDefined(flags[i].Trim(), ref names, t)) { continue; }
                v |= (ulong)Convert.ChangeType(Convert.ChangeType(Enum.Parse(t, flags[i], true), udType), typeof(ulong));
            }
            return v;
        }

        private bool IsDefined(string value, ref string[] names, Type type)
        {
            if(names == null) { names = Enum.GetNames(type); }
            for (int i = 0; i < names.Length; i++)
            {
                if(names[i].Equals(value, StringComparison.InvariantCultureIgnoreCase)) { return true; }
            }
            return false;
        }

        public object GetValue(string groupName, string fieldName)
        {
            var groupI = IndexOfGroup(groupName);
            ConfigGroup group;

            if (groupI == null)
            {
                group = _topGroup;
            }
            else
            {
                if (groupI.Value < 0) { return null; }
                group = _groups[groupI.Value];
            }

            return group.TryFindValue(fieldName, out var obj) ? obj.Value : null;
        }

        public T GetValue<T>(string groupName, string fieldName, T defaultValue = default)
        {
            var groupI = IndexOfGroup(groupName);
            ConfigGroup group;

            if (groupI == null)
            {
                group = _topGroup;
            }
            else
            {
                if (groupI.Value < 0) { return defaultValue; }
                group = _groups[groupI.Value];
            }

            if (group.TryFindValue(fieldName, out var obj))
            {
                var type = typeof(T);

                if (type.IsEnum)
                {
                    string[] names = null;
                    string tgt = obj.Value.ToString().Replace(",", " |");
                    if (tgt.IndexOf('|') > -1)
                    {
                       return (T)Enum.ToObject(type, GetEnumFlagsAsValue(type, ref names, tgt.Split(FLAG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)));
                    }

                    if (IsDefined(tgt, ref names, type))
                    {
                        return (T)Enum.Parse(type, tgt, true);
                    }
                    return ulong.TryParse(tgt, out var valO) ? (T)Enum.ToObject(type, valO) : defaultValue;
                }
                return (T)Convert.ChangeType(obj.Value, typeof(T));
            }
            return defaultValue;
        }

        public void RemoveField(string groupName, string fieldName)
        {
            var group = GetOrAddGroup(groupName);
            group.RemoveValue(fieldName);
        }

        public void InsertComment(string comment, int type, int location) => InsertComment(comment, type, location, LineBreakFlags.OnlyUpIfNotAtTop, 0, 1);
        public void InsertComment(string comment, int type, int location, LineBreakFlags flags) => InsertComment(comment, type, location, flags, 0, 1);
        public void InsertComment(string comment, int type, int location, LineBreakFlags flags, byte paddingUp, byte paddingDown)
        {
            _comments.Add(new ConfigComment(comment, type, location, flags, paddingUp, paddingDown));
        }

        public void RemoveGroup(string groupName)
        {
            var group = IndexOfGroup(groupName);

            if (group == null)
            {
                _topGroup.Clear();
                return;
            }

            int val = group.Value;
            if (val < 0) { return; }
            _groups.Clear();
            _groups.RemoveAt(val);
        }

        public void Flush(Stream stream)
        {
            if (!stream.CanWrite) { return; }

            if (!_flushPending) { return; }
            int line = 0;
            int loc = 0;
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, 8192, true))
            {
                _flushPending = false;

                WriteComments(writer, loc++, ref line);
                _topGroup.Write(writer);
                foreach (var item in _groups)
                {
                    if (item.Count < 1) { continue; }
                    writer.Write('\n');
                    WriteComments(writer, loc++, ref line);
                    item.Write(writer);
                }

                //Write last comments
                WriteComments(writer, -1, ref line);
            }
        }

        public void Clear()
        {
            _comments.Clear();
            foreach (var item in _groups)
            {
                item.Clear();
            }
            _topGroup.Clear();
            _groups.Clear();
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                Flush(_stream);
                _stream = null;
            }

            Clear();
        }

        private ConfigGroup GetOrAddGroup(string name)
        {
            var group = IndexOfGroup(name);
            if (group == null) { return _topGroup; }

            if (group.Value < 0)
            {
                var grp = new ConfigGroup(name);
                group = _groups.Count;
                _groups.Add(grp);
            }
            return _groups[group.Value];
        }

        private int? IndexOfGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) { return null; }

            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i].Name.Equals(groupName)) { return i; }
            }
            return -1;
        }
    }
}