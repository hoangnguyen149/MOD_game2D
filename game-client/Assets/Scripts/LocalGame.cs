using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using OwnedGame;
using OwnedGame.Net;
public sealed class LocalGame:MonoBehaviour,IModHost
{
 sealed class UnityJson:IWireJson{public string Encode<T>(T value){return JsonUtility.ToJson(value);}public T Decode<T>(string value){return JsonUtility.FromJson<T>(value);}}
 const int UiWidth=1024,UiHeight=600;
 readonly World world=new World();readonly CancellationTokenSource lifetime=new CancellationTokenSource();
 ConfigurationStore store;ClientPreferences prefs=new ClientPreferences();ServerList servers=new ServerList();GameApi api;
 AudioSource tone;AudioClip castSound;
 Transform actor;string username="demo",password="",characterId="",characterName="",status="Đăng nhập máy chủ local để bắt đầu",title="OWNED ADVENTURE";
 bool connected,busy,syncing,alive=true,showFps;int selectedServer,spawned;float fps,nextSync;Color accent=Color.cyan;
 string prefsPath;readonly string[] actions={"up","down","left","right","cast"};string[] keys={"W","S","A","D","Space"};string[] editKeys={"W","S","A","D","Space"};
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot(){if(FindFirstObjectByType<LocalGame>()==null)new GameObject("OwnedGame").AddComponent<LocalGame>();}
 void Awake(){
  Application.targetFrameRate=60;store=new ConfigurationStore(new UnityJson());prefsPath=Path.Combine(Application.persistentDataPath,"settings.json");
  var envUser=Environment.GetEnvironmentVariable("OWNED_TEST_USER");if(envUser=="demo"||envUser=="demo2")username=envUser;
  var cam=Camera.main;if(cam==null){cam=new GameObject("Camera").AddComponent<Camera>();cam.tag="MainCamera";}
  if(cam.GetComponent<AudioListener>()==null)cam.gameObject.AddComponent<AudioListener>();
  tone=gameObject.AddComponent<AudioSource>();castSound=AudioClip.Create("Owned sine tone",2205,1,22050,false);var samples=new float[2205];for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*440*2*Mathf.PI/22050)*.08f;castSound.SetData(samples,0);
  cam.transform.position=new Vector3(0,0,-10);cam.orthographic=true;cam.orthographicSize=6;cam.backgroundColor=new Color(.04f,.07f,.12f);
  for(int y=-5;y<=5;y++)for(int x=-9;x<=9;x++)Square("Tile",new Vector2(x,y),new Vector2(.97f,.97f),((x+y)&1)==0?new Color(.12f,.25f,.2f):new Color(.14f,.29f,.23f),0);
  actor=Square("Player",Vector2.zero,new Vector2(.6f,.8f),Color.white,2).transform;
  new SampleMod().Configure(this);
  try{prefs=store.LoadPreferences(prefsPath);ApplyPreferences(false);}catch(Exception){status="Cấu hình cài đặt hỏng; dùng mặc định, tệp cũ được giữ nguyên.";}
  LoadServers();
 }
 void LoadServers(){
  try{
   var external=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Data","servers.json");
   servers=store.LoadServers(File.Exists(external)?external:Path.Combine(Application.streamingAssetsPath,"servers.json"));
   var args=Environment.GetCommandLineArgs();var i=Array.IndexOf(args,"--server-id");if(i>=0&&i+1<args.Length){var index=Array.FindIndex(servers.servers,s=>s.id==args[i+1]);if(index>=0)selectedServer=index;}
  }catch(Exception){servers=new ServerList();status="Cấu hình máy chủ hỏng. Sửa Data/servers.json rồi tải lại.";}
 }
 GameObject Square(string name,Vector2 pos,Vector2 size,Color color,int order){var go=new GameObject(name);go.transform.position=pos;go.transform.localScale=size;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);sr.color=color;sr.sortingOrder=order;return go;}
 bool Held(string key){return Input.GetKey((KeyCode)Enum.Parse(typeof(KeyCode),key));}
 void Update(){
  fps=Mathf.Lerp(fps,1/Mathf.Max(Time.unscaledDeltaTime,.001f),.1f);
  if(connected&&Input.GetKeyDown(KeyCode.Escape))world.Paused=!world.Paused;
  if(!connected)return;
  if(!world.Paused){
  world.Move((Held(keys[3])?1:0)-(Held(keys[2])?1:0),(Held(keys[0])?1:0)-(Held(keys[1])?1:0),Time.deltaTime);
  actor.position=new Vector3(world.X,world.Y,0);
  if(Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode),keys[4]))&&world.Cast()){status="Đã dùng kỹ năng local (-5 MP)";tone.PlayOneShot(castSound);}
  }
  if(!syncing&&Time.unscaledTime>=nextSync){nextSync=Time.unscaledTime+.4f;SyncPosition();}
 }
 async void Login(){
  if(busy||servers.servers.Length==0)return;busy=true;world.Paused=true;var entered=password;password="";
  try{
   api?.Dispose();api=new GameApi(servers.servers[selectedServer],new UnityJson());
   await api.Login(username,entered,lifetime.Token);entered="";
   var available=await api.Servers(lifetime.Token);
   if(available==null||available.servers==null||!available.servers.Any(s=>s.id==servers.servers[selectedServer].id))throw new GameApiException("Unknown server");
   var characters=await api.Characters(servers.servers[selectedServer].id,lifetime.Token);
   if(characters==null||characters.characters==null||characters.characters.Length==0)throw new GameApiException("No demo characters");
   var c=characters.characters[0];world.ApplySnapshot(c.x,c.y,c.hp,c.mp);characterId=c.id;characterName=c.name;
   if(!alive)return;connected=true;world.Paused=false;status="Đã kết nối local: "+servers.servers[selectedServer].name;
  }catch(GameApiException e){if(alive)Disconnected(e.Code);}
  catch(Exception){if(alive)Disconnected("Không đọc được phản hồi máy chủ local.");}
  finally{entered="";busy=false;}
 }
 async void SyncPosition(){
  syncing=true;
  try{var response=await api.Position(characterId,world.X,world.Y,lifetime.Token);if(response==null||response.character==null||response.character.id!=characterId)throw new GameApiException("Invalid position reply");}
  catch(GameApiException e){if(alive)Disconnected(e.Code);}
  catch(Exception){if(alive)Disconnected("Mất kết nối máy chủ local.");}
  finally{syncing=false;}
 }
 void Disconnected(string reason){connected=false;world.Paused=true;status=reason+" — kiểm tra mock server rồi đăng nhập lại.";api?.Dispose();api=null;}
 void ApplyPreferences(bool resize){prefs.Validate();keys=new[]{prefs.up,prefs.down,prefs.left,prefs.right,prefs.cast};editKeys=(string[])keys.Clone();AudioListener.volume=prefs.volume;SetTheme(prefs.theme);ShowFps(prefs.fps);if(resize)Screen.SetResolution(prefs.width,prefs.height,false);}
 void SaveSettings(){
  try{prefs.up=editKeys[0];prefs.down=editKeys[1];prefs.left=editKeys[2];prefs.right=editKeys[3];prefs.cast=editKeys[4];prefs.Validate();store.SavePreferences(prefsPath,prefs);ApplyPreferences(true);status="Đã lưu cài đặt.";}
  catch(Exception){status="Cài đặt không hợp lệ: phím cần khác nhau, dùng chữ A–Z, Space hoặc phím mũi tên.";}
 }
 public void SetTheme(string theme){if(theme!="ocean"&&theme!="sunset")throw new ArgumentException("Invalid theme");accent=theme=="ocean"?Color.cyan:new Color(1,.55f,.25f);}
 public void ShowFps(bool enabled){showFps=enabled;}
 public void Bind(string action,string key){if(action!="cast")throw new ArgumentException("Unknown action");var p=new ClientPreferences{cast=key};p.Validate();keys[4]=key;}
 public void SetHudTitle(string text){title=string.IsNullOrWhiteSpace(text)?"OWNED ADVENTURE":text.Substring(0,Math.Min(60,text.Length));}
 void OnGUI(){
  GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/(float)UiWidth,Screen.height/(float)UiHeight,1));
  GUI.color=accent;GUI.Box(new Rect(12,12,430,125),title);GUI.color=Color.white;
  GUI.Label(new Rect(25,40,410,25),(connected?characterName:"Chưa đăng nhập")+" · HP "+world.Hp+" · MP "+world.Mp+(showFps?" · FPS "+fps.ToString("F0"):""));
  GUI.Label(new Rect(25,65,410,25),connected?"Đã kết nối · Esc: menu":"Chưa kết nối");GUI.Label(new Rect(25,90,410,40),status);
  if(!connected){LoginPanel();return;}
  if(world.Paused)SettingsPanel();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  GUILayout.BeginArea(new Rect(UiWidth-220,12,205,210),GUI.skin.box);GUILayout.Label("LOCAL DEVELOPMENT DEBUG");
  GUILayout.Label("Speed "+world.Speed.ToString("F1"));world.DebugSpeed(GUILayout.HorizontalSlider(world.Speed,1,12));
  if(GUILayout.Button("Heal HP / MP"))world.DebugHeal();if(GUILayout.Button("Test damage -10"))world.Damage(10);
  if(GUILayout.Button("Spawn object")&&spawned<50){Square("Debug object",new Vector2(world.X,world.Y),Vector2.one*.3f,Color.magenta,3);spawned++;}GUILayout.EndArea();
#endif
 }
 void LoginPanel(){
  GUI.Box(new Rect(UiWidth/2-190,155,380,420),"ĐĂNG NHẬP LOCAL");GUILayout.BeginArea(new Rect(UiWidth/2-170,185,340,380));
  GUI.enabled=!busy;GUILayout.Label("Tài khoản thử nghiệm (demo / demo2)");username=GUILayout.TextField(username,40);
  GUILayout.Label("Mật khẩu (không lưu)");password=GUILayout.PasswordField(password,'*',128);
  for(int i=0;i<servers.servers.Length;i++){if(GUILayout.Toggle(selectedServer==i,servers.servers[i].name))selectedServer=i;}
  GUI.enabled=!busy&&servers.servers.Length>0;if(GUILayout.Button(busy?"Đang kết nối…":"Đăng nhập"))Login();GUI.enabled=!busy;
  if(GUILayout.Button("Tải lại cấu hình máy chủ")){selectedServer=0;LoadServers();}if(GUILayout.Button("Thoát"))Application.Quit();GUI.enabled=true;GUILayout.EndArea();
 }
 void SettingsPanel(){
  GUI.Box(new Rect(UiWidth/2-190,155,380,430),"CÀI ĐẶT");GUILayout.BeginArea(new Rect(UiWidth/2-170,185,340,390));
  GUILayout.Label("Âm lượng: "+prefs.volume.ToString("F2"));prefs.volume=GUILayout.HorizontalSlider(prefs.volume,0,1);AudioListener.volume=prefs.volume;
  GUILayout.BeginHorizontal();if(GUILayout.Button("1024 × 600")){prefs.width=1024;prefs.height=600;}if(GUILayout.Button("1280 × 720")){prefs.width=1280;prefs.height=720;}GUILayout.EndHorizontal();
  prefs.fps=GUILayout.Toggle(prefs.fps,"Hiển thị FPS");if(GUILayout.Button("HUD: "+prefs.theme))prefs.theme=prefs.theme=="ocean"?"sunset":"ocean";
  for(int i=0;i<keys.Length;i++){GUILayout.BeginHorizontal();GUILayout.Label(actions[i],GUILayout.Width(60));editKeys[i]=GUILayout.TextField(editKeys[i],12);GUILayout.EndHorizontal();}
  if(GUILayout.Button("Lưu và áp dụng"))SaveSettings();if(GUILayout.Button("Tiếp tục")){try{prefs.up=editKeys[0];prefs.down=editKeys[1];prefs.left=editKeys[2];prefs.right=editKeys[3];prefs.cast=editKeys[4];ApplyPreferences(false);world.Paused=false;}catch(Exception){status="Phím/cài đặt không hợp lệ. Sửa rồi lưu.";}}
  if(GUILayout.Button("Đăng xuất")){Disconnected("Đã đăng xuất local");}GUILayout.EndArea();
 }
 void OnDestroy(){alive=false;password="";lifetime.Cancel();api?.Dispose();lifetime.Dispose();if(castSound!=null)Destroy(castSound);}
}
