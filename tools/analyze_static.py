"""Static-only: does not load or execute supplied PE files; never emits raw strings."""
import pathlib,hashlib,json,re,struct,sys
import pefile
root=pathlib.Path(sys.argv[1]); out=pathlib.Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True)
rows=[];pes=[]
for p in sorted(root.rglob('*')):
 if not p.is_file():continue
 b=p.read_bytes();rel=str(p.relative_to(root));rows.append(dict(path=rel,size=len(b),sha256=hashlib.sha256(b).hexdigest()))
 if b[:2]!=b'MZ':continue
 pe=pefile.PE(data=b);pe.parse_data_directories()
 sections=[dict(name=s.Name.rstrip(b'\0').decode(errors='replace'),rva=s.VirtualAddress,virtual_size=s.Misc_VirtualSize,raw_size=s.SizeOfRawData,entropy=round(s.get_entropy(),3),flags=hex(s.Characteristics)) for s in pe.sections]
 imports=[dict(dll=i.dll.decode(errors='replace'),symbols=[x.name.decode(errors='replace') if x.name else 'ordinal:'+str(x.ordinal) for x in i.imports]) for i in getattr(pe,'DIRECTORY_ENTRY_IMPORT',[])]
 exports=[dict(name=x.name.decode(errors='replace') if x.name else None,rva=x.address,ordinal=x.ordinal) for x in getattr(getattr(pe,'DIRECTORY_ENTRY_EXPORT',None),'symbols',[])]
 ver=[]
 for group in getattr(pe,'FileInfo',[]):
  for item in group:
   if item.Key==b'StringFileInfo':
    for table in item.StringTable:
     ver.append({k.decode():v.decode(errors='replace') for k,v in table.entries.items() if k in [b'FileVersion',b'ProductVersion',b'OriginalFilename',b'ProductName']})
 strings=re.findall(rb'[\x20-\x7e]{5,}',b)
 pes.append(dict(path=rel,machine=hex(pe.FILE_HEADER.Machine),pe_magic=hex(pe.OPTIONAL_HEADER.Magic),image_base=hex(pe.OPTIONAL_HEADER.ImageBase),entry_rva=hex(pe.OPTIONAL_HEADER.AddressOfEntryPoint),clr_header=bool(pe.OPTIONAL_HEADER.DATA_DIRECTORY[14].VirtualAddress),versions=ver,sections=sections,imports=imports,exports=exports,ascii_strings_count=len(strings),url_like_strings_count=sum(bool(re.search(rb'https?://',s)) for s in strings),unity_versions=sorted(set(x.decode() for x in re.findall(rb'20\d\d\.\d+\.\d+[fpab]\d+',b)))))
meta=next(root.rglob('global-metadata.dat'));b=meta.read_bytes();magic,version=struct.unpack_from('<II',b)
versions={}
for p in root.rglob('*'):
 if p.is_file() and p.suffix not in ['.dll','.exe','.ini']:
  matches=sorted(set(x.decode() for x in re.findall(rb'20\d\d\.\d+\.\d+[fpab]\d+',p.read_bytes())))
  if matches:versions[str(p.relative_to(root))]=matches
archive=pathlib.Path('/workspace/attachments/1d5ad6e1-847d-48d3-bbd5-0c6d74013445/mabu6sao.rar')
(out/'inventory.json').write_text(json.dumps(dict(archive_size=archive.stat().st_size,archive_sha256=hashlib.sha256(archive.read_bytes()).hexdigest(),extracted_files=rows),indent=2))
(out/'pe-analysis.json').write_text(json.dumps(pes,indent=2))
(out/'runtime.json').write_text(json.dumps(dict(metadata_magic=hex(magic),metadata_version=version,unity_versions=versions),indent=2))
print('Files:',len(rows),'total bytes:',sum(x['size'] for x in rows),'metadata:',hex(magic),version)
for p in pes:print(p['path'],p['machine'],'CLR',p['clr_header'],p['versions'],p['unity_versions'],'imports',len(p['imports']),'exports',len(p['exports']))
print('Unity versions:',versions)
