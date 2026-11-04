"""Own local system only. Starts/stops only its own server and fixtures."""
import subprocess,pathlib,time,urllib.request,urllib.error,json,tempfile,os,shutil
ROOT=pathlib.Path(__file__).resolve().parents[1]
DOTNET=os.environ.get('DOTNET') or shutil.which('dotnet') or '/workspace/dotnet/dotnet'
env=dict(os.environ,DOTNET_CLI_TELEMETRY_OPTOUT='1')
if pathlib.Path('/workspace/dotnet/dotnet').exists():
 env.update(DOTNET_CLI_HOME='/workspace/dotnet-home',DOTNET_ROOT='/workspace/dotnet',NUGET_PACKAGES='/workspace/nuget-packages')
EXE='.exe' if os.name=='nt' else ''
http=urllib.request.build_opener(urllib.request.ProxyHandler({}));count=0

def check(ok,name):
 global count
 assert ok,name
 count+=1;print('PASS',name,flush=True)
def request(path,body=None,token=None):
 headers={}
 if body is not None:headers['Content-Type']='application/json'
 if token:headers['Authorization']='Bearer '+token
 req=urllib.request.Request('http://127.0.0.1:5077'+path,data=body,headers=headers)
 try:
  with http.open(req,timeout=3) as r:return r.status,json.load(r)
 except urllib.error.HTTPError as e:return e.code,None
try:
 request('/health');raise RuntimeError('Port 5077 already serves HTTP; stop existing service before isolated test')
except urllib.error.URLError:pass
with tempfile.TemporaryDirectory() as temp:
 log=open(pathlib.Path(temp)/'server.log','w+')
 server=subprocess.Popen([DOTNET,str(ROOT/'mock-server/bin/Release/net10.0/MockServer.dll'),'--DemoFaults=true'],cwd=ROOT,env=env,stdout=log,stderr=subprocess.STDOUT)
 try:
  for _ in range(100):
   if server.poll() is not None:raise RuntimeError('Our server exited before readiness')
   try:
    code,data=request('/health')
    if code==200:break
   except (urllib.error.URLError,TimeoutError):pass
   time.sleep(.1)
  else:raise RuntimeError('Server readiness timeout')
  check(data['status']=='ok' and data['demoFaults'],'health and explicit demo-fault mode')
  check(request('/world')[0]==401,'world requires session')
  check(request('/characters?serverId=meadow')[0]==401,'character list requires session')
  check(request('/login',b'{broken')[0]==400,'malformed JSON rejected')
  check(request('/login',b'null')[0]==401,'null login rejected')
  check(request('/login',b'{"username":"demo","password":"wrong"}')[0]==401,'incorrect login rejected')
  code,reply=request('/login',b'{"username":"demo","password":"local-demo-only"}')
  token=reply['token'];check(code==200 and len(token)==64,'test account session issued')
  check(request('/characters?serverId=invalid',token=token)[0]==400,'unknown server rejected')
  check(request('/position',b'{"characterId":"demo-1","x":99,"y":0}',token)[0]==400,'map bounds validated')
  check(request('/position',b'{"characterId":"demo2-1","x":0,"y":0}',token)[0]==400,'character ownership validated')
  check(request('/position',b'{"characterId":"demo-1","x":"NaN","y":0}',token)[0]==400,'invalid coordinate type rejected')
  code,reply=request('/position',b'{"characterId":"demo-1","x":2,"y":3}',token)
  check(code==200 and reply['character']['x']==2,'position update succeeds')
  check(request('/world',token=token)[1]['character']['y']==3,'position retained within session')
  request('/logout',b'',token);check(request('/world',token=token)[0]==401,'logout revokes server session')
  for name,extra in [('ApiChecks',[]),('LauncherChecks',[str(ROOT/('tests/ClientProbe/bin/Release/net10.0/ClientProbe'+EXE))])]:
   result=subprocess.run([DOTNET,str(ROOT/f'tests/{name}/bin/Release/net10.0/{name}.dll'),*extra],cwd=ROOT,env=env,text=True,capture_output=True,timeout=45)
   print(result.stdout,end='',flush=True)
   if result.returncode:raise RuntimeError(name+' failed: '+result.stderr[:2000])
  log.flush();log.seek(0);text=log.read()
  check('local-demo-only' not in text and token not in text and 'Authorization:' not in text,'server logs contain no password/token')
 finally:
  server.terminate()
  try:server.wait(timeout=5)
  except subprocess.TimeoutExpired:server.kill();server.wait()
  log.close()
print(f'{count} HTTP/log checks passed',flush=True)
