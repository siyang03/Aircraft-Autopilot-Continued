// GPL-3.0-only. Standalone KSP host; no MechJeb dependency.
using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
namespace AircraftAutopilotContinued
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class AircraftAutopilotAddon : MonoBehaviour
    {
        public static AircraftAutopilotAddon Instance { get; private set; }
        public AircraftAutopilotController Controller { get; private set; }
        public AircraftAutopilotWindow Window { get; private set; }
        public Rect WindowRect = new Rect(180, 100, 400, 260);
        public bool Visible { get; private set; }
        public string Status { get; private set; } = "Disengaged. Disable other flight controllers before engaging.";
        readonly Dictionary<Guid, AircraftAutopilotController> controllers = new Dictionary<Guid, AircraftAutopilotController>();
        readonly Dictionary<Guid, AircraftAutopilotWindow> windows = new Dictionary<Guid, AircraftAutopilotWindow>();
        readonly CallbackOwnership<Vessel> ownership = new CallbackOwnership<Vessel>();
        readonly ConflictDetector conflicts = new ConflictDetector(message => Debug.LogWarning("[AAC] " + message));
        Vessel conflictVessel;
        float nextConflictDiscovery;
        Vessel selected;
        ApplicationLauncherButton button;
        Vector2 scroll;
        const string LockName = "AircraftAutopilotContinued.Window";
        const int WindowId = 0x414143;
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Debug.Log("[AAC] Flight host initialized (Aircraft Autopilot Continued 1.0.0)");
            GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(LauncherUnreadifying);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveButton);
            GameEvents.onVesselChange.Add(VesselChanged);
            if (ApplicationLauncher.Ready) AddButton();
        }
        void Start() { VesselChanged(FlightGlobals.ActiveVessel); }
        void AddButton()
        {
            if (button != null || ApplicationLauncher.Instance == null) return;
            button = ApplicationLauncher.Instance.AddModApplication(() => SetVisible(true), () => SetVisible(false),
                null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT,
                GameDatabase.Instance.GetTexture("AircraftAutopilotContinued/Icons/AppLauncherIcon", false));
            Debug.Log("[AAC] Stock flight toolbar button created");
            if (Visible) button.SetTrue(false);
        }
        void LauncherUnreadifying(GameScenes scene) { RemoveButton(); }
        void RemoveButton()
        {
            if (button != null && ApplicationLauncher.Instance != null) ApplicationLauncher.Instance.RemoveModApplication(button);
            button = null;
        }
        public void SetVisible(bool visible)
        {
            Visible = visible;
            if (button != null) { if (visible) button.SetTrue(false); else button.SetFalse(false); }
            if (!visible) { InputLockManager.RemoveControlLock(LockName); AutopilotSettings.Save(this); }
        }
        void VesselChanged(Vessel vessel)
        {
            if (selected == vessel && Controller != null) return;
            AutopilotSettings.Save(this);
            Disengage("Vessel changed: autopilot disengaged");
            selected = vessel; Controller = null; Window = null;
            conflictVessel = null;
            if (vessel == null) return;
            AircraftAutopilotController c;
            if (!controllers.TryGetValue(vessel.id, out c)) {
                c = new AircraftAutopilotController(); AutopilotSettings.Load(c);
                controllers.Add(vessel.id, c);
                windows.Add(vessel.id, new AircraftAutopilotWindow(this, c));
            }
            c.ActiveVessel = vessel; Controller = c; Window = windows[vessel.id];
            AutopilotSettings.LoadWindow(this);
        }
        static bool Usable(Vessel vessel) => vessel != null && vessel == FlightGlobals.ActiveVessel && vessel.loaded && !vessel.packed && vessel.IsControllable;
        public bool Engage()
        {
            if (!Usable(selected) || Controller == null) { Status = "Cannot engage: active vessel must be loaded, unpacked and controllable."; return false; }
            if (Controller.Enabled) return true;
            string conflict = Conflict(selected, true);
            if (conflict != null) { Status = conflict; return false; }
            Controller.SetEngaged(true);
            ownership.Attach(selected, v => { v.OnFlyByWire -= FlyByWire; v.OnFlyByWire += FlyByWire; }, v => v.OnFlyByWire -= FlyByWire);
            Status = "Auto ON. Other autopilots must remain OFF.";
            return true;
        }
        public void Disengage(string reason = "Disengaged")
        {
            ownership.Detach(v => v.OnFlyByWire -= FlyByWire);
            if (Controller != null) Controller.SetEngaged(false);
            Status = reason;
        }
        void FixedUpdate()
        {
            if (FlightGlobals.ActiveVessel != selected) VesselChanged(FlightGlobals.ActiveVessel);
            if (Controller == null) return;
            if (!Usable(selected)) { Disengage("Vessel unavailable: autopilot disengaged"); return; }
            try {
                if (!Controller.Enabled) Controller.VesselState.Update(selected);
            } catch (Exception e) { Fault(e); }
        }
        void FlyByWire(FlightCtrlState state)
        {
            try {
                if (!Usable(ownership.Owner) || ownership.Owner != selected || Controller == null || !Controller.Enabled) { Disengage("Control ownership released"); return; }
                string conflict = Conflict(selected);
                if (conflict != null) { Disengage(conflict); return; }
                Controller.VesselState.Update(selected);
                Controller.Drive(state);
                if (Controller.SpeedHoldEnabled && FlightGlobals.ActiveVessel == selected && Maths.Finite(state.mainThrottle))
                    FlightInputHandler.state.mainThrottle = state.mainThrottle;
            } catch (Exception e) { Fault(e); }
        }
        void Fault(Exception e) { Disengage("Error: autopilot released control. See KSP.log."); Debug.LogError("[AAC] " + e); }
        static IEnumerable<object> ConflictModules(Vessel vessel)
        {
            foreach (Part part in vessel.parts) foreach (PartModule module in part.Modules) yield return module;
        }
        static IEnumerable<Delegate> ConflictCallbacks(Vessel vessel)
        {
            // Kramax George owns pre/post callbacks rather than OnFlyByWire.
            Delegate[] phases = { vessel.OnFlyByWire, vessel.OnPreAutopilotUpdate, vessel.OnPostAutopilotUpdate };
            foreach (Delegate phase in phases) if (phase != null)
                foreach (Delegate callback in phase.GetInvocationList()) yield return callback;
        }
        string Conflict(Vessel vessel, bool refresh = false)
        {
            if (vessel.ActionGroups[KSPActionGroup.SAS]) return "SAS is ON: disable SAS before using Auto.";
            try {
                float now = Time.realtimeSinceStartup;
                if (refresh || conflictVessel != vessel || now >= nextConflictDiscovery) {
                    conflicts.Refresh(ConflictModules(vessel), ConflictCallbacks(vessel));
                    conflictVessel = vessel;
                    nextConflictDiscovery = now + 1f;
                }
                return conflicts.Check(false);
            } catch (Exception e) {
                Debug.LogWarning("[AAC] Conflict discovery: " + e);
                return "Cannot inspect flight-control ownership: discovery failed. See KSP.log.";
            }
        }
        void Update()
        {
            if (Visible && Window != null) {
                Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                if (WindowRect.Contains(mouse)) InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.CAMERACONTROLS, LockName);
                else InputLockManager.RemoveControlLock(LockName);
            } else InputLockManager.RemoveControlLock(LockName);
        }
        void OnGUI()
        {
            if (!Visible || Window == null || !HighLogic.LoadedSceneIsFlight) return;
            GUI.skin = HighLogic.Skin;
            WindowRect.x = Mathf.Clamp(WindowRect.x, 0, Math.Max(0, Screen.width - WindowRect.width));
            WindowRect.y = Mathf.Clamp(WindowRect.y, 0, Math.Max(0, Screen.height - 80));
            WindowRect = GUILayout.Window(WindowId, WindowRect, id => {
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(Math.Max(100, Screen.height - 100)));
                Window.WindowGUI(id); GUILayout.EndScrollView();
                GUI.DragWindow(new Rect(0, 0, 10000, 22));
            }, "Aircraft Autopilot", GUILayout.Width(400), GUILayout.Height(0));
        }
        void OnDestroy()
        {
            if (Instance != this) return;
            AutopilotSettings.Save(this);
            Disengage("Flight scene ended");
            InputLockManager.RemoveControlLock(LockName);
            GameEvents.onGUIApplicationLauncherReady.Remove(AddButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(LauncherUnreadifying);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveButton);
            GameEvents.onVesselChange.Remove(VesselChanged);
            RemoveButton(); Instance = null;
        }
    }
}
