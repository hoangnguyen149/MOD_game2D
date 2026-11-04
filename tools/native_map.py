import json,pathlib,sys,collections,re
import pefile,capstone
pe=pefile.PE(sys.argv[1]);s=json.load(open(sys.argv[2]))['ScriptMethod'];out=pathlib.Path(sys.argv[3]);base=pe.OPTIONAL_HEADER.ImageBase
by=collections.defaultdict(list)
for m in s:by[m['Address']].append(m['Name'])
addrs=sorted(by);cs=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32);cs.detail=True
edges=[];details=[]
for i,a in enumerate(addrs[:-1]):
 size=addrs[i+1]-a
 if not 0<size<=65536:continue
 try:code=pe.get_data(a,size)
 except Exception:continue
 for ins in cs.disasm(code,base+a):
  if ins.mnemonic=='call' and ins.operands and ins.operands[0].type==capstone.x86.X86_OP_IMM:
   target=ins.operands[0].imm-base
   if target in by:edges.append({'caller_rva':hex(a),'caller':by[a][:3],'caller_alias_count':len(by[a]),'instruction_rva':hex(ins.address-base),'callee_rva':hex(target),'callee':by[target][:3],'callee_alias_count':len(by[target])})
 if any(n in ['Main$$Start','Main$$Update','Main$$OnGUI'] for n in by[a]):
  details.append({'rva':hex(a),'aliases':by[a],'instructions':[{'rva':hex(x.address-base),'op':x.mnemonic,'args':x.op_str} for x in cs.disasm(code,base+a)]})
(out/'native-calls.json').write_text(json.dumps(edges,separators=(',',':')));(out/'entry-disassembly.json').write_text(json.dumps(details,indent=2))
selected=[e for e in edges if any(n.startswith('Main$$') for n in e['caller']) or any(n.startswith(('UnityEngine.Input$$','System.Net.Sockets.TcpClient$$','UnityEngine.Resources$$','UnityEngine.PlayerPrefs$$')) for n in e['callee'])]
(out/'selected-calls.json').write_text(json.dumps(selected,indent=2))
print('imagebase',hex(base),'mapped methods',len(s),'direct call sites',len(edges),'selected',len(selected))
for e in selected[:45]:print(e['instruction_rva'],','.join(e['caller'][:2]),'->',','.join(e['callee'][:3]))
