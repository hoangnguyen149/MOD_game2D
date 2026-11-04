"""Package only our own code/artifacts; reference-game binaries are never included."""
from pathlib import Path
import argparse,zipfile,shutil,hashlib,json
root=Path(__file__).resolve().parents[1];out=root/'downloads';out.mkdir(exist_ok=True)
parser=argparse.ArgumentParser();parser.add_argument('--legacy-dir',type=Path);args=parser.parse_args()
if args.legacy_dir:
 for name in ['OwnedAdventure-source-reports.zip','OwnedAdventure-Windows-Tools-only.zip']:
  source=args.legacy_dir/name
  if not source.is_file():raise RuntimeError('Legacy archive missing: '+name)
  shutil.copyfile(source,out/name)
for folder in ['Server','Launcher']:
 source=root/'artifacts/windows-selfcontained'/folder
 if not source.is_dir():raise RuntimeError('Publish Windows tools before packaging')
 with zipfile.ZipFile(out/f'OwnedAdventure-v2-Windows-{folder}.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
  for p in sorted(source.rglob('*')):
   if p.is_file():z.write(p,Path(folder)/p.relative_to(source))
  z.writestr('README.txt',f'Owned Adventure v2 {folder}, Windows x64, self-contained .NET runtime. Cross-published; NOT executed on Windows. Unity client is NOT included.\nExtract all files; run {folder}/'+('MockServer.exe' if folder=='Server' else 'Launcher.exe')+'.\nSee docs/C-HUONG-DAN.md in the source archive or repository. Use only synthetic local accounts.\n')
# Excludes downloads to prevent recursive inclusion of artifacts, all build state and reference inputs.
with zipfile.ZipFile(out/'OwnedAdventure-v2-source-reports.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in sorted(root.rglob('*')):
  rel=p.relative_to(root)
  if not p.is_file() or any(s in ['.git','downloads','bin','obj','Builds','artifacts','Library','Temp','Logs','UserSettings','__pycache__'] for s in rel.parts):continue
  z.write(p,Path('OwnedAdventure')/rel)
rows=[]
for p in sorted(out.glob('*.zip')):
 if p.stat().st_size>=100*1024*1024:raise RuntimeError('Archive exceeds GitHub file limit: '+p.name)
 with zipfile.ZipFile(p) as z:
  if z.testzip() is not None:raise RuntimeError('ZIP integrity failed: '+p.name)
 rows.append(dict(file=p.name,bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
(out/'SHA256.json').write_text(json.dumps(rows,indent=2)+'\n')
print(json.dumps(rows,indent=2))
