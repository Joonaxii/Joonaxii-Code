using System;

namespace Joonaxii.Data.Coding
{
    public abstract class CodecBase : IDisposable
    {
        public const int PROGRESS_INTERVAL = 32;
        protected bool HasProgressListeners { get => _onProgress != null; }

        protected Action _onBegin;
        protected Action<ProgressData> _onProgress;
        protected Action _onFinished;

        protected int _progressInterval = PROGRESS_INTERVAL;
        protected int _progressEvents = 0;

        public abstract void Dispose();

        public void SetProgressInterval(int interval)
        {
            _progressInterval = interval;
        }

        protected bool TriggerProgress()
        {
            if (!HasProgressListeners) { return false; }
            return (_progressEvents++ % _progressInterval) == 0;
        }

        protected void RaiseOnProgress(ref ProgressData progress) => _onProgress?.Invoke(progress);
        public void AddOnProgressListener(Action<ProgressData> act) => _onProgress += act;
        public void RemoveOnProgressListener(Action<ProgressData> act)
        {
            if (_onProgress == null) { return; }
            _onProgress -= act;
        }

        protected void RaiseOnBegin() => _onBegin?.Invoke();
        public void AddOnBeginListener(Action act) => _onBegin += act;
        public void RemoveOnBeginListener(Action act)
        {
            if(_onBegin == null) { return; }
            _onBegin -= act;
        }

        protected void RaiseOnFinished() => _onFinished?.Invoke();
        public void AddOnFinishedListener(Action act) => _onBegin += act;
        public void RemoveOnFinishedListener(Action act)
        {
            if (_onBegin == null) { return; }
            _onBegin -= act;
        }
    }
}
