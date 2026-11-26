// Aircraft Autopilot Continued — GPL-3.0-only.
// Adapted from MechJeb 2.15.4.0, f06a76e2bde14ec4d47794323376d73de72c604f.
// Copyright the MechJeb contributors; see THIRD_PARTY_NOTICES.md.
namespace AircraftAutopilotContinued
{
    public class MovingAverage
    {
        private readonly double[] _store;
        private readonly int      _storeSize;
        private          int      _nextIndex;

        public double Value
        {
            get
            {
                double tmp = 0;
                for (int i = 0; i < _store.Length; i++)
                {
                    tmp += _store[i];
                }

                return tmp / _storeSize;
            }
            set
            {
                _store[_nextIndex] = value;
                _nextIndex         = (_nextIndex + 1) % _storeSize;
            }
        }

        public MovingAverage(int size = 10, double startingValue = 0)
        {
            _storeSize = size;
            _store     = new double[size];
            Force(startingValue);
        }

        private void Force(double newValue)
        {
            for (int i = 0; i < _storeSize; i++)
            {
                _store[i] = newValue;
            }
        }

        public static implicit operator double(MovingAverage v) => v.Value;

        public override string ToString() => Value.ToString();

        public string ToString(string format) => Value.ToString(format);
    }

}
