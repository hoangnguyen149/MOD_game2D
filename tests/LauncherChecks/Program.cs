using OwnedLauncher;
var count=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
void Reject(Action action,string name){try{action();}catch(Exception e) when(e is InvalidDataException or IOException or InvalidOperationException or ArgumentException){Check(true,name);return;}throw new Exception("Expected rejection: "+name);}
var root=Path.Combine(Path.GetTempPath(),"Owned Launcher Checks "+Guid.NewGuid());Directory.CreateDirectory(root);
try{
 var store=new ProfileStore(Path.Combine(root,"profiles.json"));var settings=store.Load();Check(settings.Profiles.Count==0,"missing config defaults");
 var p=new Profile{Name="Demo one"};settings.Profiles.Add(p);store.Save(settings);Check(store.Load().Profiles[0].Name=="Demo one","profile add persists");
 settings.Profiles[0].Name="Demo edited";store.Save(settings);Check(store.Load().Profiles[0].Name=="Demo edited","profile edit persists");
 settings.Profiles.Clear();store.Save(settings);Check(store.Load().Profiles.Count==0,"profile delete persists");
 File.WriteAllText(Path.Combine(root,"profiles.json"),"{bad");Reject(()=>store.Load(),"broken JSON reported");Check(File.ReadAllText(Path.Combine(root,"profiles.json"))=="{bad","broken JSON preserved");
 store.ArchiveBroken();store.Save(settings);Check(Directory.GetFiles(root,"*.broken-*").Length==1,"explicit recovery archives original");
 File.WriteAllText(Path.Combine(root,"profiles.json"),"{\"Password\":\"DO_NOT_LOG_THIS\"}");Reject(()=>store.Load(),"unknown password field rejected");
 File.WriteAllText(Path.Combine(root,"profiles.json"),"{\"Profiles\":[null]}");Reject(()=>store.Load(),"null profile rejected");
 store.Save(settings);var saved=File.ReadAllText(Path.Combine(root,"profiles.json"));Check(!saved.Contains("Password")&&!saved.Contains("Token"),"storage has no password/token fields");
 Reject(()=>ProcessSupervisor.ValidatePath(Path.Combine(root,"missing.exe")),"missing executable rejected");
 var source=Path.GetFullPath(args[0]);var clientDir=Path.Combine(root,"client with spaces");Directory.CreateDirectory(clientDir);
 foreach(var file in Directory.GetFiles(Path.GetDirectoryName(source)!))File.Copy(file,Path.Combine(clientDir,Path.GetFileName(file)));
 var client=Path.Combine(clientDir,Path.GetFileName(source));if(!OperatingSystem.IsWindows())File.SetUnixFileMode(client,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
 var logPath=Path.Combine(root,"launcher.log");using var supervisor=new ProcessSupervisor(new EventLog(logPath));supervisor.SetLimit(1);
 var marker=Path.Combine(root,"client-result.txt");Environment.SetEnvironmentVariable("OWNED_PROBE_RESULT",marker);Environment.SetEnvironmentVariable("OWNED_PROBE_HOLD","1");
 int pid=supervisor.Start(p,client);Check(pid>0&&supervisor.IsRunning(p.Id),"launcher starts selected child from spaced path");
 for(int i=0;i<100&&!File.Exists(marker);i++)await Task.Delay(100);
 Check(File.Exists(marker)&&File.ReadAllText(marker)=="ok","end-to-end launcher -> client probe -> mock login/position");
 Reject(()=>supervisor.Start(p,client),"duplicate profile rejected");var p2=new Profile{Name="Second",Username="demo2"};Reject(()=>supervisor.Start(p2,client),"concurrent process limit enforced");
 Reject(()=>supervisor.SetLimit(0),"invalid process limit rejected");supervisor.SetLimit(2);supervisor.Start(p2,client);Check(supervisor.Snapshot().Count(s=>s.Status=="Đang chạy")==2,"two clients within raised limit");
 Reject(()=>supervisor.SetLimit(1),"limit cannot fall below active count");
 supervisor.Stop(p.Id);Check(!supervisor.IsRunning(p.Id)&&supervisor.IsRunning(p2.Id),"stop affects only selected owned process");supervisor.Stop(p2.Id);Check(supervisor.Snapshot().All(s=>s.Status=="Đã dừng"),"exit status tracked");
 var text=File.ReadAllText(logPath);Check(!text.Contains("local-demo-only")&&!text.Contains("DO_NOT_LOG_THIS")&&!text.Contains("Bearer")&&!text.Contains("Demo one")&&!text.Contains("token",StringComparison.OrdinalIgnoreCase),"logs contain only allowlisted events");
 Environment.SetEnvironmentVariable("OWNED_PROBE_HOLD",null);File.Delete(marker);supervisor.Start(p,client);
 for(int i=0;i<100&&supervisor.IsRunning(p.Id);i++)await Task.Delay(100);
 var exit=supervisor.Snapshot().Single(s=>s.ProfileId==p.Id);Check(exit.Status=="Đã dừng"&&exit.ExitCode==0,"natural client exit observed");
 Environment.SetEnvironmentVariable("OWNED_PROBE_FAIL","1");supervisor.Start(p,client);
 for(int i=0;i<100&&supervisor.IsRunning(p.Id);i++)await Task.Delay(100);
 var failed=supervisor.Snapshot().Single(s=>s.ProfileId==p.Id);Check(failed.Status=="Lỗi"&&failed.ExitCode==7,"nonzero client exit reported as error");Environment.SetEnvironmentVariable("OWNED_PROBE_FAIL",null);
}finally{Environment.SetEnvironmentVariable("OWNED_PROBE_FAIL",null);Environment.SetEnvironmentVariable("OWNED_PROBE_RESULT",null);Environment.SetEnvironmentVariable("OWNED_PROBE_HOLD",null);Directory.Delete(root,true);}
Console.WriteLine($"{count} launcher checks passed");
