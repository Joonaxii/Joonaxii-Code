using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Joonaxii.Debugging
{
    public class TimeStamper : IDisposable
    {
        private List<TimeStamp> _timeStamps = new List<TimeStamp>();
        private Stopwatch _sw = new Stopwatch();
        private TimeStamp _current;

        private string _title;

        public TimeStamper() : this("") {  }
        public TimeStamper(string title) { _title = title; }

        public void Pause() => _sw.Stop();
        public void Resume() => _sw.Start();

        public void Merge(TimeStamper other) => _timeStamps.AddRange(other._timeStamps);
        public void Start(string name)
        {
            Stamp();
            _current = new TimeStamp(name);
            _sw.Restart();
        }

        public TimeStamp Stamp()
        {
            if(_current == null) { return null; }
            _sw.Stop();
            TimeStamp stamp = _current;

            stamp.seconds = _sw.Elapsed.TotalSeconds;
            stamp.ms = _sw.ElapsedMilliseconds;
            stamp.ticks = _sw.ElapsedTicks;

            _timeStamps.Add(stamp);

            _current = null;
            return stamp;
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
            long totMS = 0;
            long totTicks = 0;

            for (int i = 0; i < _timeStamps.Count; i++)
            {
                bool isNotLast = i < _timeStamps.Count - 1;
                string s = isNotLast ? "│ └──" : "  └──";
                string s2 = isNotLast ? "│" : "";
                sb.AppendLine($"{(isNotLast ? "├" : "└")}─┬{_timeStamps[i].name}");
                sb.AppendLine($"{s}{_timeStamps[i].ToString()}");
                sb.AppendLine(s2);

                totS += _timeStamps[i].seconds;
                totMS += _timeStamps[i].ms;
                totTicks += _timeStamps[i].ticks;
            }
            sb.AppendLine(new string('=', 64));
            sb.AppendLine($"Total Time Elapsed: {totS} secods, {totMS} ms, {totTicks} ticks");
            return sb.ToString();
        }

        public void Dispose()
        {
            Reset();
        }

        public class TimeStamp
        {
            public string name;

            public double seconds;
            public long ms;
            public long ticks;

            public TimeStamp(string name)
            {
                this.name = name;
            }

            public override string ToString() => $"{seconds} seconds, {ms} ms, {ticks} ticks";
            public string ToString(int padding) => $"{name.PadRight(padding, ' ')} => {seconds} seconds, {ms} ms, {ticks} ticks";
        }
    }
}
