# Aircraft Autopilot Continued

Independent GPL-3.0-only standalone continuation of the MechJeb 2.15.4.0
Aircraft Autopilot for KSP 1.12.5 (macOS build 03190). MechJeb is not required.

Build: `bash scripts/build.sh` with a .NET SDK (`DOTNET=/absolute/path/to/dotnet`)
and the supplied game under `Source/KSP_osx`. Compile references come from that
game, never from MechJeb. No package restore is needed.

Test: `bash scripts/test.sh`. Install: `bash scripts/install.sh`.
Uninstall only this mod: `bash scripts/install.sh --uninstall`.
For another game installation, copy `dist/GameData/AircraftAutopilotContinued`
into its GameData. Distribute the binary ZIP together with the source ZIP.

In flight, open the aircraft icon on the stock toolbar. Set the desired modes
and targets, disable SAS and other autopilots, and press Engage autopilot.
Closing the window keeps Auto engaged. Disengage to return the owned channels
to the player. Throttle remains at its last command until the player changes it;
Speed Hold OFF never commands zero. SAS activation releases our controller.
Vessel switching, unloading, scene exit and faults disengage; returning never
silently reengages. Unknown OnFlyByWire writers cause conservative refusal.
FAR aerodynamics are allowed; disable any separate FAR assistance controller.

All five original modes and four editable PIDs are retained. As in the original,
vertical-speed target is nonnegative (a climb/descent magnitude in Altitude Hold),
heading hold enables roll hold, disabling heading hold disables roll hold, and
rudder runs only under heading hold. Fields apply with Set; +/- applies immediately;
KSP's modifier key changes increments from 1 to 5. Invalid numeric text retains
the last valid value. PID gains apply live. A zero V/S limit prevents altitude changes.

Settings: `GameData/AircraftAutopilotContinued/PluginData/settings.cfg` saves gains,
limits, targets, window position and PID panel visibility. Remove it to reset.
Engagement and mode flags are never restored across sessions. Within a flight,
mode/target state belongs to its vessel; saved numeric defaults seed new vessels.

See [porting notes](docs/PORTING_NOTES.md), [testing](docs/TESTING.md) and
[small public API](docs/PUBLIC_API.md). Successful compilation and deterministic
tests do not establish physical aircraft stability; complete the manual checklist.
