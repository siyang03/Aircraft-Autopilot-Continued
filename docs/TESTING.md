# Validation

Run from the repository root:

```sh
bash scripts/build.sh
bash scripts/test.sh
bash scripts/install.sh
```

The plugin compiles as C# 7.3 against the supplied KSP 1.12.5 managed assemblies,
including the game's mscorlib, without NuGet dependencies. The local compiler is
a project-local copy of an already available .NET 8.0.428 SDK. On another machine,
install a .NET 8 SDK and set DOTNET to its executable (or use PATH).

The test executable uses .NET 8 and small test-only KSP/Unity API shapes. It tests
production PID, controller, telemetry sampling, numeric fields and callback-owner
code. Native Unity quaternion calls and game physics are deliberately not faked.
Historical method bodies, under a separate test namespace, provide 1,200
comparison steps across six mode combinations and changing flight samples.
The test fixture is included in the source distribution, never the plugin DLL.

Automated coverage includes PID P/I/D, reset and anti-windup, zero Ki, finite
validation and overflow, integral limits, exact synchronization scaling, heading
wrap and fine heading strategy, pitch/roll signs, original bank limiting, ten
sample zero-seeded averages, mode dependencies, inactive-channel preservation,
throttle clamp/handoff, exclusive external bank ownership, duplicate callback
prevention, vessel-owner switching and exception teardown. The package verifier
checks exact ZIP contents, source ZIP exclusions, 38x38 icon and installed DLL
identity. Metadata inspection verifies the unique assembly name, KSP/Unity/Mono
references and absence of MechJeb/Harmony/Kramax dependencies. A source-level
check verifies closing the window cannot disengage the host; this is not GUI testing.

Actual game validation is separate. This refinement did not launch or operate
KSP. Restart KSP before testing the replacement DLL. Removing the SAS interlock
is not proof of physically stable SAS coexistence. Layout policy tests do not
render Unity's stock skin; verify the actual text fields and panel in game.

## Manual flight checklist

1. Restart KSP. Enter flight with a controllable aircraft; confirm one stock-toolbar
   aircraft icon. Check KSP.log for AAC assembly loading and any exceptions.
2. Open/close the window through the toolbar and Close button. At 1512x982, open
   PID with all modes/diagnostics active: all four gain rows and limits should fit
   vertically without scrolling. Hide PID should shrink without blank space.
   Verify 0.005, 0.0001, 0.02, 0.5 and 123.456 are fully visible/editable, including
   scientific notation. Drag near the bottom edge, reopen PID and verify it stays
   on screen. Reduce screen height/width to test fallback scrolling, then enlarge
   again: unnecessary scrollbars should disappear. Verify no click-through,
   draggable title and toolbar/Close behavior; hiding releases locks.
3. Disengage other autopilots. Test engaging AAC with SAS OFF and ON. While AAC
   controls the craft, toggle SAS both directions: AAC must remain engaged.
   Disengage AAC in each SAS state and verify SAS is untouched. Check actual
   flight behavior separately; the removed software interlock does not certify
   stability. If refused for unreadable known mod ownership, inspect the named
   mod. Unknown hooks should warn without automatically refusing engagement.
4. In stable flight, test standalone Roll Hold at 0, +20 and -20 degrees; verify
   convergence, correct bank direction and manual rudder availability.
5. Test Heading Hold both ways through 359/0/1 degrees, limits, and fine corrections.
6. Test Vertical Speed Hold and Altitude Hold. Use a positive V/S magnitude for
   altitude capture; verify climb/descent, pitch limits and the altitude offset.
7. Test Speed Hold at achievable speeds; inspect throttle gauge. Disable it and
   change throttle manually; no forced zero or stale throttle restoration should occur.
8. Edit all twelve PID gains and the four limits. Close/reopen, exit/reenter flight,
   restart the game and verify settings persist but engagement defaults OFF.
9. Disengage and verify player pitch, roll, yaw and throttle control immediately.
10. Switch vessel and return: no background control, no stale callback, Auto remains
    OFF, and each vessel retains its own in-scene targets/modes.
11. Engage, close the GUI and verify control continues. Reopen and disengage.
12. Exit/reenter flight repeatedly; verify one toolbar button and no duplicate control.
13. Optionally repeat with FAR, with its own flight assistance disabled; check actual
    aerodynamic stability and KSP.log. Mathematical regression tests cannot certify it.

MechJeb-free operation should be tested in a game session with MechJeb absent.
When installed together, test refusal while MechJeb aircraft, attitude or throttle
control actively owns a channel. Idle MechJeb thrust must permit engagement.
SAS is allowed and unmanaged; keep other mod autopilots disengaged.

## Conflict regression coverage

The original conflict fix passed 8,521 assertions including the existing 1,200 historical
comparison steps. Added cases cover MechJeb absent; enabled idle thrust with one
self-user and OFF mode; additional users; non-OFF Tmode; active attitude/aircraft
control; the former SAS veto (now removed); idle auxiliary callbacks with/without MechJeb; idle and independently
active Kramax horizontal/vertical/throttle flags; live deactivation without
rediscovery; deduplicated diagnostic warnings; missing ownership information;
and public property reflection. No KSP launch or UI operation was performed for
this fix. Installed optional-mod DLLs were inspected as managed data only.

Manual follow-up: restart KSP to load the replacement DLL, test engagement with
MechJeb installed but idle, and verify refusal when enabling a genuine MechJeb
throttle request or Kramax mode. Auxiliary idle hooks should only warn. Run
`python3 scripts/verify.py --installed` to recheck installed/build DLL identity;
install.sh also performs this check automatically.

## SAS and panel refinement regression coverage

The current suite passes 8,560 assertions, including the unchanged 1,200 historical
controller comparison steps. The production conflict detector has no SAS argument
or veto; compiled metadata verifies no KSPActionGroup reference. The host has no
SAS read/write, cache or restoration path. Existing active MechJeb/Kramax refusal
and idle auxiliary-hook cases still pass.

Numeric tests assert exact initial strings for 0.005, 0.0001, 0.02, 0.5 and 123.456,
bit-exact round-trip parsing, retained scientific notation, long finite input and
invariant-culture editing. NumericField.cs and its invalid-input protection are
unchanged. Gain widths use the stock style's measured size of 123.456 (plus a
small allowance), with a 60-pixel minimum. Kp/Ki/Kd labels remain explicit.

WindowLayout tests cover compact/expanded hints, measured content replacement,
collapse shrinkage, restored showpid, stale/invalid measurement rejection,
repeated stable sizing, exact-fit screens, short/narrow-screen fallback, scrollbar
removal after enlarging and coordinate bounds. IMGUI applies sizing changes on
Layout events and records natural group height on Repaint; a reserved gutter
keeps wrap width independent of scrollbar presence to avoid feedback oscillation.
These are policy tests, not rendered visual tests.

AutopilotSettings.cs and the persisted keys are unchanged: coordinates and showpid
still save; transient window dimensions and scroll state are not persisted.
Controller/PID/telemetry algorithms and the public controller API are unchanged.
