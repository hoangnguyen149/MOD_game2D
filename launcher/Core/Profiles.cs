using System.Text.Json;
namespace OwnedLauncher;
public sealed class Profile{
 public Guid Id{get;set;}=Guid.NewGuid();public string Name{get;set;}="Demo";public string Username{get;set;}="demo";public string ServerId{get;set;}="meadow";
 public int Width{get;set;}=1024;public int Height{get;set;}=600;
 public void Validate(){if(Id==Guid.Empty || string.IsNullOrWhiteSpace(Name) || Name.Length>60 || Name.Any(char.IsControl) || Username is not ("demo" or "demo2") || ServerId!="meadow" || Width<640 || Width>3840 || Height<360 || Height>2160)throw new InvalidDataException("Hồ sơ không hợp lệ: tên, tài khoản demo, máy chủ hoặc kích thước cửa sổ.");}
}
public sealed class Settings{
 public int Version{get;set;}=1;public string ClientPath{get;set;}="";public int MaxClients{get;set;}=2;public List<Profile> Profiles{get;set;}=[];
 public void Validate(){if(Version!=1 || MaxClients<1 || MaxClients>8 || Profiles==null || Profiles.Count>50 || Profiles.Any(p=>p==null) || Profiles.Select(p=>p.Id).Distinct().Count()!=Profiles.Count)throw new InvalidDataException("Cấu hình launcher không hợp lệ.");foreach(var p in Profiles){if(p==null)throw new InvalidDataException("Hồ sơ trống.");p.Validate();}}
}
public sealed class ProfileStore(string path){
 readonly JsonSerializerOptions options=new(){WriteIndented=true,UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow};
 public Settings Load(){
  if(!File.Exists(path))return new Settings();
  if(new FileInfo(path).Length>131072)throw new InvalidDataException("Cấu hình quá lớn.");
  try{var s=JsonSerializer.Deserialize<Settings>(File.ReadAllText(path),options)??throw new InvalidDataException("Cấu hình rỗng.");s.Validate();return s;}
  catch(JsonException){throw new InvalidDataException("Cấu hình JSON hỏng. Giữ lại tệp cũ và chọn khôi phục cấu hình mới.");}
 }
 public void Save(Settings settings){settings.Validate();var folder=Path.GetDirectoryName(Path.GetFullPath(path))!;Directory.CreateDirectory(folder);var tmp=Path.Combine(folder,Guid.NewGuid()+".tmp");try{File.WriteAllText(tmp,JsonSerializer.Serialize(settings,options));File.Move(tmp,path,true);}finally{if(File.Exists(tmp))File.Delete(tmp);}}
 public void ArchiveBroken(){if(File.Exists(path))File.Move(path,path+".broken-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));}
}
// Events are allowlisted; never accepts arbitrary log messages or profile fields.
public sealed class EventLog(string path){
 readonly object gate=new();
 public void Write(string code){if(code is not ("start" or "stop" or "exit" or "error" or "save"))throw new ArgumentException("Unknown event");lock(gate){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);if(File.Exists(path) && new FileInfo(path).Length>1048576)File.Move(path,path+".1",true);File.AppendAllText(path,DateTime.UtcNow.ToString("O")+" "+code+Environment.NewLine);}}
}
