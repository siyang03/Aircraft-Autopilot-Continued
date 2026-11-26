#!/usr/bin/env python3
from pathlib import Path
import zipfile,struct,hashlib,sys
r=Path(__file__).resolve().parent.parent
mod='GameData/AircraftAutopilotContinued/'
with zipfile.ZipFile(r/'dist/AircraftAutopilotContinued-1.0.0.zip') as z:
    expected={mod+p for p in ('Plugins/AircraftAutopilotContinued.dll','Icons/AppLauncherIcon.png','LICENSE','README.md','THIRD_PARTY_NOTICES.md','docs/TESTING.md','docs/PORTING_NOTES.md','docs/PUBLIC_API.md')}
    assert set(z.namelist())==expected, z.namelist()
    assert z.read(mod+'Plugins/AircraftAutopilotContinued.dll')==(r/'build/AircraftAutopilotContinued.dll').read_bytes()
icon=(r/'assets/AppLauncherIcon.png').read_bytes()
assert struct.unpack('>II',icon[16:24])==(38,38)
with zipfile.ZipFile(r/'dist/AircraftAutopilotContinued-1.0.0-source.zip') as z:
    assert 'Source/AircraftAutopilotContinued/AircraftAutopilotController.cs' in z.namelist()
    assert not any('KSP_osx' in p or 'MechJeb2-dev' in p or p.endswith('.dll') for p in z.namelist())
addon=(r/'Source/AircraftAutopilotContinued/AircraftAutopilotAddon.cs').read_text()
visible=addon.split('public void SetVisible(bool visible)',1)[1].split('void VesselChanged',1)[0]
assert 'SetEngaged' not in visible and 'Disengage(' not in visible
print('PASS: package hierarchy, DLL identity, 38x38 icon, source package, window/controller separation')
installed=r/'Source/KSP_osx'/mod/'Plugins/AircraftAutopilotContinued.dll'
if "--installed" in sys.argv:
    assert installed.exists(), "Installed DLL is missing"
    assert installed.read_bytes()==(r/'build/AircraftAutopilotContinued.dll').read_bytes(), 'Installed DLL differs: rerun install.sh'
    print('PASS: installed DLL matches build: '+hashlib.sha256(installed.read_bytes()).hexdigest())
