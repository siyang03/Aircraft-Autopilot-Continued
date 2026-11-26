// GPL-3.0-only. Exercise the production reflection detector with small optional-mod doubles.
using System;
using System.Collections.Generic;
using AircraftAutopilotContinued;
namespace Kramax {
    public class George {
        public bool HrztActive, VertActive, ThrtActive;
        public void Pre(FlightCtrlState s) { }
        public void Post(FlightCtrlState s) { }
    }
}
static class ConflictTests {
    class Active { public bool Enabled = true; }
    class IdleAuxiliary { public void Hook(FlightCtrlState s) { } }
    class MissingUsers { public bool Enabled = true; public MuMech.ThrustController.TMode Tmode = MuMech.ThrustController.TMode.OFF; }
    class MissingMode { public bool Enabled = true; public List<object> Users = new List<object>(); }
    class PropertyThrust {
        public bool Enabled => true;
        public List<object> Users { get; } = new List<object> { new object() };
        public MuMech.ThrustController.TMode Tmode => MuMech.ThrustController.TMode.OFF;
    }
    internal static void Run(Action<bool,string> check) {
        var messages = new List<string>();
        var detector = new ConflictDetector(messages.Add);
        Action<FlightCtrlState> idle = new IdleAuxiliary().Hook;
        detector.Refresh(new object[0], new Delegate[0]);
        check(detector.Check(false)==null,"MechJeb absent allowed");
        var mj = new MuMech.MechJebCore(); mj.Thrust.Enabled=true; mj.Thrust.Users.Add(mj.Thrust);
        detector.Refresh(new object[]{mj}, new Delegate[]{new Action<FlightCtrlState>(mj.Hook)});
        check(detector.Check(false)==null,"enabled MechJeb sole self-user OFF allowed");
        mj.Thrust.Users.Add(new object());
        check(detector.Check(false)?.Contains("additional user")==true,"MechJeb additional user refused without rediscovery");
        mj.Thrust.Users.RemoveAt(1); mj.Thrust.Tmode=MuMech.ThrustController.TMode.DIRECT;
        check(detector.Check(false)?.Contains("not OFF")==true,"MechJeb non-OFF mode refused");
        mj.Thrust.Tmode=MuMech.ThrustController.TMode.OFF;
        mj.Attitude=new Active(); check(detector.Check(false)?.Contains("attitude control is active")==true,"MechJeb attitude refused");
        mj.Attitude=null; mj.Aircraft=new Active(); check(detector.Check(false)?.Contains("aircraft autopilot is active")==true,"MechJeb aircraft refused");
        mj.Aircraft=null; check(detector.Check(true)?.Contains("SAS")==true,"stock SAS refused");
        detector.Refresh(new object[]{mj},new Delegate[]{idle});
        check(detector.Check(false)==null,"idle auxiliary callback allowed alongside idle MechJeb");
        check(messages.Count==1&&messages[0].Contains("does not establish active control"),"unknown callback diagnosed without fabricated conflict");
        detector.Refresh(new object[]{mj},new Delegate[]{idle});
        check(messages.Count==1,"unknown diagnostic deduplicated across refreshes");
        detector.Refresh(new object[0],new Delegate[]{idle});
        check(detector.Check(false)==null,"auxiliary callback allowed without MechJeb");
        var george=new Kramax.George();
        detector.Refresh(new object[0],new Delegate[]{new Action<FlightCtrlState>(george.Pre),new Action<FlightCtrlState>(george.Post)});
        check(detector.Check(false)==null,"idle Kramax pre/post callbacks allowed");
        george.HrztActive=true;check(detector.Check(false)?.Contains("Kramax flight control is active")==true,"active Kramax horizontal refused");
        george.HrztActive=false;george.VertActive=true;check(detector.Check(false)!=null,"active Kramax vertical refused");
        george.VertActive=false;george.ThrtActive=true;check(detector.Check(false)!=null,"active Kramax throttle refused");
        george.ThrtActive=false;check(detector.Check(false)==null,"Kramax live deactivation allowed without rediscovery");
        check(detector.CheckThrust(null)?.Contains("Thrust is unavailable")==true,"missing thrust reports uncertainty");
        check(detector.CheckThrust(new MissingUsers())?.Contains("Users.Count is unavailable")==true,"missing users reports specific uncertainty");
        check(detector.CheckThrust(new MissingMode())?.Contains("Tmode is unavailable")==true,"missing mode reports specific uncertainty");
        check(detector.CheckThrust(new PropertyThrust())==null,"public property reflection supported");
        detector.Refresh(new object[0],new Delegate[0]);check(detector.Check(false)==null,"refresh releases old optional controller references");
    }
}
