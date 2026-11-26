// Test-only minimal adapters for the historical MechJeb controller fixture.
using AircraftAutopilotContinued;
public class PartModule { public enum StartState { Flying } }
namespace MuMech {
    public class ComputerModule {
        public AircraftTelemetry VesselState = new AircraftTelemetry();
        public MechJebCore Core;
        public bool Enabled;
        public ComputerModule(MechJebCore core) { Core=core; }
        public virtual void OnStart(PartModule.StartState state) {}
        protected virtual void OnModuleEnabled() {}
        protected virtual void OnModuleDisabled() {}
        public virtual void Drive(FlightCtrlState state) {}
    }
    public class MechJebCore {
        public ThrustController Thrust = new ThrustController();
        public object Attitude, Aircraft;
        public object GetComputerModule(string name) => Aircraft;
        public void Hook(FlightCtrlState state) { }
    }
    public class ThrustController { public float TargetThrottle;
        public bool Enabled;
        public enum TMode { OFF, DIRECT }
        public TMode Tmode;
        public System.Collections.Generic.List<object> Users = new System.Collections.Generic.List<object>(); }
    public static class MuUtils {
        public static double Clamp(double x,double a,double b) => Maths.Clamp(x,a,b);
        public static double ClampDegrees180(double x) => Maths.ClampDegrees180(x);
    }
    public static class UtilMath { public static double Clamp(double x,double a,double b) => Maths.Clamp(x,a,b); }
}
