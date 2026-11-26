// Test-only managed API shapes. No emulation of Unity flight physics or quaternion native calls.
using System;
public static class TimeWarp { public static float fixedDeltaTime = 0.02f; }
public class FlightCtrlState { public float mainThrottle, pitch, roll, yaw; }
public class Vessel {
    public Vector3d CoMD, north, east, obt_velocity;
    public CelestialBody mainBody;
    public UnityEngine.Transform GetTransform() => throw new NotSupportedException("Native telemetry requires KSP");
}
public class CelestialBody {
    public Vector3d position;
    public Vector3d getRFrmVel(Vector3d p) => throw new NotSupportedException();
    public double GetAltitude(Vector3d p) => throw new NotSupportedException();
}
public struct Vector3d {
    public double x,y,z;
    public Vector3d(double x,double y,double z) { this.x=x; this.y=y; this.z=z; }
    public double magnitude => Math.Sqrt(x*x+y*y+z*z);
    public Vector3d normalized => magnitude == 0 ? this : new Vector3d(x/magnitude,y/magnitude,z/magnitude);
    public static Vector3d operator -(Vector3d a,Vector3d b) => new Vector3d(a.x-b.x,a.y-b.y,a.z-b.z);
    public static double Dot(Vector3d a,Vector3d b) => a.x*b.x+a.y*b.y+a.z*b.z;
    public static implicit operator UnityEngine.Vector3(Vector3d v) => new UnityEngine.Vector3 { x=(float)v.x,y=(float)v.y,z=(float)v.z };
    public static implicit operator Vector3d(UnityEngine.Vector3 v) => new Vector3d(v.x,v.y,v.z);
}
namespace UnityEngine {
    public static class Time { public static float fixedDeltaTime = 0.02f; }
    public static class Mathf { public static float Clamp(float x,float a,float b) => Math.Max(a,Math.Min(b,x)); }
    public struct Vector3 { public float x,y,z; }
    public struct Quaternion {
        public Vector3 eulerAngles => throw new NotSupportedException();
        public static Quaternion LookRotation(Vector3 n,Vector3 u) => throw new NotSupportedException();
        public static Quaternion Inverse(Quaternion q) => throw new NotSupportedException();
        public static Quaternion Euler(float x,float y,float z) => throw new NotSupportedException();
        public static Quaternion operator *(Quaternion a,Quaternion b) => throw new NotSupportedException();
    }
    public class Transform { public Vector3 up; public Quaternion rotation; }
}
