#!/usr/bin/env python3
from pathlib import Path
import zipfile
root = Path(__file__).resolve().parent.parent
with zipfile.ZipFile(root/'dist/AircraftAutopilotContinued-1.0.0.zip','w',zipfile.ZIP_DEFLATED) as z:
    for p in sorted((root/'dist/GameData').rglob('*')):
        if p.is_file(): z.write(p,p.relative_to(root/'dist'))
# Include corresponding source and build/test scripts for GPL redistribution.
with zipfile.ZipFile(root/'dist/AircraftAutopilotContinued-1.0.0-source.zip','w',zipfile.ZIP_DEFLATED) as z:
    for folder in ('Source/AircraftAutopilotContinued','scripts','tests','docs','assets'):
        for p in sorted((root/folder).rglob('*')):
            if p.is_file(): z.write(p,p.relative_to(root))
    for name in ('LICENSE','README.md','THIRD_PARTY_NOTICES.md','.gitignore'):
        z.write(root/name,name)
