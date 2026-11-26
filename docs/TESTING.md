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

Actual game validation is separate. KSP was opened and its main menu inspected;
no aircraft was launched or controlled. The user requested to do gameplay testing
personally. The game was started before the DLL installation, so **restart KSP
before testing the installed build**. An existing log is not proof that this DLL
was loaded. No flight stability, GUI interaction or FAR compatibility is claimed.

## Manual flight checklist

1. Restart KSP. Enter flight with a controllable aircraft; confirm one stock-toolbar
   aircraft icon. Check KSP.log for AAC assembly loading and any exceptions.
2. Open/close the window through the toolbar and Close button. Drag the title,
   expand PID, scroll, edit fields, and verify no click-through; hiding releases locks.
3. Disable SAS and other autopilots. Engage/disengage Auto; verify SAS prevents
   engagement and turning SAS on while engaged releases our controller. If refused
   for a foreign callback, inspect the named mod rather than bypassing the check.
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
control is enabled. Do not run two flight controllers together.
