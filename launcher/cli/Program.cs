using System.Diagnostics;
if(args.Length>0 && args[0]=="--help") { Console.WriteLine("Owned Manager: select <full path>, start, stop, status, quit. Only launches your own trusted client; stops only its child process.");return; }
string? selected=null; Process? child=null;
Console.WriteLine("Owned Manager — select <client path> | start | stop | status | quit");
try {
while(true){
 Console.Write("> ");var line=Console.ReadLine();if(line==null || line=="quit")break;
 try {
 if(line.StartsWith("select ",StringComparison.Ordinal)){
  var path=Path.GetFullPath(line[7..].Trim().Trim('"'));
  if(!File.Exists(path))throw new FileNotFoundException("Client does not exist.");
  if(child is {HasExited:false})throw new InvalidOperationException("Stop the current client first.");
  selected=path;Console.WriteLine("Selected: "+Path.GetFileName(selected));
 } else switch(line){
 case "start":
  if(selected==null)throw new InvalidOperationException("Select your client first.");
  if(child is {HasExited:false})throw new InvalidOperationException("Already running.");
  child?.Dispose();child=Process.Start(new ProcessStartInfo(selected){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(selected)!});
  if(child==null)throw new InvalidOperationException("Process did not start.");Console.WriteLine("Started PID "+child.Id);break;
 case "stop":Stop();break;
 case "status":Console.WriteLine(child==null?"Not started":child.HasExited?"Exited: "+child.ExitCode:"Running PID "+child.Id);break;
 default:Console.WriteLine("Unknown command.");break;
 }
 }catch(Exception e) when(e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException or ArgumentException){Console.WriteLine("Error: "+e.Message);}
}
}finally{Stop();child?.Dispose();}
void Stop(){if(child is {HasExited:false}){if(!child.CloseMainWindow() || !child.WaitForExit(2000)){child.Kill(entireProcessTree:true);child.WaitForExit();}Console.WriteLine("Stopped.");}}
