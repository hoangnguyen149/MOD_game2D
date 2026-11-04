using System.Diagnostics;
namespace OwnedLauncher;
public sealed record ClientState(Guid ProfileId,int Pid,string Status,int? ExitCode);
public sealed class ProcessSupervisor:IDisposable{
 sealed class Running(Guid profileId,Process process){public Guid ProfileId{get;}=profileId;public Process Process{get;}=process;public bool StopRequested;}
 readonly List<Running> children=[];readonly object gate=new();readonly EventLog log;int maxClients=2;
 public ProcessSupervisor(EventLog eventLog){log=eventLog;}
 public void SetLimit(int value){if(value<1 || value>8)throw new ArgumentOutOfRangeException(nameof(value));lock(gate){if(children.Count(p=>!p.Process.HasExited)>value)throw new InvalidOperationException("Dừng bớt client trước khi giảm giới hạn.");maxClients=value;}}
 public static string ValidatePath(string path){
  if(string.IsNullOrWhiteSpace(path))throw new InvalidDataException("Chưa chọn client.");
  var full=Path.GetFullPath(path);if(!File.Exists(full))throw new FileNotFoundException("Không tìm thấy client.");
  if(OperatingSystem.IsWindows()){
   if(!string.Equals(Path.GetExtension(full),".exe",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Chọn tệp EXE của client do bạn build.");
   using var stream=File.OpenRead(full);using var reader=new BinaryReader(stream);
   if(stream.Length<64 || reader.ReadUInt16()!=0x5a4d)throw new InvalidDataException("Client không có định dạng PE hợp lệ.");
   stream.Position=0x3c;var offset=reader.ReadInt32();if(offset<64 || offset>stream.Length-4)throw new InvalidDataException("PE header không hợp lệ.");
   stream.Position=offset;if(reader.ReadUInt32()!=0x4550)throw new InvalidDataException("PE signature không hợp lệ.");
  }
  return full;
 }
 public int Start(Profile profile,string path){
  profile.Validate();var full=ValidatePath(path);
  lock(gate){
   if(children.Any(p=>p.ProfileId==profile.Id && !p.Process.HasExited))throw new InvalidOperationException("Hồ sơ này đang chạy.");
   if(children.Count(p=>!p.Process.HasExited)>=maxClients)throw new InvalidOperationException("Đã đạt giới hạn client.");
   foreach(var dead in children.Where(p=>p.Process.HasExited).ToArray()){dead.Process.Dispose();children.Remove(dead);}
   var info=new ProcessStartInfo(full){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(full)!};
   foreach(var arg in new[]{"-screen-width",profile.Width.ToString(),"-screen-height",profile.Height.ToString(),"-screen-fullscreen","0","--profile-id",profile.Id.ToString(),"--server-id",profile.ServerId})info.ArgumentList.Add(arg);
   // Only a public demo username hint; never a password or token.
   info.Environment["OWNED_TEST_USER"]=profile.Username;
   // No credential transfer: the client owns the login form.
   var process=Process.Start(info)??throw new InvalidOperationException("Không mở được client.");children.Add(new Running(profile.Id,process));log.Write("start");return process.Id;
  }
 }
 public IReadOnlyList<ClientState> Snapshot(){lock(gate)return children.Select(p=>new ClientState(p.ProfileId,p.Process.Id,p.Process.HasExited?(p.StopRequested||p.Process.ExitCode==0?"Đã dừng":"Lỗi"):"Đang chạy",p.Process.HasExited?p.Process.ExitCode:null)).ToArray();}
 public bool IsRunning(Guid id){lock(gate)return children.Any(p=>p.ProfileId==id && !p.Process.HasExited);}
 public void Stop(Guid id){
  lock(gate){foreach(var item in children.Where(p=>p.ProfileId==id)){
   var p=item.Process;if(p.HasExited)continue;item.StopRequested=true;
   if(!p.CloseMainWindow() || !p.WaitForExit(2000)){try{p.Kill(entireProcessTree:true);}catch(InvalidOperationException){}p.WaitForExit(3000);}log.Write("stop");
  }}
 }
 public void Dispose(){lock(gate){foreach(var c in children){Stop(c.ProfileId);c.Process.Dispose();}children.Clear();}}
}
