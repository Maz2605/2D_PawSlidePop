"""Read-only architecture/GUID/doc checks. Run with Python 3 from the project root."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
errors = []
base = root / 'Assets/_Base'
for module in ('Foundation', 'Bootstrap', 'Audio', 'Haptics', 'Settings', 'UI', 'SceneManagement', 'Persistence', 'Device', 'Docs'):
    if not (base / module).is_dir():
        errors.append(f'Missing base module: {module}')
for path in base.rglob('*.cs'):
    content = path.read_text(encoding='utf-8-sig')
    if re.search(r'_PawSlidePopGame|using Firebase|using GoogleMobileAds', content):
        errors.append(f'Base depends on game/SDK: {path.relative_to(root)}')
guids = {}
for path in (root / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', path.read_text(encoding='utf-8-sig', errors='replace'), re.M)
    if match:
        guid = match[1]
        if guid in guids:
            errors.append(f'Duplicate GUID: {path.relative_to(root)} / {guids[guid]}')
        guids[guid] = path.relative_to(root)
for docroot in (base / 'Docs', root / 'Assets/_PawSlidePopGame/Docs'):
    if not (docroot / 'README.md').exists():
        errors.append(f'Missing docs index: {docroot.relative_to(root)}')
    for path in docroot.rglob('*.md'):
        for target in re.findall(r'\]\(([^)]+)\)', path.read_text(encoding='utf-8-sig')):
            if '://' not in target and not target.startswith('#'):
                target = target.split('#')[0].strip('<>')
                if target and not (path.parent / target).exists():
                    errors.append(f'Broken docs link: {path.relative_to(root)} -> {target}')
if errors:
    print('\n'.join(errors))
    sys.exit(1)
print(f'PASS: base boundaries, {len(guids)} unique asset GUIDs, and documentation links')
