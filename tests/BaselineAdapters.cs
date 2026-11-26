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
    public class MechJebCore { public ThrustController Thrust = new ThrustController(); }
    public class ThrustController { public float TargetThrottle; public System.Collections.Generic.HashSet<object> Users = new System.Collections.Generic.HashSet<object>(); }
    public static class MuUtils {
        public static double Clamp(double x,double a,double b) => Maths.Clamp(x,a,b);
        public static double ClampDegrees180(double x) => Maths.ClampDegrees180(x);
    }
    public static class UtilMath { public static double Clamp(double x,double a,double b) => Maths.Clamp(x,a,b); }
}
