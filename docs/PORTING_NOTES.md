# Porting record

Behavioral authority: MechJeb 2.15.4.0, commit
`f06a76e2bde14ec4d47794323376d73de72c604f`, confirmed with the local reference
checkout. The checkout and the three supplied extracts remain unchanged.

The aircraft controller's equations, calculation order, default targets (heading
90; other targets zero), initially disabled modes, gains, 0.4 m altitude offset,
mode dependencies, 0.2 degree fine-heading threshold, output /100 scaling and
pre-compute integral limiting are retained. Acceleration divides by Unity
`Time.fixedDeltaTime`; each PID divides by `TimeWarp.fixedDeltaTime`, as upstream.
PID synchronization deliberately preserves **float multiplication before double
division** in `s.pitch/roll/yaw * 100 / Ki`. Differential tests caught this precision
subtlety. The first historical PID class returns its *unclamped* action even when
its internal saturation test rolls back integration. This surprising behavior is
preserved and tested; the aircraft controller clamps final channel commands.

Telemetry uses `CoMD`, body rotational velocity subtraction and the exact
`Inverse(Euler(90,0,0) * Inverse(vessel rotation) * surface rotation)` transform.
Instantaneous pitch/roll/heading remain distinct from averaged pitch. Surface
speed, vertical speed, pitch and altitude each use ten zero-seeded samples,
summing the ring in original index order. Sampling continues while disengaged in
FixedUpdate and moves inside OnFlyByWire while engaged, without double sampling.

The guidance window is adapted directly from the historical source, with the
same row order, conditional V/S/bank rows, modifier increments, target clamps,
Set actions, four PID triplets, diagnostics and limit controls. Standalone
English strings replace localization. A close button, status text, stock skin,
scrolling, saved position and mouse-over control locks replace DisplayModule.
The window is 400 pixels wide to fit independent controls and status text.
The title remains draggable outside the scrolling content. The original GUI's
nonnegative vertical-speed editing and signed [-60,+60] bank field clamp remain;
normal operation should use a positive bank magnitude.

## Explicit protective/host changes

* Invalid/nonpositive timestep: controller leaves inputs untouched. PID's explicit
  timestep overload returns zero and resets on invalid inputs, invalid gains or
  arithmetic overflow. Zero Ki synchronizes/limits the integral to zero. Negative
  Ki uses symmetric absolute accumulator bounds rather than inverted bounds.
  Normal positive-gain computations match the historical fixture.
* Near-zero surface speed (<1e-9 m/s averaged): vertical-speed pitch adjustment is
  zero, avoiding the historical division by zero. This is a ground/zero-speed
  protection, not a new flight law.
* Nonfinite telemetry samples throw to the host, which disengages and detaches.
  Finite numeric editing retains the last valid value on invalid text. Independent
  invariant-culture editing also permits exponent notation.
* Native KSP callback writes throttle directly, clamped to [0,1], and mirrors it
  into `FlightInputHandler.state.mainThrottle` only for the active vessel while
  Speed Hold owns it. Speed Hold OFF or full disengagement writes no throttle,
  restores no stale input, and leaves the last commanded value for player handoff.
* The host refuses engagement with SAS active, MechJeb aircraft/attitude/throttle
  controllers enabled, unreadable MechJeb ownership, or unknown OnFlyByWire writers.
  These checks repeat before every control tick; later conflicts disengage AAC.
  It never disables another mod or SAS. FAR aerodynamics callbacks are permitted;
  this is not certification of compatibility with FAR stability assistance.
  Mods using other callback phases cannot all be identified; the UI explicitly
  warns to keep other flight controllers off. Unknown writers can cause a
  conservative false refusal even when they are currently idle.
* One active-vessel callback is owned at a time. Vessel switching always disengages;
  per-vessel modes/targets remain in memory within the scene, with no automatic
  reengagement. Scene exit/unload/faults detach. No background control is attempted.
* Settings save numeric defaults and GUI state, never live mode or engagement
  flags. The settings file is not bundled in releases. Installation preserves it.
* External bank control is opt-in, bounded, exclusive by owner token, and cannot
  engage the controller. Releasing it disables its roll mode and leaves player
  inputs untouched.

## Specification/API clarifications

No flight-law disagreement between the specification and historical source was
found. The specification intentionally did not give exact launcher teardown
signatures: the actual local compiler verifies `onGUIApplicationLauncherUnreadifying`
is `EventData<GameScenes>`, whereas `onGUIApplicationLauncherDestroyed` is EventVoid.
The port uses those exact delegate shapes. The stock launcher also requires the
Unity Animation module as a compile reference through its inherited API; no extra
runtime assembly is packaged.

The historical upstream source, extracted references and game are excluded by
.gitignore. Runtime code never references MuMech types. Strings naming MechJeb
classes exist only in optional conflict reflection. The historical tests compile
a separate test fixture into the **test executable only**, never the plugin.
No Kramax, Harmony, Waypoint Navigator or unrelated MechJeb implementation is used.
