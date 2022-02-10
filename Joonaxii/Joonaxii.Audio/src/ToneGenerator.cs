using Joonaxii.Collections;
using System;

namespace Joonaxii.Audio
{
    public class ToneGenerator
    {
        public const int DEFAULT_FLUSH_THRESHOLD = 1024;
        private Action _onFlush;
        private BufferList _samples;

        private bool _stereo;
        private double _rateS;
        private double _rateOPI;
        private int _sampleRate;

        private double _gTheta;
        private double _tOffset;

        private int _flushThreshold;
        private bool _isSigned;

        public ToneGenerator(bool signed, BufferList samples, bool stereo, int sampleRate, Action onFlush = null, int flushTreshold = DEFAULT_FLUSH_THRESHOLD)
        {
            _isSigned = signed;
            _flushThreshold = flushTreshold;
            _onFlush = onFlush;

            _samples = samples;
            _stereo = stereo;
            _sampleRate = sampleRate;

            _rateS = 10000000.0 / sampleRate;
            _rateOPI = (2.0 * Math.PI / _sampleRate);

            _tOffset = 0;
            _gTheta = 0;
        }

        public void Generate(double frequency, double amplitude, double duration, double startFade = 0, double endFade = 0)
        {
            duration *= 10000;
            startFade *= 10000;
            endFade *= 10000;

            duration += _tOffset;
            int toneSamples = (int)Math.Floor((duration / _rateS) + 0.5);
            int startSamples = frequency == 0 ? 0 : (int)Math.Floor((startFade / _rateS) + 0.5);
            int endSamples = frequency == 0 ? 0 : (int)Math.Floor((endFade / _rateS) + 0.5);
            double deltaTheta = _rateOPI * frequency;

            for (int i = 0; i < toneSamples; i++)
            {
                if (frequency == 0)
                {
                    _samples.Add(0.5, _isSigned);
                    if (_stereo) { _samples.Add(0.5, _isSigned); }
                    CheckForFlush();
                    continue;
                }

                double n = startSamples < 1 ? 1.0 : (i / (startSamples < 2 ? 1.0 : startSamples - 1.0));
                n = (n > 1.0 ? 1.0 : n);
                double voltage = (Math.Sin(_gTheta) * (amplitude * n * n * n)); ;
        
                _samples.Add((voltage + 1.0) * 0.5, _isSigned);
                if (_stereo) { _samples.Add(voltage, _isSigned); }
                CheckForFlush();
                _gTheta += deltaTheta;
            }

            if (frequency > 0)
            {
                for (int i = 0; i < endSamples; i++)
                {
                    double n = 1.0 - (i / (endSamples < 2 ? 1.0 : endSamples - 1.0)); 
                    double voltage = (Math.Sin(_gTheta) * (amplitude * n * n * n));

                    _samples.Add((voltage + 1.0) * 0.5, _isSigned);
                    if (_stereo) { _samples.Add(voltage, _isSigned); }
                    CheckForFlush();
                    _gTheta += deltaTheta;
                }
            }
            _tOffset = duration - (toneSamples * _rateS);
        }

        private void CheckForFlush()
        {
            if(_onFlush != null && _samples.Count >= _flushThreshold)
            {
                _onFlush.Invoke();
            }
        }
    }
}
