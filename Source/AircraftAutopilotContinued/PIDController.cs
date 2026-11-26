// Aircraft Autopilot Continued — GPL-3.0-only.
// Adapted from MechJeb 2.15.4.0, f06a76e2bde14ec4d47794323376d73de72c604f.
// Copyright the MechJeb contributors; see THIRD_PARTY_NOTICES.md.
using System;

namespace AircraftAutopilotContinued
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
            return Compute(error, TimeWarp.fixedDeltaTime);
        }
        public double Compute(double error, double dt)
        {
            if (!Maths.Finite(error) || !Maths.Finite(dt) || dt <= 0 ||
                !Maths.Finite(Kp) || !Maths.Finite(Ki) || !Maths.Finite(Kd) || !Maths.Finite(INTAccum))
            { Reset(); return 0; }
            INTAccum += error * dt;
            double action = Kp * error + Ki * INTAccum + Kd * (error - _prevError) / dt;
            if (!Maths.Finite(action)) { Reset(); return 0; }
            double clamped = Math.Max(_min, Math.Min(_max, action));
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (clamped != action)
                INTAccum -= error * dt;

            _prevError = error;

            return action;
        }

        public void Reset() => _prevError = INTAccum = 0;

    }
}
