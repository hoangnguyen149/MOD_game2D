using System;
using System.IO;
using System.Linq;
namespace OwnedGame.Net
{
 [Serializable] public sealed class ClientPreferences
 {
  public float volume=.7f;public int width=1024;public int height=600;
  public string up="W",down="S",left="A",right="D",cast="Space",theme="ocean";public bool fps=true;
  public void Validate(){
   if(float.IsNaN(volume)||float.IsInfinity(volume)||volume<0||volume>1||width<640||width>3840||height<360||height>2160)throw new GameApiException("Invalid volume or resolution");
   var keys=new[]{up,down,left,right,cast};
   if(keys.Any(k=>!ValidKey(k))||keys.Distinct().Count()!=5)throw new GameApiException("Keys must be distinct letters, arrows or Space");
   if(theme!="ocean" && theme!="sunset")throw new GameApiException("Invalid HUD theme");
  }
  static bool ValidKey(string k){return k!=null && ((k.Length==1&&k[0]>='A'&&k[0]<='Z')||k=="UpArrow"||k=="DownArrow"||k=="LeftArrow"||k=="RightArrow"||k=="Space");}
 }
 // Only configuration types are passed by the game; sessions/passwords never use this store.
 public sealed class ConfigurationStore
 {
  readonly IWireJson json;
  public ConfigurationStore(IWireJson wire){json=wire;}
  public ClientPreferences LoadPreferences(string path){if(!File.Exists(path))return new ClientPreferences();var p=json.Decode<ClientPreferences>(Read(path));if(p==null)throw new GameApiException("Empty settings");p.Validate();return p;}
  public ServerList LoadServers(string path){var list=json.Decode<ServerList>(Read(path));if(list==null||list.servers==null||list.servers.Length<1||list.servers.Length>12)throw new GameApiException("Invalid server list");foreach(var s in list.servers)GameApi.ValidateServer(s);return list;}
  public void SavePreferences(string path,ClientPreferences value){value.Validate();var folder=Path.GetDirectoryName(Path.GetFullPath(path));Directory.CreateDirectory(folder);var temp=Path.Combine(folder,Guid.NewGuid()+".tmp");try{File.WriteAllText(temp,json.Encode(value));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  string Read(string path){if(new FileInfo(path).Length>65536)throw new GameApiException("Configuration too large");return File.ReadAllText(path);}
 }
}
