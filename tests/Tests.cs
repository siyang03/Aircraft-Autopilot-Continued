// GPL-3.0-only. Lightweight deterministic regression tests.
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using AircraftAutopilotContinued;
class Tests {
    static int checks;
    static void Check(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
    static void Near(double actual,double expected,string name,double tolerance=1e-9) => Check(Maths.Finite(actual) && Math.Abs(actual-expected)<=tolerance,name+": "+actual+" != "+expected);
    static AircraftAutopilotController Ready() {
        var c=new AircraftAutopilotController();
        for(int i=0;i<10;i++) c.VesselState.Sample(100,0,0,1000);
        c.SetEngaged(true); return c;
    }
    static void PID() {
        var p=new PIDController(2,3,4); Near(p.Compute(2,0.5),23,"P/I/D first step");
        Near(p.INTAccum,1,"integral first step"); Near(p.Compute(3,0.5),21.5,"P/I/D next step");
        p.Reset(); Near(p.INTAccum,0,"reset accumulator"); Near(p.Compute(2,0.5),23,"reset derivative");
        var bounded=new PIDController(2,3,4,1,-1); Near(bounded.Compute(2,0.5),23,"historical return is UNCLAMPED"); Near(bounded.INTAccum,0,"antiwindup rolls integration back");
        var zero=new PIDController(2,0,0); Near(zero.Compute(3,0.02),6,"zero Ki");
        foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity}) {
            Near(p.Compute(invalid,0.02),0,"invalid error"); Near(p.INTAccum,0,"invalid resets");
            Near(p.Compute(1,invalid),0,"invalid dt");
        }
        Near(p.Compute(1,0),0,"zero dt"); Near(p.Compute(1,-1),0,"negative dt");
        p.Kp=double.NaN; Near(p.Compute(1,0.02),0,"invalid gain");
        p.Kp=double.MaxValue; Near(p.Compute(double.MaxValue,1),0,"overflow protection");
        Near(Maths.InitialIntegral(.25f,.5),50,"bumpless initial integral"); Near(Maths.InitialIntegral(.25f,0),0,"zero Ki sync");
        Near(Maths.LimitIntegral(400,100,.5),200,"integral limit"); Near(Maths.LimitIntegral(-400,100,.5),-200,"negative integral limit");
        Near(Maths.LimitIntegral(400,100,0),0,"zero Ki limit");
    }
    static void Telemetry() {
        Near(Maths.ClampDegrees360(-1),359,"negative heading"); Near(Maths.ClampDegrees360(721),1,"multi wrap");
        Near(Maths.ClampDegrees180(1-359),2,"north positive"); Near(Maths.ClampDegrees180(359-1),-2,"north negative"); Near(Maths.ClampDegrees180(-180),180,"historical antipode");
        Near(Maths.Pitch(350),10,"pitch up"); Near(Maths.Pitch(10),-10,"pitch down");
        Near(Maths.Roll(350),-10,"roll negative"); Near(Maths.Roll(10),10,"roll positive");
        var m=new MovingAverage(); m.Value=100; Near(m,10,"zero seeded 10 sample");
        for(int i=1;i<10;i++)m.Value=100; Near(m,100,"full moving average");m.Value=0;Near(m,90,"ring eviction");
        var t=new AircraftTelemetry(); t.Sample(100,10,20,1000);
        Near(t.speedSurface,10,"speed smoothing");Near(t.speedVertical,1,"vertical smoothing");Near(t.vesselPitch,2,"pitch smoothing");Near(t.altitudeASL,100,"altitude smoothing");
        t.currentPitch=25;Near(t.currentPitch,25,"instantaneous pitch independent");
        t.north=new Vector3d(0,0,1);t.east=new Vector3d(1,0,0);
        Near(t.HeadingFromDirection(new Vector3d(1,0,0)),90,"east heading");Near(t.HeadingFromDirection(new Vector3d(-1,0,0)),270,"west heading");
        bool threw=false;try{t.Sample(double.NaN,0,0,0);}catch(InvalidOperationException){threw=true;}Check(threw,"invalid telemetry rejected");
    }
    static void Modes() {
        var c=new AircraftAutopilotController();Check(!c.Enabled && !c.HeadingHoldEnabled && !c.RollHoldEnabled && !c.VertSpeedHoldEnabled && !c.AltitudeHoldEnabled && !c.SpeedHoldEnabled,"all original defaults OFF");
        Near(c.HeadingTarget,90,"heading default");Near(c.BankAngle,30,"bank default");
        var state=new FlightCtrlState{pitch=.2f,roll=.3f,yaw=.4f,mainThrottle=.6f};c.Drive(state);Near(state.pitch,.2f,"disabled pitch");Near(state.mainThrottle,.6f,"disabled throttle");
        c=Ready();c.Drive(state);Near(state.roll,.3f,"inactive roll");Near(state.yaw,.4f,"inactive yaw");Near(state.pitch,.2f,"inactive pitch");Near(state.mainThrottle,.6f,"inactive throttle");
        c.EnableHeadingHold();Check(c.RollHoldEnabled && c.HeadingHoldEnabled,"heading enables roll");c.DisableHeadingHold();Check(!c.RollHoldEnabled && !c.HeadingHoldEnabled,"heading disables roll");
        c.EnableAltitudeHold();Check(c.VertSpeedHoldEnabled && c.AltitudeHoldEnabled,"altitude enables vertical");c.DisableAltitudeHold();Check(!c.VertSpeedHoldEnabled && !c.AltitudeHoldEnabled,"altitude disables vertical");
        c.RollHoldEnabled=true;c.RollTarget=80;c.BankAngle=30;c.RollLimit.Val=45;c.Drive(state);Near(c.RealRollTarget,30,"bank limit");Check(state.roll>0,"positive bank command");Near(state.yaw,.4f,"standalone roll no yaw");
        c.RollTarget=-80;c.Drive(state);Near(c.RealRollTarget,-30,"negative bank limit");Check(state.roll<0,"negative bank command");
        c.RollLimit.Val=15;c.Drive(state);Near(c.RealRollTarget,-15,"control roll limit");
        c.VesselState.currentRoll=-20;c.RollTarget=20;c.BankAngle=30;c.RollLimit.Val=45;c.Drive(state);Near(c.RollErr,0,"original opposite telemetry roll sign");
        c=Ready();c.HeadingTarget=1;c.VesselState.currentHeading=359;c.EnableHeadingHold();c.Drive(state);Near(c.RealRollTarget,4,"heading across north bank");
        c.HeadingTarget=359;c.VesselState.currentHeading=1;c.Drive(state);Near(c.RealRollTarget,-4,"reverse across north bank");
        c.HeadingTarget=1.1;c.Drive(state);Near(c.RealRollTarget,0,"fine heading bank zero");Near(c.RealYawTarget,.2,"fine heading yaw");
        c=Ready();c.SpeedTarget=120;c.EnableSpeedHold();c.Drive(state);Check(state.mainThrottle>=0&&state.mainThrottle<=1,"speed throttle bounded");float last=state.mainThrottle;c.DisableSpeedHold();c.Drive(state);Near(state.mainThrottle,last,"speed off leaves last throttle");state.mainThrottle=.42f;c.Drive(state);Near(state.mainThrottle,.42f,"speed off respects player throttle");
        c=Ready();c.EnableHeadingHold();c.EnableVertSpeedHold();c.EnableSpeedHold();c.PitKi.Val=0;c.RolKi.Val=0;c.YawKi.Val=0;c.AccKi.Val=0;c.Drive(state);Check(Maths.Finite(state.pitch)&&Maths.Finite(state.roll)&&Maths.Finite(state.yaw)&&Maths.Finite(state.mainThrottle),"all zero Ki safe");
        var stopped=Ready();for(int i=0;i<10;i++)stopped.VesselState.Sample(0,0,0,0);stopped.EnableVertSpeedHold();stopped.VertSpeedTarget=10;stopped.Drive(state);Near(stopped.RealPitchTarget,0,"zero airspeed pitch protection");
        TimeWarp.fixedDeltaTime=0;state.roll=.7f;c.Drive(state);Near(state.roll,.7f,"invalid dt no writes");TimeWarp.fixedDeltaTime=.02f;
        object a=new object(), b=new object();c=Ready();Check(c.TrySetBank(a,50),"external acquire");Near(c.RollTarget,30,"external bounded");Check(!c.TrySetBank(b,0),"external exclusivity");Check(!c.TrySetBank(a,double.NaN),"external invalid rejected");c.ReleaseExternalControl(b);Check(c.RollHoldEnabled,"nonowner release ignored");c.ReleaseExternalControl(a);Check(!c.RollHoldEnabled,"owner releases roll");c.SetEngaged(false);Check(!c.TrySetBank(a,0),"disengaged external refused");
    }
    static void Numeric() {
        double[] gains={0.005,0.0001,0.02,0.5,123.456};
        string[] displayed={"0.005","0.0001","0.02","0.5","123.456"};
        for(int i=0;i<gains.Length;i++) {
            var gain=new EditableDouble(gains[i]);
            Check(gain.Text==displayed[i],"exact initial gain display "+displayed[i]);
            var roundTrip=new EditableDouble(0);roundTrip.Text=gain.Text;
            Check(roundTrip.Valid&&BitConverter.DoubleToInt64Bits(roundTrip.Val)==BitConverter.DoubleToInt64Bits(gains[i]),"gain display roundtrip "+displayed[i]);
        }
        foreach(string text in new[]{"5e-3","1E-4","-1.25e2","1.2345678901234567"}) {
            var gain=new EditableDouble(0);gain.Text=text;
            double expected=double.Parse(text,System.Globalization.CultureInfo.InvariantCulture);
            Check(gain.Valid&&gain.Text==text&&BitConverter.DoubleToInt64Bits(gain.Val)==BitConverter.DoubleToInt64Bits(expected),"unrounded numeric editing "+text);
        }
        var culture=System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("fr-FR");
            var gain=new EditableDouble(0.005);Check(gain.Text=="0.005","invariant gain display");
            gain.Text="1e-4";Check(gain.Valid&&gain.Val==0.0001,"invariant exponent parsing");
        } finally {System.Globalization.CultureInfo.CurrentCulture=culture;}
        var f=new EditableDouble(12.5); f.Text="-"; Check(!f.Valid,"incomplete number rejected"); Near(f.Val,12.5,"last value retained");
        f.Text="NaN";Check(!f.Valid,"NaN numeric text rejected");Near(f.Val,12.5,"NaN retains value");
        f.Text="Infinity";Check(!f.Valid,"infinity numeric text rejected");
        f.Text="-1.25e2";Check(f.Valid,"finite exponent accepted");Near(f.Val,-125,"numeric parsed");
        f.Val=25;Check(f.Valid&&f.Text=="25","programmatic numeric value updates text");
        f.Val=double.NaN;Near(f.Val,25,"invalid programmatic numeric ignored");
    }
    static void Ownership() {
        var o=new CallbackOwnership<object>();object a=new object(),b=new object();int added=0,removed=0;
        Action<object> add=x=>added++;Action<object> remove=x=>removed++;
        o.Attach(a,add,remove);o.Attach(a,add,remove);Check(added==1&&removed==0,"one callback per owner");
        o.Attach(b,add,remove);Check(added==2&&removed==1&&o.Owner==b,"switch detaches previous");
        o.Detach(remove);o.Detach(remove);Check(removed==2&&o.Owner==null,"idempotent teardown");
        try{o.Attach(a,x=>throw new Exception("attach fail"),remove);}catch{}Check(o.Owner==null&&removed==3,"exception-safe attach teardown");
    }
    static void Differential() {
        // Compare every output to historical controller method bodies across changing flight samples.
        for(int scenario=0;scenario<6;scenario++) {
            var c=new AircraftAutopilotController();var core=new MuMech.MechJebCore();var h=new MuMech.MechJebModuleAirplaneAutopilot(core);
            h.OnStart(PartModule.StartState.Flying);h.Enabled=true;c.SetEngaged(true);
            c.SpeedTarget=h.SpeedTarget=120;c.AltitudeTarget=h.AltitudeTarget=1200;c.VertSpeedTarget=h.VertSpeedTarget=8;c.HeadingTarget=h.HeadingTarget=1;c.RollTarget=h.RollTarget=-20;
            if(scenario==0||scenario==5){c.EnableHeadingHold();h.EnableHeadingHold();}
            if(scenario==1||scenario==5){c.EnableAltitudeHold();h.EnableAltitudeHold();}
            if(scenario==2){c.EnableVertSpeedHold();h.EnableVertSpeedHold();}
            if(scenario==3||scenario==5){c.EnableSpeedHold();h.EnableSpeedHold();}
            if(scenario==4){c.RollHoldEnabled=h.RollHoldEnabled=true;}
            for(int step=0;step<200;step++) {
                double speed=100+10*Math.Sin(step*.04), vertical=2*Math.Sin(step*.03),pitch=5*Math.Sin(step*.02),altitude=1000+step;
                foreach(var t in new[]{c.VesselState,h.VesselState}) {
                    t.Sample(speed,vertical,pitch,altitude);t.currentRoll=10*Math.Sin(step*.03);t.currentHeading=Maths.ClampDegrees360(355+step*.05);
                    t.north=new Vector3d(0,0,1);t.east=new Vector3d(1,0,0);t.forward=new Vector3d(.01,0,1);t.surfaceVelocity=new Vector3d(.02,0,1);
                }
                var a=new FlightCtrlState{pitch=.15f,roll=-.1f,yaw=.05f,mainThrottle=.4f};var b=new FlightCtrlState{pitch=a.pitch,roll=a.roll,yaw=a.yaw,mainThrottle=a.mainThrottle};
                c.Drive(a);h.Drive(b);if(h.SpeedHoldEnabled)b.mainThrottle=core.Thrust.TargetThrottle;
                Near(a.pitch,b.pitch,"historical pitch");Near(a.roll,b.roll,"historical roll");Near(a.yaw,b.yaw,"historical yaw");Near(a.mainThrottle,b.mainThrottle,"historical throttle");
                Near(c.RealPitchTarget,h.RealPitchTarget,"historical pitch demand");Near(c.RealRollTarget,h.RealRollTarget,"historical bank demand");Near(c.RealVertSpeedTarget,h.RealVertSpeedTarget,"historical vertical demand");
            }
        }
    }
    static void Metadata(string root) {
        using var stream=File.OpenRead(Path.Combine(root,"build/AircraftAutopilotContinued.dll"));using var pe=new PEReader(stream);var r=pe.GetMetadataReader();
        var refs=r.AssemblyReferences.Select(x=>r.GetString(r.GetAssemblyReference(x).Name)).ToArray();
        Check(refs.Contains("Assembly-CSharp")&&refs.Contains("UnityEngine.CoreModule"),"compiled against real KSP/Unity");
        Check(!refs.Any(x=>x.Contains("MechJeb")||x.Contains("Harmony")||x.Contains("Kramax")),"no prohibited assembly dependencies");
        Check(!refs.Contains("System.Private.CoreLib")&&!refs.Contains("System.Runtime"),"Mono framework target");
        Check(!r.TypeReferences.Any(x=>r.GetString(r.GetTypeReference(x).Name)=="KSPActionGroup"),"plugin does not read/write SAS action groups");
        Console.WriteLine("Plugin references: "+string.Join(", ",refs));
        Check(r.GetString(r.GetAssemblyDefinition().Name)=="AircraftAutopilotContinued","unique assembly name");
    }
    static int Main(string[] args) { try{PID();Telemetry();Modes();Numeric();Ownership();ConflictTests.Run(Check);WindowSizingTests.Run(Check);Differential();Metadata(args[0]);Console.WriteLine("PASS: "+checks+" assertions (including 1,200 historical differential steps). Native flight/GUI not tested.");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;} }
}
