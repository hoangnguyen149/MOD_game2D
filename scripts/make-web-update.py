"""Small source-only update for the existing Docker checkout. Never packages reference game files."""
from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[1];out=root/'downloads';out.mkdir(exist_ok=True)
archive=out/'OwnedAdventure-web-update.zip'
files=[root/'docker/Dockerfile',root/'mock-server/Program.cs',*(root/'mock-server/wwwroot').rglob('*')]
if not (root/'mock-server/wwwroot/index.html').is_file():raise RuntimeError('Web client source is not available')
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in sorted(files):
  if p.is_file():z.write(p,p.relative_to(root))
 z.writestr('DOC-TRUOC.txt','CAP NHAT CLIENT WEB CHO DOCKER\n\nSao chep cac thu muc mock-server va docker trong ZIP vao thu muc du an dang co compose.yaml (giu dung cap thu muc). Chon thay the cac tep prototype khi duoc hoi.\n\nMo PowerShell tai thu muc du an, chay:\ndocker compose up -d --build --wait mock-server\n\nMo Chrome/Edge: http://127.0.0.1:5077/\nTai khoan gia lap: demo hoac demo2\nMat khau demo cong khai: local-demo-only\n\nDay la client Canvas viet rieng cho prototype, khong phai ban game RAR hay Unity WebGL. Khong can Unity Hub de choi client web.\n')
with zipfile.ZipFile(archive) as z:assert z.testzip() is None
manifest=out/'SHA256.json';rows=json.loads(manifest.read_text())
rows=[x for x in rows if x['file']!=archive.name]
rows.append(dict(file=archive.name,bytes=archive.stat().st_size,sha256=hashlib.sha256(archive.read_bytes()).hexdigest()))
manifest.write_text(json.dumps(rows,indent=2)+'\n');print(json.dumps(rows[-1],indent=2))
