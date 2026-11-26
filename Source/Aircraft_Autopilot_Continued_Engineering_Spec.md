# Aircraft Autopilot Continued — verified porting specification

**Source target:** MechJeb2 `2.15.4.0`, commit `f06a76e2bde14ec4d47794323376d73de72c604f`.  
**Game target:** local macOS KSP build `03190`, corresponding to KSP 1.12.5.  
**Scope:** first standalone GPL-3.0 release, preserving the original aircraft PID control modes and almost identical IMGUI interface. No runtime or compile-time MechJeb dependency.

## Project paths

Root: `/Users/siyang/Documents/Computation/AI/Codex/Aircraft Autopilot Continued/`

- KSP: `Source/KSP_osx/`
- Historical source: `Source/MechJeb2-dev/` (despite its name, tag `2.15.4.0`)
- Three extracted reference sources: `Source/MechJeb essential/`
- New tracked code: `Source/AircraftAutopilotContinued/`
- Built package: `dist/GameData/AircraftAutopilotContinued/`

`.gitignore` excludes game, historical source, standalone reference copies, and build outputs. Codex must confirm the resulting new source files are not ignored before completion.

## Verified local-assembly API (not guessed)

The uploaded KSP 1.12.5 `Assembly-CSharp.dll` metadata directly contains:

- `Vessel.OnFlyByWire`, `Vessel.OnAutopilotUpdate`, `Vessel.OnPreAutopilotUpdate`, `Vessel.OnPostAutopilotUpdate`, all fields of delegate type `FlightInputCallback`.
- `FlightInputCallback.Invoke(FlightCtrlState)`.
- `FlightCtrlState` fields `mainThrottle`, `roll`, `yaw`, `pitch`, `rollTrim`, `yawTrim`, `pitchTrim` (`float`).
- `Vessel.CoMD`, `Vessel.obt_velocity`, `Vessel.north`, `Vessel.east` (`Vector3d`), `Vessel.loaded`, `Vessel.packed` (`bool`), `Vessel.srfSpeed` (`double`).
- `Vessel.GetTransform()`, `Vessel.ReferenceTransform`, `Vessel.GetSrfVelocity()`, `Vessel.GetWorldPos3D()`.
- `CelestialBody.getRFrmVel(Vector3d)`, `CelestialBody.GetAltitude(Vector3d)`.
- `KSP.UI.Screens.ApplicationLauncher.Instance`, `ApplicationLauncher.Ready` (`bool`), `ApplicationLauncher.AddModApplication(...)`, `ApplicationLauncher.RemoveModApplication(...)`.
- `ApplicationLauncherButton.SetTrue(bool)`, `SetFalse(bool)`.
- `GameDatabase.Instance.GetTexture(string, bool)`.
- `InputLockManager.SetControlLock(ControlTypes, string)` and `RemoveControlLock(string)`.
- `GameEvents.onGUIApplicationLauncherReady`, `onGUIApplicationLauncherUnreadifying`, `onGUIApplicationLauncherDestroyed`, `onVesselChange`.
- `[KSPAddon(KSPAddon.Startup.Flight, false)]` is appropriate for a flight-scene MonoBehaviour.

**Important:** MechJeb2 `2.15.4.0` itself registers on `vessel.OnFlyByWire`, not merely `OnAutopilotUpdate`. Reuse `OnFlyByWire` for closest fidelity. Do not attach more than one duplicate callback; always unregister on vessel change/scene destruction.

Kramax `Toolbar/AppLauncher.cs` from the uploaded archive uses this proven call pattern:

```csharp
ApplicationLauncher.Instance.AddModApplication(
    OnToggleTrue, OnToggleFalse,
    null, null, null, null,
    ApplicationLauncher.AppScenes.FLIGHT,
    GameDatabase.Instance.GetTexture(
        "AircraftAutopilotContinued/Icons/AppLauncherIcon", false));
```

Use **the native API pattern**, but implement new original plugin code and choose a texture path in our own GameData. Do not copy Kramax source verbatim into our GPL project; its licensing differs.

## Recommended minimal implementation files

1. `AircraftAutopilotAddon.cs`: `[KSPAddon(KSPAddon.Startup.Flight, false)]` MonoBehaviour, active vessel callback registration and deregistration, GUI state, launcher lifecycle.
2. `AircraftAutopilotController.cs`: ported 352-line MechJeb aircraft controller, with `ComputerModule` inheritance removed; keep original control equations, fields, mode interaction, enable/disable behavior.
3. `AircraftTelemetry.cs`: faithful small replacement for needed MechJeb `VesselState` quantities.
4. `PIDController.cs`: GPL port of old MechJeb PID controller's **first** class (not the unrelated V2/V3 variants); preserve accumulator and derivative semantics.
5. `MovingAverage.cs`: 10-sample ring average matching `MechJeb2/MuUtils.cs` default; used for `speedSurface`, `speedVertical`, `vesselPitch`, `altitudeASL` where original `VesselState` used averages.
6. `AircraftAutopilotWindow.cs`: standalone Unity IMGUI adaptation of the original 427-line guidance window, including PID panel and diagnostics.
7. `NumericField.cs`: small replacement for MechJeb `EditableDouble`, keeping editable text + parsed valid numeric value distinct.
8. `AutopilotSettings.cs`: standalone ConfigNode persistence; per-vessel state must not inadvertently be shared across aircraft.
9. Icon: historical GPL MechJeb aircraft icon can be reused with attribution, or create an original icon.
10. `scripts/build.sh`, `scripts/install.sh`, `scripts/test.sh`, `README.md`, `LICENSE`, `THIRD_PARTY_NOTICES.md`, `docs/TESTING.md`.

## Exact telemetry mapping from original MechJeb 2.15.4

**Keep the original coordinate conventions rather than using any apparently similar navball Euler angles.** The original `VesselState.UpdateBasicInfo` does:

```csharp
Vector3d com = vessel.CoMD;
Vector3d up = (com - vessel.mainBody.position).normalized;
Vector3d north = vessel.north;
Vector3d east = vessel.east;
Vector3d forward = vessel.GetTransform().up;
Quaternion rotationSurface = Quaternion.LookRotation(north, up);
Quaternion rotationVesselSurface = Quaternion.Inverse(
    Quaternion.Euler(90, 0, 0) *
    Quaternion.Inverse(vessel.GetTransform().rotation) *
    rotationSurface);
Vector3d surfaceVelocity = vessel.obt_velocity -
    vessel.mainBody.getRFrmVel(com);

double eulerX = rotationVesselSurface.eulerAngles.x;
double eulerZ = rotationVesselSurface.eulerAngles.z;
double currentPitch = eulerX > 180 ? 360 - eulerX : -eulerX;
double currentRoll = eulerZ > 180 ? eulerZ - 360 : eulerZ;
double currentHeading = rotationVesselSurface.eulerAngles.y;

double rawSurfaceSpeed = surfaceVelocity.magnitude;
double rawVerticalSpeed = Vector3d.Dot(surfaceVelocity, up);
double rawAltitudeASL = vessel.mainBody.GetAltitude(com);
```

MechJeb then places speed/pitch/altitude in `MovingAverage` instances. The default is **10 samples**, initially filled with zero. The `currentHeading`, `currentRoll`, `currentPitch` accessors above are instantaneous, whereas the original `vesselPitch`, `speedSurface`, `speedVertical`, `altitudeASL` fields are smoothed. Do not conflate them.

`VesselState.HeadingFromDirection(dir)`:

```csharp
Wrap360(Math.Atan2(
    Vector3d.Dot(dir, east),
    Vector3d.Dot(dir, north)) * 180 / Math.PI)
```

Original heading-based yaw estimator:

```csharp
double path = HeadingFromDirection(surfaceVelocity);
double nose = HeadingFromDirection(forward);
double currYaw = Wrap180(nose - path);
```

Original roll-error sign:

```csharp
RollErr = Wrap180(RealRollTarget - (-currentRoll));
```

A flight test must verify both ±20-degree roll commands converge rather than diverge; purely mathematical tests cannot establish the real craft's control-surface sign, FAR response, or physical outcome.

## Port controller unchanged wherever possible

The source in `MechJeb2/MechJebModuleAirplaneAutopilot.cs` should be the reference, not a newly invented PID implementation:

- Acceleration PID defaults: `Kp=0.5`, `Ki=0.5`, `Kd=0.005`.
- Pitch PID: `2.0, 1.0, 0.005`.
- Roll PID: `0.5, 0.02, 0.5`.
- Yaw PID: `1.0, 0.25, 0.02`.
- `YawLimit=10`, `RollLimit=45`, `PitchDownLimit=15`, `PitchUpLimit=25` degrees; `BankAngle=30` degrees.
- Target fields, `EnableHeadingHold`, `EnableAltitudeHold`, `EnableVertSpeedHold`, `EnableSpeedHold`, and matching disables must keep the original semantics.
- PID code: `INTAccum += error * TimeWarp.fixedDeltaTime; action = Kp*error + Ki*INTAccum + Kd*(error-prevError)/dt`, with source-specific bounding and anti-windup behavior.
- `SynchronizePIDs` uses incoming `FlightCtrlState` controls, with source-specific scaling by `100/Ki`.
- Heading logic changes strategy for `abs(headingError) < 0.2°`; larger heading changes command bank, small errors command yaw.
- **Original yaw loop only outputs rudder when `HeadingHoldEnabled`.** Do not automatically enable yaw while roll hold is active; this is a separate future improvement.
- Altitude mode outputs vertical-speed demand, and vertical-speed mode outputs pitch demand. Do not replace with generic altitude P-controller.

Do **not** make everything run every frame without checking the enabled modes. The old controller writes `s.pitch` only with vertical-speed hold, `s.roll` only with roll hold, `s.yaw` only with heading hold, and throttle only while speed hold is active. For standalone implementation, enforce finite/numerical validation before writing.

## How to replace core lifecycle

MechJeb2 `MechJebCore.OnFlyByWire(FlightCtrlState s)` calls `VesselState.Update(vessel)` and then `module.Drive(s)` for enabled modules. To mirror:

- Create one owner object for the active flight-scene vessel (or a dictionary of per-vessel state if safe and justified).
- On engagement, verify loaded/unpacked/controllable vessel and clear/initialize the mode-specific PID state.
- Attach **once** to `vessel.OnFlyByWire += Callback` and remove on disengagement, vessel switch, unloading, scene destruction, and error (choose a consistent ownership model).
- Inside callback: check vessel identity, sample telemetry, call `Controller.Drive(state)` only if enabled, validate commanded channel outputs. Handle exception by disengaging and removing callback safely without crashing KSP.
- Closing the UI does not disengage the controller.
- Handle SAS conflict explicitly (prefer refusing to engage with SAS already on, or disabling/restoring only with tracked ownership). Do not fight another attitude controller.
- Preserve control state for the correct vessel. State changes across vessel switches should be intentional and documented, not accidental.

## How to replace throttle

Old autopilot writes `Core.Thrust.TargetThrottle` while Speed Hold is active. MechJeb's thrust module then assigns `s.mainThrottle = TargetThrottle` when it has active users. Native KSP directly exposes `FlightCtrlState.mainThrottle` as a `float` field.

Inside the same vessel `OnFlyByWire` callback, when and only when speed hold owns throttle, set:

```csharp
s.mainThrottle = Mathf.Clamp01((float)computedThrottle);
```

MechJeb also updates `FlightInputHandler.state.mainThrottle` for the on-screen throttle gauge when the controlled vessel is the active vessel. Evaluate whether the standalone port needs that synchronization: if done, scope the operation strictly to the active vessel, and never write to global flight input from an inactive vessel. On disengagement, stop writing throttle; do **not** command zero automatically. Document how the pilot's throttle becomes authoritative again.

## Exact GUI design

Adapt the old `MechJebModuleAirplaneGuidance.cs`, preserving:

- Auto On/Off
- Altitude Hold with target, +/−, Set
- Vertical Speed Hold with target, +/−, Set
- Heading Hold with target, +/−, Set
- Roll Hold or heading-associated bank limit as in original
- Speed Hold with target, +/−, Set
- Expandable PID section: acceleration/pitch/roll/yaw Kp/Ki/Kd, diagnostic values, pitch/yaw/roll limits
- Modifier-key adjustment behavior, field validity, original default target values

Replace only `DisplayModule`, `WindowGUI` base calls, `Persistent`, `EditableDouble`, localization, and any `Core` references with standalone equivalents. Use `GUI.Window`/`GUILayout.Window` and `GUI.DragWindow`, with safe window ID and saved `Rect`. Keep input lock only when mouse is over the visible window, and *always* remove it on close/scene destruction. Avoid Kramax copy/paste to sidestep CC BY-NC-SA licensing issues.

**Stock toolbar lifecycle:** create flight-scene KSPAddon, subscribe to `GameEvents.onGUIApplicationLauncherReady` in `Awake`, call `AddModApplication` if launcher is already Ready, remove button and unsubscribe in `OnDestroy`. `onGUIApplicationLauncherUnreadifying`/`Destroyed` may be used to handle launcher invalidation. Button OnTrue opens window; OnFalse closes. Use `SetTrue/SetFalse(false)` for programmatic state sync. Use only one button instance.

## Build requirements

Determine compiler available on the user's Mac (`dotnet`, Roslyn CSC, Mono). The uploaded game DLL inventory contains all relevant KSP/Unity references. Compile C# compatible with game runtime, using local managed assemblies, including `Assembly-CSharp.dll`, `mscorlib.dll`, `System.dll`, `System.Core.dll`, and the relevant `UnityEngine.*.dll`. Confirm compiler emits a Mono-compatible managed DLL. No runtime dependency on `MechJeb2.dll`, Kramax DLL, or Harmony. Package DLL, icon, standalone config and GPL/attribution files in `GameData/AircraftAutopilotContinued/`.

Check `git status --short` and `git check-ignore -v Source/AircraftAutopilotContinued/*.cs`; confirm nothing outside desired directories is tracked, and no upstream clone/game files enter the package.

## Verification gates

1. **Compile**: actual DLL builds against local game assemblies, no MechJeb reference; `build.sh` repeatable.
2. **Deterministic tests**: PID step response and windup, heading wraparound, sign of roll-error mapping, moving average startup and steady state, mode transitions and throttle handoff, invalid/zero Ki, finite output.
3. **Install**: `install.sh` deploys only our mod into local KSP `GameData`, rerunnable and reversible.
4. **Load**: if graphical test is possible, inspect `KSP.log` for addon/load exceptions and duplicate launcher creation.
5. **Manual flight**: button appears, GUI opens and closes independently of autopilot, heading ± and across north, roll ± commands stabilize, pitch/altitude/speed hold, PID edits, SAS conflicts, vessel switch, exit/re-enter, restart persistence, FAR test.

Tests 1–3 can generally be verified without launching the game. **Flight behavior (5) must not be claimed verified without actual flight testing.**

## Highest-risk subtleties, now known

- The original controller runs at `OnFlyByWire`, not an arbitrary `Update` tick.
- The original MechJeb angle transform includes a 90-degree quaternion Euler rotation; naive KSP Euler replacements can reverse roll and pitch.
- The source has both instantaneous and *10-tick averaged* values; substituting raw fields alters PID dynamics.
- MechJeb throttle is a two-step handoff through `Core.Thrust`; directly forcing throttle to zero on release would be wrong.
- PID initialization uses incoming pilot control values and scales by gains; omitting it causes engagement transients.
- Original yaw behavior is **not** zero-sideslip yaw during Roll Hold; that was a separate navigator Harmony change, outside this port.
- Use original MechJeb GPL source and optionally GPL icon, but **do not incorporate Kramax CC BY-NC-SA source** into the GPL port.
- Game-loaded C# DLL does not prove aircraft control stability; real flight test remains necessary.

## Sources used

- `https://github.com/MuMech/MechJeb2/blob/2.15.4.0/MechJeb2/MechJebModuleAirplaneAutopilot.cs`
- `https://github.com/MuMech/MechJeb2/blob/2.15.4.0/MechJeb2/MechJebModuleAirplaneGuidance.cs`
- `https://github.com/MuMech/MechJeb2/blob/2.15.4.0/MechJeb2/PIDController.cs`
- `https://github.com/MuMech/MechJeb2/blob/2.15.4.0/MechJeb2/VesselState.cs`
- `https://github.com/MuMech/MechJeb2/blob/2.15.4.0/MechJeb2/MechJebCore.cs`
- `https://github.com/MuMech/MechJeb2/blob/2.15.4.0/MechJeb2/MechJebModuleThrustController.cs`
- `https://github.com/MuMech/MechJeb2/blob/2.15.4.0/MechJeb2/MuUtils.cs`
- User-supplied KSP 1.12.5 Managed DLLs, buildID.txt, Kramax `Toolbar/AppLauncher.cs`.
