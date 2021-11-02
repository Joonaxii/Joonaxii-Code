using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Joonaxii.Debugging
{
    public class FileDebugger
    {
        public bool IsDirty { get => _curData != null || _curChildData != null; }

        private string _title;

        private List<DebugData> _debugData = new List<DebugData>();
        private DebugData _curData;
        private DebugData _curChildData;

        private Stream _stream;

        public FileDebugger(string title, Stream stream)
        {
            _title = title;
            _stream = stream;
        }

        public void Start(string name)
        {
            _curData = new DebugData(name, _stream.Position);
        }

        public void StartSub(string name)
        {
            if(_curData == null) { return; }
            _curChildData = new DebugData(name, _stream.Position);
            _curData.segments.Add(_curChildData);
        }

        public void Stamp(bool both = false)
        {
            if (!IsDirty) { return; }

            if (_curChildData != null)
            {
                _curChildData.endPos = _stream.Position;
                _curChildData = null;

                if (!both)
                {
                    return;
                }
            }

            _curData.endPos = _stream.Position;
            _debugData.Add(_curData);

            _curData = null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"┌[{_title}]");
            for (int i = 0; i < _debugData.Count; i++)
            {
                sb.AppendLine(_debugData[i].ToString());
            }
            sb.AppendLine($"╘═════════════════════");
            return sb.ToString();
        }

        private class DebugData
        {
            public string name;

            public long startPos;
            public long endPos;

            public List<DebugData> segments = new List<DebugData>();

            public DebugData(string name, long startPos)
            {
                this.name = name;
                this.startPos = startPos;
                this.endPos = 0;
            }

            public string ToString(bool isChild = false)
            {
                if(segments.Count < 1) { return isChild ? $"{name}: {DebugExtensions.GetFileSizeString((endPos - startPos))}" : $"├{name}: {DebugExtensions.GetFileSizeString((endPos - startPos))}"; }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"├┬{name}: {DebugExtensions.GetFileSizeString((endPos - startPos))}");
                for (int i = 0; i < segments.Count; i++)
                {
                    bool isLast = i >= segments.Count - 1;

                    sb.AppendLine($"│{(isLast ? "└" : "├")}─{segments[i].ToString(true)}");
                }

                sb.Append($"├─────────────────────────────");
                return sb.ToString();
            }
        }
    }
}