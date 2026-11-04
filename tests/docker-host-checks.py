"""Checks an already-started own mock Compose service; never starts/stops unrelated services."""
import argparse,subprocess,os,pathlib,json,urllib.request,urllib.error,http.client,time
root=pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--project');parser.add_argument('--faults',action='store_true');args=parser.parse_args()
docker=os.environ.get('DOCKER','docker');compose=[docker,'compose','-f',str(root/'compose.yaml')]
if args.project:compose+=['-p',args.project]
opener=urllib.request.build_opener(urllib.request.ProxyHandler({}));count=0;tokens=[]
def check(ok,name):
 global count
 assert ok,name
 count+=1;print('PASS',name,flush=True)
def request(path,body=None,token=None,fault=None,timeout=3):
 headers={}
 if body is not None:headers['Content-Type']='application/json'
 if token:headers['Authorization']='Bearer '+token
 if fault:headers['X-Demo-Fault']=fault
 req=urllib.request.Request('http://127.0.0.1:5077'+path,data=None if body is None else json.dumps(body).encode(),headers=headers)
 try:
  with opener.open(req,timeout=timeout) as r:return r.status,json.load(r)
 except urllib.error.HTTPError as e:return e.code,None
def login(fault=None):
 code,data=request('/login',{'username':'demo','password':'local-demo-only'},fault=fault)
 if code==200:tokens.append(data['token'])
 return code,data
code,health=request('/health');check(code==200 and health['status']=='ok','published host port reaches Docker health API')
check(health['demoFaults']==args.faults,'demo-fault mode matches requested Compose configuration')
if not args.faults:
 cid=subprocess.check_output(compose+['ps','-q','mock-server'],text=True).strip();assert cid
 details=json.loads(subprocess.check_output([docker,'inspect','--format','{{json .Config.User}}|{{json .HostConfig.ReadonlyRootfs}}|{{json .NetworkSettings.Ports}}',cid],text=True).split('|')[2])
 flags=subprocess.check_output([docker,'inspect','--format','{{.Config.User}} {{.HostConfig.ReadonlyRootfs}}',cid],text=True).strip().split()
 check(flags[0]=='app','runtime container uses non-root app user');check(flags[1]=='true','runtime root filesystem is read-only')
 check(details['5077/tcp']==[{'HostIp':'127.0.0.1','HostPort':'5077'}],'port published only on host IPv4 loopback')
 check(request('/world')[0]==401,'anonymous world request rejected through published port')
 check(request('/login',{'username':'demo','password':'wrong'})[0]==401,'incorrect credentials rejected through Docker')
 code,data=login();check(code==200 and len(data['token'])==64,'Docker login issues session');token=data['token']
 code,data=request('/characters?serverId=meadow',token=token);check(code==200 and data['characters'][0]['name']=='Meadow Hero','Docker character list payload')
 code,data=request('/position',{'characterId':'demo-1','x':2,'y':-3},token);check(code==200 and data['character']['y']==-3,'Docker position update payload')
 check(request('/position',{'characterId':'demo2-1','x':0,'y':0},token)[0]==400,'Docker character ownership validation')
 request('/logout',{},token);check(request('/world',token=token)[0]==401,'Docker logout revokes session')
 code,data=login('login-error');check(code==200,'fault headers inactive without explicit opt-in')
else:
 check(login('login-error')[0]==401,'Docker demo login rejection')
 started=time.monotonic()
 try:login_result=request('/login',{'username':'demo','password':'local-demo-only'},fault='timeout',timeout=.5)
 except TimeoutError:check(.4<=time.monotonic()-started<2,'Docker demo timeout response remains bounded')
 else:raise AssertionError('Expected timeout')
 code,data=login('disconnect');check(code==200,'Docker disconnect scenario logs in before dropping position');token=data['token']
 try:request('/position',{'characterId':'demo-1','x':0,'y':0},token,fault='disconnect')
 except (http.client.RemoteDisconnected,ConnectionResetError,ConnectionAbortedError):check(True,'Docker demo aborts position connection')
 except urllib.error.URLError as error:
  if not isinstance(error.reason,(http.client.RemoteDisconnected,ConnectionResetError,ConnectionAbortedError)):raise
  check(True,'Docker demo aborts position connection')
 else:raise AssertionError('Expected dropped connection')
 check(request('/health')[0]==200,'server stays healthy after timeout/drop')
log=subprocess.check_output(compose+['logs','--no-color','mock-server'],text=True)
check('local-demo-only' not in log and all(t not in log for t in tokens),'container logs omit test passwords and issued tokens')
print(f'{count} Docker host checks passed',flush=True)
