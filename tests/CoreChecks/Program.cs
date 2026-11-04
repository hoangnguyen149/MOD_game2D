using OwnedGame;
var count=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
var w=new World();w.Move(1,0,.1f);Check(w.X==0,"menu pauses movement");
w.Paused=false;w.Move(1,1,.1f);Check(Math.Abs(w.X-w.Y)<.0001 && Math.Abs(Math.Sqrt(w.X*w.X+w.Y*w.Y)-.4)<.0001,"diagonal normalization");
for(int i=0;i<1000;i++)w.Move(1,1,.1f);Check(w.X==9 && w.Y==5,"map bounds");
w.Damage(1000);Check(w.Hp==0,"HP clamp");w.Damage(-1);Check(w.Hp==0,"negative damage rejected");
for(int i=0;i<10;i++)Check(w.Cast(),"cast spends MP "+i);Check(!w.Cast() && w.Mp==0,"insufficient MP");
var x=w.X;w.Move(float.NaN,0,1);Check(w.X==x,"nonfinite input ignored");
var host=new Host();new SampleMod().Configure(host);Check(host.Theme=="ocean" && host.Fps && host.Key=="Space" && host.Title.Contains("MOD"),"sample mod API");
Check(typeof(World).GetMethod("DebugHeal")==null,"release excludes debug API");
Console.WriteLine($"{count} checks passed");
sealed class Host:IModHost{
 public string Theme="",Key="",Title="";public bool Fps;
 public void SetTheme(string s)=>Theme=s;public void ShowFps(bool b)=>Fps=b;public void Bind(string a,string k)=>Key=k;public void SetHudTitle(string s)=>Title=s;
}
