# Small public control API

Assembly and namespace: `AircraftAutopilotContinued`. No navigator dependency.
Call from KSP's main Unity thread. This API is available only in flight.

`AircraftAutopilotAddon.Instance` is null outside the flight host's lifetime.
Its `Controller` is the selected vessel's controller, or null when no vessel is
selected. Use `Engage()` (bool result) and `Disengage()` on the host; these own the
safety checks and callback attachment. `Status` explains refusal or disengagement.
Never register a second callback or invoke `Drive` yourself in a running game.

Controller members:

| Member | Meaning |
| --- | --- |
| `ActiveVessel` | KSP vessel association (read-only to consumers) |
| `Enabled` | Host engagement state (read-only to consumers) |
| `HeadingHoldEnabled`, `RollHoldEnabled` | Historical mode flags |
| `CurrentHeading` | Instantaneous 0–360 degrees |
| `CurrentRoll` | Historical signed telemetry roll; command sign is its negative |
| `EffectiveBankLimit` | Minimum absolute bank/control roll limit |
| `TargetRollAngle` | Requested roll command, degrees |
| `TrySetBank(owner, degrees)` | Acquire/update exclusive bank ownership; clamps to effective limit |
| `ReleaseExternalControl(owner)` | Owner-only release; disables the externally owned roll hold |

Use a stable non-null object token for the owner. TrySetBank returns false when
Auto is OFF, the command is nonfinite, or another owner holds bank control.
On success it disables heading mode, enables roll mode and changes RollTarget.
It does not enable rudder. Check the return value on each update. Full host
 disengagement invalidates ownership. Release does not restore a previous mode,
reengage heading, or write a final control command. Stop sending commands before
release. A window being hidden has no bearing on this API or engagement.

The original mode methods remain available for explicit mode changes. Public
historical tuning/target fields are retained for compatibility with the source;
prefer the bounded owner API for external bank requests. Do not set live mode
fields concurrently from an external control loop and the pilot UI.
