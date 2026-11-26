// GPL-3.0-only. Angle helpers adapted from MechJeb 2.15.4.0.
using System;
namespace AircraftAutopilotContinued
{
    public static class Maths
    {
        public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
        public static double Clamp(double x, double min, double max) => Math.Max(min, Math.Min(max, x));
        public static double ClampDegrees360(double x) { x %= 360; return x < 0 ? x + 360 : x; }
        public static double ClampDegrees180(double x) { x = ClampDegrees360(x); return x > 180 ? x - 360 : x; }
        public static double Pitch(double x) => x > 180 ? 360 - x : -x;
        public static double Roll(double z) => z > 180 ? z - 360 : z;
        public static double InitialIntegral(float control, double ki) => ki == 0 ? 0 : control * 100 / ki;
        public static double LimitIntegral(double integral, double scale, double ki)
            => ki == 0 ? 0 : Clamp(integral, -Math.Abs(scale / ki), Math.Abs(scale / ki));
    }
}
