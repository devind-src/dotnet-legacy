using System;

namespace SyncNet.Networking
{
    internal class RetryPolicyDelay
    {
        // Array jeda dalam satuan detik (ideal & bertahap)
        private readonly int[] _delaySequenceSecond = { 5, 10, 15, 20, 25, 30 };
        private int _sequenceIndex = 0;

        public TimeSpan GetCurrentDelaySeconds()
        {
            // Proteksi tambahan: mencegah indeks keluar dari batas array
            if (_sequenceIndex >= _delaySequenceSecond.Length)
            {
                _sequenceIndex = _delaySequenceSecond.Length - 1; // Kunci di max value
            }

            if (_sequenceIndex < 0)
            {
                _sequenceIndex = 0; // Jaga di min value
            }

            return TimeSpan.FromSeconds(_delaySequenceSecond[_sequenceIndex]);
        }

        public void Increment()
        {
            if (_sequenceIndex < _delaySequenceSecond.Length - 1)
            {
                _sequenceIndex++;
            }
        }

        public void Reset()
        {
            _sequenceIndex = 0;
        }
    }
}
