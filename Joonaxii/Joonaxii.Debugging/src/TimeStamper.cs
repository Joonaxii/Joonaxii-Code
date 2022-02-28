using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Joonaxii.Debugging
{
    public class TimeStamper
    {
        private List<TimeMarker> _timeStamps = new List<TimeMarker>();
        private Stopwatch _sw = new Stopwatch();
        private TimeMarker _current;

        private string _title;

        public TimeStamper() : this("") { }
        public TimeStamper(string title) { _title = title; }

        public void Pause() => _sw.Stop();
        public void Resume() => _sw.Start();

        public void Merge(TimeStamper other) => _timeStamps.AddRange(other._timeStamps);
        public void Start(string name)
        {
            Stamp();
            _current = new TimeMarker(name);
            _sw.Restart();
        }

        public TimeMarker Stamp()
        {
            if (_current == null) { return null; }
            _sw.Stop();
            TimeMarker stamp = _current;
            stamp.ticks = _sw.ElapsedTicks;

            _timeStamps.Add(stamp);

            _current = null;
            return stamp;
        }

        public void StartSub(string subName)
        {
            if (_current == null) { return; }
            _current.StartSub(subName);
        }

        public void StampSub(string subName)
        {
            if (_current == null) { return; }
            _current.StampSub(subName);
        }

        public void Stop()
        {
            _sw.Reset();
            _current = null;
        }

        public void Reset()
        {
            Stop();
            _timeStamps.Clear();
        }

        public override string ToString() => ToString(_title);

        public string ToString(string title)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"┌{title}");

            double totS = 0;
            double totMS = 0;
            long totTicks = 0;

            for (int i = 0; i < _timeStamps.Count; i++)
            {
                bool isNotLast = i < _timeStamps.Count - 1;
                string s = isNotLast ? "│ └──" : "  └──";
                string s2 = isNotLast ? "│" : "";
                sb.AppendLine($"{(isNotLast ? "├" : "└")}─┬{_timeStamps[i].name}");
                sb.Append($"{s}");
                _timeStamps[i].ToString(sb, isNotLast ? " " : "│");
                sb.AppendLine(s2);

                TimeSpan span = TimeSpan.FromTicks(_timeStamps[i].ticks);
                totS += span.TotalSeconds;
                totMS += span.TotalMilliseconds;
                totTicks += span.Ticks;
            }
            sb.AppendLine($"Total Time Elapsed: {totS} seconds, {totMS} ms, {totTicks} ticks");
            sb.AppendLine(new string('=', 64));
            return sb.ToString();
        }

        public class TimeMarker
        {

            public string name;
            public long ticks;

            public Dictionary<string, TimeStamp> subParts = new Dictionary<string, TimeStamp>();
     
            public class TimeStamp
            {
                public int Count { get => _counter; }

                public string name;
                public long ticks;
                private int _counter = 0;
                private bool _stopped = true;

                private Stopwatch _sw = new Stopwatch();

                public void Start()
                {
                    Stamp();
                    _sw.Restart();
                    _stopped = false;
                }

                public void Stamp()
                {
                    if (_stopped) { return; }
                    _sw.Stop();
                    ticks += _sw.ElapsedTicks;
                    _counter++;
                    _stopped = true;
                }
            }

            public TimeMarker(string name)
            {
                this.name = name;
            }

            public void StartSub(string name)
            {
                if (!subParts.TryGetValue(name, out var v))
                {
                    subParts.Add(name, v = new TimeStamp());
                    v.name = name;
                }
                v.Start();
            }

            public void StampSub(string name)
            {
                if(subParts.TryGetValue(name, out var v))
                {
                    v.Stamp();
                }
            }

            public void ToString(StringBuilder sb, string start)
            {
                TimeSpan span = TimeSpan.FromTicks(ticks);
                sb.AppendLine($"{start}{span.TotalSeconds} seconds, {span.TotalMilliseconds} ms, {ticks} ticks");

                if (subParts.Count > 0)
                {
                    sb.AppendLine($"{start} -Sub Parts: {subParts.Count}");
                    foreach (var item in subParts)
                    {
                        var part = item.Value;
                        span = TimeSpan.FromTicks(part.ticks);
                        sb.AppendLine($"{start}    -{part.name} ({((part.ticks / (float)ticks) * 100.0f).ToString("F2")}%) [{part.Count}]: {span.TotalSeconds} sec, {span.TotalMilliseconds} ms, {span.Ticks} ticks");
                    }
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                ToString(sb, "");
                return sb.ToString();
            }

            public string ToString(int padding)
            {
                string padD = new string(' ', padding);
                TimeSpan span = TimeSpan.FromTicks(ticks);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{padD}{span.TotalSeconds} seconds, {span.TotalMilliseconds} ms, {ticks} ticks");

                if (subParts.Count > 0)
                {
                    sb.AppendLine($"{padD} -Sub Parts: {subParts.Count}");
                    foreach (var item in subParts)
                    {
                        var part = item.Value;
                        span = TimeSpan.FromTicks(part.ticks);
                        sb.AppendLine($"{padD}    -{part.name} ({((part.ticks / (float)ticks) * 100.0f).ToString("F2")}%): {span.TotalSeconds} sec, {span.TotalMilliseconds} ms, {span.Ticks} ticks");
                    }
                }
                return sb.ToString();
            }

        }
    }
}
