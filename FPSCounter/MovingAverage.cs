using System.Collections.Generic;

namespace FPSCounter
{
    internal class MovingAverage
    {
        public static int WindowSize = 11;
        private readonly Queue<long> _samples;
        private long _sampleAccumulator;

        public MovingAverage()
        {
            _samples = new Queue<long>(WindowSize + 1);
        }

        public long GetAverage()
        {
            return _sampleAccumulator / _samples.Count;
        }

        public void Sample(long newSample)
        {
            _sampleAccumulator += newSample;
            _samples.Enqueue(newSample);

            if (_samples.Count > WindowSize)
                _sampleAccumulator -= _samples.Dequeue();
        }
    }
}