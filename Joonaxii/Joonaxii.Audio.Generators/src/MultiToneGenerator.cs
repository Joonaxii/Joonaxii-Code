using Joonaxii.Collections;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Audio.Generators
{
    public class MultiToneGenerator
    {
        private BufferList _samples;
        private bool _stereo;
        private double _rateS;
        private double _rateOPI;
        private int _sampleRate;

        private List<double>[][] _tracks;
        private double[] _gTheta;
        private double[] _tOffset;
        private int _trackCount;

        public MultiToneGenerator(BufferList samples, bool stereo, int sampleRate, int trackCount)
        {
            _samples = samples;
            _stereo = stereo;
            _sampleRate = sampleRate;

            _rateS = 10000000.0 / sampleRate;
            _rateOPI = (2 * Math.PI / _sampleRate);
            _tracks = new List<double>[stereo ? 2 : 1][];

            _trackCount = trackCount;

            _tOffset = new double[_trackCount];
            _gTheta = new double[_trackCount];
            for (int i = 0; i < _tracks.Length; i++)
            {
                _tracks[i] = new List<double>[trackCount];
                for (int j = 0; j < trackCount; j++)
                {
                    _tracks[i][j] = new List<double>();
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _tracks.Length; i++)
            {
                var trckCH = _tracks[i];
                for (int j = 0; j < _trackCount; j++)
                {
                    trckCH[j].Clear();
                }
            }
        }

        public void Generate(double frequency, double amplitude, double duration, double startFade = 0, double endFade = 0) => Generate(-1, frequency, amplitude, duration, startFade, endFade);
        public void Generate(int track, double frequency, double amplitude, double duration, double startFade = 0, double endFade = 0)
        {        
            if(track < 0)
            {
                for (int i = 1; i < _trackCount; i++)
                {
                    Generate(i, 0, amplitude, duration, startFade, endFade);
                }
                track = 0;
            }

            track = track % _trackCount;

            duration *= 10000;
            startFade *= 10000;
            endFade *= 10000;

            duration += _tOffset[track];
            int toneSamples = (int)Math.Floor((duration / _rateS) + 0.5);
            int startSamples = frequency == 0 ? 0 : (int)Math.Floor((startFade / _rateS) + 0.5);
            int endSamples = frequency == 0 ? 0 : (int)Math.Floor((endFade / _rateS) + 0.5);
            double deltaTheta = _rateOPI * frequency;

            var trckL = _tracks[0][track];
            var trckR = _stereo ? _tracks[1][track] : null;

            for (int i = 0; i < toneSamples; i++)
            {
                if(frequency == 0)
                {
                    trckL.Add(0.0);
                    if (_stereo) { trckR.Add(0.0); }
                    continue;
                }

                double n = startSamples < 1 ? 1.0 : (i / (startSamples < 2 ? 1.0 : startSamples - 1.0));
                n = (n > 1.0 ? 1.0 : n);
                double voltage;
                double theth = _gTheta[track];
                switch (track) 
                {
                    default: voltage = (Math.Sin(theth) * (amplitude * n * n * n)); break;
                    case 1: voltage = (Maths.Triangle((float)(theth + Math.PI)) * (amplitude * n * n * n)); break;
                    case 2: voltage = (Math.Sin(theth) * (amplitude * n * n * n)); break;
                }

                trckL.Add(voltage);
                if (_stereo) { trckR.Add(voltage); }
                _gTheta[track] += deltaTheta;
            }

            if(frequency > 0)
            {
                for (int i = 0; i < endSamples; i++)
                {
                    double n = 1.0 - (i / (endSamples < 2 ? 1.0 : endSamples - 1.0));
                    double voltage;
                    double theth = _gTheta[track];
                    switch (track)
                    {
                        default: voltage = (Math.Sin(theth) * (amplitude * n * n * n)); break;
                        case 1: voltage = (Maths.Triangle((float)(theth + Math.PI)) * (amplitude * n * n * n)); break;
                        case 2: voltage = (Math.Sin(theth) * (amplitude * n * n * n)); break;
                    }
                    trckL.Add(voltage);
                    if (_stereo) { trckR.Add(voltage); }
                    _gTheta[track] += deltaTheta;
                }
            }
            _tOffset[track] = duration - (toneSamples * _rateS);
        }

        public void Combine(double maxVolume)
        {
            int maxLen = 0;
            foreach (var track in _tracks[0])
            {
                maxLen = track.Count < maxLen ? maxLen : track.Count;
            }

            double[] tones = new double[maxLen * (_stereo ? 2 : 1)];

            double maxL = 0.000001;
            double maxR = 0.000001;

            for (int k = 0; k < maxLen; k += _stereo ? 2 : 1)
            {
                double valL = 0.0;
                double valR = 0.0;

                double trackC = 0;
                for (int i = 0; i < _trackCount; i++)
                {
                    var tracKL = _tracks[0][i];
                    
                    if(tracKL.Count < 1) { continue; }
                    trackC++;
                    int kL = _stereo ? k / 2 : k;

                    if(tracKL.Count > kL) 
                    {
                        var prev = valL;
                        valL += tracKL[kL];
                        //if (valL > 1) { System.Diagnostics.Debug.Print($"[{prev} + {tracKL[kL]} => {valL}] {k}"); }
                    }
                    if (_stereo)
                    {
                        List<double> tracKR = _tracks[1][i];
                        if (tracKR.Count > kL) { valR += tracKR[kL]; }
                    }
                }

                tones[k] = valL;
                valL = Math.Abs(valL);
                maxL = valL > maxL ? valL : maxL;

            

                if (_stereo)
                {
                    tones[k + 1] = valR;
                    valR = Math.Abs(valR);
                    maxR = valR > maxR ? valR : maxR;
                }
            }

            for (int i = 0; i < tones.Length; i += _stereo ? 2 : 1)
            {
                _samples.Add((tones[i] / maxL * maxVolume + 1.0) * 0.5, false);
                if (_stereo)
                {
                    _samples.Add((tones[i + 1] / maxR * maxVolume + 1.0) * 0.5, false);
                }
            }
        }
    }
}
