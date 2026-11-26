// Aircraft Autopilot Continued — GPL-3.0-only.
// Adapted from MechJeb 2.15.4.0, f06a76e2bde14ec4d47794323376d73de72c604f.
// Copyright the MechJeb contributors; see THIRD_PARTY_NOTICES.md.
using System;
using UnityEngine;

namespace AircraftAutopilotContinued
{
    public class AircraftAutopilotWindow
    {
        private static GUIStyle btNormal, btActive, btAuto, btGreen, btWhite;

        public AircraftAutopilotController autopilot;
        public AircraftTelemetry VesselState => autopilot.VesselState;
        private readonly AircraftAutopilotAddon host;
        public AircraftAutopilotWindow(AircraftAutopilotAddon host, AircraftAutopilotController controller)
        { this.host = host; autopilot = controller; }
        public bool showpid;

        public EditableDouble AltitudeTargettmp = 0,
            HeadingTargettmp                    = 90,
            RollTargettmp                       = 0,
            SpeedTargettmp                      = 0,
            VertSpeedTargettmp                  = 0,
            RollMaxtmp                          = 30;

        // Measure the longest required sample in the active stock style; never round its value.
        static string GainField(EditableDouble field)
        {
            float width = Math.Max(60, GUI.skin.textField.CalcSize(new GUIContent("123.456")).x + 4);
            return GUILayout.TextField(field.Text, GUILayout.Width(width));
        }
        static GUIStyle wrappedLabel;
        public void WindowGUI(int windowID)
        {
            if (wrappedLabel == null) wrappedLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
            if (btNormal == null)
            {
                btNormal                    = new GUIStyle(GUI.skin.button);
                btNormal.normal.textColor   = btNormal.focused.textColor   = Color.white;
                btNormal.hover.textColor    = btNormal.active.textColor    = Color.yellow;
                btNormal.onNormal.textColor = btNormal.onFocused.textColor = btNormal.onHover.textColor = btNormal.onActive.textColor = Color.green;
                //btNormal.padding = new RectOffset(8, 8, 8, 8);

                btActive           = new GUIStyle(btNormal);
                btActive.active    = btActive.onActive;
                btActive.normal    = btActive.onNormal;
                btActive.onFocused = btActive.focused;
                btActive.hover     = btActive.onHover;

                btGreen                  = new GUIStyle(btNormal);
                btGreen.normal.textColor = Color.green;
                btGreen.fixedWidth       = 35;

                btWhite                  = new GUIStyle(btNormal);
                btWhite.normal.textColor = Color.white;
                btWhite.fixedWidth       = 35;

                btAuto = new GUIStyle(btNormal);
                btAuto.padding = new RectOffset(8, 8, 8, 8);
                btAuto.normal.textColor = Color.red;
                btAuto.onActive = btAuto.onFocused = btAuto.onHover = btAuto.onNormal = btAuto.active = btAuto.focused = btAuto.hover = btAuto.normal;
            }

            if (autopilot.Enabled)
            {
                if (GUILayout.Button(string.Format("Disengage autopilot"), btActive))
                {
                    //Disengage autopilot
                    host.Disengage("Disengaged");
                }
            }
            else
            {
                if (GUILayout.Button(string.Format("Engage autopilot")))
                {
                    //Engage autopilot
                    host.Engage();
                }
            }

            GUILayout.BeginHorizontal();
            bool AltitudeHold = autopilot.AltitudeHoldEnabled;
            autopilot.AltitudeHoldEnabled = GUILayout.Toggle(autopilot.AltitudeHoldEnabled, string.Format("Altitude Hold"),
                GUILayout.Width(140)); //Altitude Hold
            if (AltitudeHold != autopilot.AltitudeHoldEnabled)
            {
                if (autopilot.AltitudeHoldEnabled)
                    autopilot.EnableAltitudeHold();
                else
                    autopilot.DisableAltitudeHold();
            }

            bool change = false;
            if (GUILayout.Button("-", GUILayout.Width(18)))
            {
                AltitudeTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change                =  true;
            }

            AltitudeTargettmp.Text = GUILayout.TextField(AltitudeTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            if (GUILayout.Button("+", GUILayout.Width(18)))
            {
                AltitudeTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change                =  true;
            }

            if (AltitudeTargettmp < 0)
                AltitudeTargettmp = 0;
            GUILayout.Label("m", GUILayout.ExpandWidth(true));
            if (change || GUILayout.Button(string.Format("Set"),
                    autopilot.AltitudeTarget == AltitudeTargettmp ? btWhite : btGreen))
            {
                //Set
                autopilot.AltitudeTarget = AltitudeTargettmp;
            }

            GUILayout.EndHorizontal();


            if (!autopilot.AltitudeHoldEnabled)
            {
                bool _VertSpeedHoldEnabled = autopilot.VertSpeedHoldEnabled;
                GUILayout.BeginHorizontal();
                autopilot.VertSpeedHoldEnabled = GUILayout.Toggle(autopilot.VertSpeedHoldEnabled, string.Format("Vertical Speed Hold"),
                    GUILayout.Width(140)); //Vertical Speed Hold
                if (_VertSpeedHoldEnabled != autopilot.VertSpeedHoldEnabled)
                {
                    if (autopilot.VertSpeedHoldEnabled)
                        autopilot.EnableVertSpeedHold();
                    else
                        autopilot.DisableVertSpeedHold();
                }

                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18)))
                {
                    VertSpeedTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change                 =  true;
                }

                VertSpeedTargettmp.Text = GUILayout.TextField(VertSpeedTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(18)))
                {
                    VertSpeedTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change                 =  true;
                }

                VertSpeedTargettmp = Math.Max(0, VertSpeedTargettmp);
                GUILayout.Label("m/s", GUILayout.ExpandWidth(true));
                if (change || GUILayout.Button(string.Format("Set"),
                        autopilot.VertSpeedTarget == VertSpeedTargettmp ? btWhite : btGreen))
                {
                    autopilot.VertSpeedTarget = VertSpeedTargettmp;
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("   V/S ±"), GUILayout.Width(140)); //"    V/S ±"
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18)))
                {
                    VertSpeedTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change                 =  true;
                }

                VertSpeedTargettmp.Text = GUILayout.TextField(VertSpeedTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(18)))
                {
                    VertSpeedTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change                 =  true;
                }

                VertSpeedTargettmp = Math.Max(0, VertSpeedTargettmp);
                GUILayout.Label("m/s", GUILayout.ExpandWidth(true));
                if (change || GUILayout.Button(string.Format("Set"),
                        autopilot.VertSpeedTarget == VertSpeedTargettmp ? btWhite : btGreen))
                {
                    autopilot.VertSpeedTarget = VertSpeedTargettmp;
                }

                GUILayout.EndHorizontal();
            }


            GUILayout.BeginHorizontal();
            bool _HeadingHoldEnabled = autopilot.HeadingHoldEnabled;
            autopilot.HeadingHoldEnabled = GUILayout.Toggle(autopilot.HeadingHoldEnabled, string.Format("Heading Hold"),
                GUILayout.Width(140)); //"Heading Hold"
            if (_HeadingHoldEnabled != autopilot.HeadingHoldEnabled)
            {
                if (autopilot.HeadingHoldEnabled)
                    autopilot.EnableHeadingHold();
                else
                    autopilot.DisableHeadingHold();
            }

            change = false;
            if (GUILayout.Button("-", GUILayout.Width(18)))
            {
                HeadingTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change               =  true;
            }

            HeadingTargettmp.Text = GUILayout.TextField(HeadingTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            if (GUILayout.Button("+", GUILayout.Width(18)))
            {
                HeadingTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change               =  true;
            }

            HeadingTargettmp = Maths.ClampDegrees360(HeadingTargettmp);
            GUILayout.Label("°", GUILayout.ExpandWidth(true));
            if (change || GUILayout.Button(string.Format("Set"),
                    autopilot.HeadingTarget == HeadingTargettmp ? btWhite : btGreen))
            {
                autopilot.HeadingTarget = HeadingTargettmp;
            }

            GUILayout.EndHorizontal();


            if (!autopilot.HeadingHoldEnabled)
            {
                GUILayout.BeginHorizontal();
                autopilot.RollHoldEnabled = GUILayout.Toggle(autopilot.RollHoldEnabled, string.Format("Roll Hold"),
                    GUILayout.Width(140)); //"Roll Hold"
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18)))
                {
                    RollTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change            =  true;
                }

                RollTargettmp.Text = GUILayout.TextField(RollTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(18)))
                {
                    RollTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change            =  true;
                }

                RollTargettmp = Maths.Clamp(RollTargettmp, -90, 90);
                GUILayout.Label("°", GUILayout.ExpandWidth(true));
                if (change || GUILayout.Button(string.Format("Set"),
                        autopilot.RollTarget == RollTargettmp ? btWhite : btGreen))
                {
                    autopilot.RollTarget = RollTargettmp;
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("    Bank angle ±"), GUILayout.Width(140)); //"    Roll Limit ±"
                change = false;
                if (GUILayout.Button("-", GUILayout.Width(18)))
                {
                    RollMaxtmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change         =  true;
                }

                RollMaxtmp.Text = GUILayout.TextField(RollMaxtmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(18)))
                {
                    RollMaxtmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                    change         =  true;
                }

                RollMaxtmp = Maths.Clamp(RollMaxtmp, -60, 60);
                GUILayout.Label("°", GUILayout.ExpandWidth(true));
                if (change || GUILayout.Button(string.Format("Set"),
                        autopilot.BankAngle == RollMaxtmp ? btWhite : btGreen))
                {
                    autopilot.BankAngle = RollMaxtmp;
                }

                GUILayout.EndHorizontal();
            }


            GUILayout.BeginHorizontal();
            bool _AutoThrustCtrl = autopilot.SpeedHoldEnabled;
            autopilot.SpeedHoldEnabled =
                GUILayout.Toggle(autopilot.SpeedHoldEnabled, string.Format("Speed Hold"), GUILayout.Width(140)); //Speed Hold
            if (autopilot.SpeedHoldEnabled != _AutoThrustCtrl)
            {
                if (autopilot.SpeedHoldEnabled)
                    autopilot.EnableSpeedHold();
                else
                    autopilot.DisableSpeedHold();
            }

            change = false;
            if (GUILayout.Button("-", GUILayout.Width(18)))
            {
                SpeedTargettmp.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change             =  true;
            }

            SpeedTargettmp.Text = GUILayout.TextField(SpeedTargettmp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
            if (GUILayout.Button("+", GUILayout.Width(18)))
            {
                SpeedTargettmp.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
                change             =  true;
            }

            if (SpeedTargettmp < 0)
                SpeedTargettmp = 0;
            GUILayout.Label("m/s", GUILayout.ExpandWidth(true));
            if (change || GUILayout.Button(string.Format("Set"),
                    autopilot.SpeedTarget == SpeedTargettmp ? btWhite : btGreen))
            {
                //Set
                autopilot.SpeedTarget = SpeedTargettmp;
            }

            GUILayout.EndHorizontal();


            if (!showpid)
            {
                if (GUILayout.Button(string.Format("PID"), GUILayout.Width(40)))
                {
                    //"PID"
                    showpid = true;
                }
            }
            else
            {
                if (GUILayout.Button(string.Format("Hide PID"), GUILayout.Width(140)))
                {
                    //Hide PID
                    showpid = false;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("Accceleration"), GUILayout.ExpandWidth(true)); //Accceleration
                GUILayout.Label("Kp", GUILayout.ExpandWidth(false));
                autopilot.AccKp.Text = GainField(autopilot.AccKp);
                GUILayout.Label("Ki", GUILayout.ExpandWidth(false));
                autopilot.AccKi.Text = GainField(autopilot.AccKi);
                GUILayout.Label("Kd", GUILayout.ExpandWidth(false));
                autopilot.AccKd.Text = GainField(autopilot.AccKd);
                GUILayout.EndHorizontal();
                if (autopilot.SpeedHoldEnabled)
                    GUILayout.Label(
                        string.Format("Error:{0} Target:{1} Cur:{2}", autopilot.AErr.ToString("F2"),
                            autopilot.RealAccelerationTarget.ToString("F2"), autopilot.CurAcc.ToString("F2")),
                        wrappedLabel, GUILayout.ExpandWidth(true)); //"error:"<<1>>" Target:"<<2>> " Cur:"<<3>>

                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("Pitch"), GUILayout.ExpandWidth(true)); //"VertSpeed"
                GUILayout.Label("Kp", GUILayout.ExpandWidth(false));
                autopilot.PitKp.Text = GainField(autopilot.PitKp);
                GUILayout.Label("Ki", GUILayout.ExpandWidth(false));
                autopilot.PitKi.Text = GainField(autopilot.PitKi);
                GUILayout.Label("Kd", GUILayout.ExpandWidth(false));
                autopilot.PitKd.Text = GainField(autopilot.PitKd);
                GUILayout.EndHorizontal();
                if (autopilot.VertSpeedHoldEnabled)
                    GUILayout.Label(string.Format("Error:{0} Target:{1} Cur:{2} PID: {3}", autopilot.PitchErr.ToString("F2"),
                        autopilot.RealPitchTarget.ToString("F2"), VesselState.currentPitch.ToString("F2"), autopilot.PitchAct.ToString("F5")), wrappedLabel,
                        GUILayout.ExpandWidth(true)); //error:" Target:"" Cur:"

                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("Roll"), GUILayout.ExpandWidth(true)); //Roll
                GUILayout.Label("Kp", GUILayout.ExpandWidth(false));
                autopilot.RolKp.Text = GainField(autopilot.RolKp);
                GUILayout.Label("Ki", GUILayout.ExpandWidth(false));
                autopilot.RolKi.Text = GainField(autopilot.RolKi);
                GUILayout.Label("Kd", GUILayout.ExpandWidth(false));
                autopilot.RolKd.Text = GainField(autopilot.RolKd);
                GUILayout.EndHorizontal();
                if (autopilot.RollHoldEnabled)
                    GUILayout.Label(string.Format("Error:{0} Target:{1} Cur:{2} PID: {3}", autopilot.RollErr.ToString("F2"),
                        autopilot.RealRollTarget.ToString("F2"), (-VesselState.currentRoll).ToString("F2"), autopilot.RollAct.ToString("F5")), wrappedLabel,
                        GUILayout.ExpandWidth(true)); //error:" Target:"" Cur:"

                GUILayout.BeginHorizontal();
                GUILayout.Label("Yaw", GUILayout.ExpandWidth(true));
                GUILayout.Label("Kp", GUILayout.ExpandWidth(false));
                autopilot.YawKp.Text = GainField(autopilot.YawKp);
                GUILayout.Label("Ki", GUILayout.ExpandWidth(false));
                autopilot.YawKi.Text = GainField(autopilot.YawKi);
                GUILayout.Label("Kd", GUILayout.ExpandWidth(false));
                autopilot.YawKd.Text = GainField(autopilot.YawKd);
                GUILayout.EndHorizontal();
                if (autopilot.HeadingHoldEnabled)
                    GUILayout.Label(string.Format("Error:{0} Target:{1} Cur:{2} PID: {3}", autopilot.YawErr.ToString("F2"),
                        autopilot.RealYawTarget.ToString("F2"), autopilot.CurrYaw.ToString("F2"), autopilot.YawAct.ToString("F5")), wrappedLabel,
                        GUILayout.ExpandWidth(true)); //error:" Target:"" Cur:"

                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("Limits:"), GUILayout.ExpandWidth(false)); //Yaw Control Limit
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("Pitch down"), GUILayout.ExpandWidth(false));
                autopilot.PitchDownLimit.Text = GUILayout.TextField(autopilot.PitchDownLimit.Text, GUILayout.Width(40));
                GUILayout.Label(string.Format("Pitch up"), GUILayout.ExpandWidth(false));
                autopilot.PitchUpLimit.Text = GUILayout.TextField(autopilot.PitchUpLimit.Text, GUILayout.Width(40));
                GUILayout.Label(string.Format("Yaw"), GUILayout.ExpandWidth(false));
                autopilot.YawLimit.Text = GUILayout.TextField(autopilot.YawLimit.Text, GUILayout.Width(40));
                GUILayout.Label(string.Format("Roll"), GUILayout.ExpandWidth(false));
                autopilot.RollLimit.Text = GUILayout.TextField(autopilot.RollLimit.Text, GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(host.Status, wrappedLabel, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Close")) host.SetVisible(false);
        }

    }
}
