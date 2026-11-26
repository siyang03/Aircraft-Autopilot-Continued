// GPL-3.0-only. MechJeb 2.15.4.0 first PID class; persistence methods omitted.
using System;

namespace MuMech
{
    public class PIDController
    {
        private          double _prevError;
        public           double INTAccum, Kp, Ki, Kd;
        private readonly double _max;
        private readonly double _min;

        public PIDController(double kp = 0, double ki = 0, double kd = 0, double max = double.MaxValue, double min = double.MinValue)
        {
            Kp   = kp;
            Ki   = ki;
            Kd   = kd;
            _max = max;
            _min = min;
            Reset();
        }

        public double Compute(double error)
        {
            INTAccum += error * TimeWarp.fixedDeltaTime;
            double action = Kp * error + Ki * INTAccum + Kd * (error - _prevError) / TimeWarp.fixedDeltaTime;
            double clamped = Math.Max(_min, Math.Min(_max, action));
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (clamped != action)
                INTAccum -= error * TimeWarp.fixedDeltaTime;

            _prevError = error;

            return action;
        }

        public void Reset() => _prevError = INTAccum = 0;

    }
}
