using System.Text.Json;
using System.Diagnostics;
using OwnedGame.Net;
var count=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
void Reject(Action action,string name){try{action();}catch(Exception e)when(e is GameApiException or JsonException){Check(true,name);return;}throw new Exception("Expected rejection: "+name);}
async Task RejectAsync(Func<Task> action,string expected,string name){try{await action();}catch(GameApiException e){Check(e.Code.Contains(expected,StringComparison.OrdinalIgnoreCase),name);return;}throw new Exception("Expected API rejection: "+name);}
var json=new Wire();var local=new ServerEntry{id="meadow",url="http://127.0.0.1:5077/"};
using(var api=new GameApi(local,json)){
 await RejectAsync(()=>api.Characters("meadow",default),"rejected","anonymous character request rejected");
 await RejectAsync(()=>api.Login("demo","incorrect",default),"rejected","incorrect login rejected");
 await api.Login("demo","local-demo-only",default);Check(api.Authenticated,"shared client authenticates");
 var list=await api.Servers(default);Check(list.servers.Length==1&&list.servers[0].id=="meadow","shared client reads servers");
 var chars=await api.Characters("meadow",default);Check(chars.characters[0].name=="Meadow Hero","shared client reads named character");
 var world=await api.Position(chars.characters[0].id,3,-2,default);Check(world.character.x==3&&world.character.y==-2&&world.character.hp==100,"shared client updates position");
 await RejectAsync(()=>api.Position(chars.characters[0].id,99,0,default),"400","out-of-bounds position rejected");
 await RejectAsync(()=>api.Position("demo2-1",0,0,default),"400","cross-account character rejected");
 await api.Logout(default);Check(!api.Authenticated,"client clears session after logout");
 await RejectAsync(()=>api.Characters("meadow",default),"rejected","logged-out client denied");
}
using(var api=new GameApi(new ServerEntry{id="meadow",url=local.url,fault="login-error"},json))await RejectAsync(()=>api.Login("demo","local-demo-only",default),"rejected","simulated login error");
using(var api=new GameApi(new ServerEntry{id="meadow",url=local.url,fault="timeout"},json)){
 var watch=Stopwatch.StartNew();await RejectAsync(()=>api.Login("demo","local-demo-only",default),"timeout","simulated timeout has clear error");Check(watch.Elapsed.TotalSeconds>=2&&watch.Elapsed.TotalSeconds<5,"timeout bounded without hanging");
}
using(var api=new GameApi(new ServerEntry{id="meadow",url=local.url,fault="disconnect"},json)){
 await api.Login("demo","local-demo-only",default);await RejectAsync(()=>api.Position("demo-1",0,0,default),"disconnected","simulated dropped connection");Check(!api.Authenticated,"transport failure clears session");
}
Reject(()=>GameApi.ValidateServer(new ServerEntry{id="x",url="https://example.com/"}),"remote server configuration rejected");
Reject(()=>GameApi.ValidateServer(new ServerEntry{id="x",url="http://127.0.0.1:5077/?x=1"}),"query injection in base URL rejected");
Reject(()=>new ClientPreferences{cast="W"}.Validate(),"duplicate input keys rejected");Reject(()=>new ClientPreferences{up="not-a-key"}.Validate(),"invalid input keys rejected");Reject(()=>new ClientPreferences{volume=float.NaN}.Validate(),"nonfinite volume rejected");
var folder=Path.Combine(Path.GetTempPath(),"Owned Settings "+Guid.NewGuid());Directory.CreateDirectory(folder);
try{
 var store=new ConfigurationStore(json);var path=Path.Combine(folder,"settings.json");Check(store.LoadPreferences(path).cast=="Space","missing settings defaults");
 store.SavePreferences(path,new ClientPreferences{volume=.3f,cast="F",theme="sunset"});var p=store.LoadPreferences(path);Check(p.volume==.3f&&p.cast=="F"&&p.theme=="sunset","volume, input, HUD preferences roundtrip");
 File.WriteAllText(path,"{bad");Reject(()=>store.LoadPreferences(path),"broken settings reported");Check(File.ReadAllText(path)=="{bad","broken settings preserved");
}catch{throw;}finally{Directory.Delete(folder,true);}
Console.WriteLine($"{count} API/configuration checks passed");
sealed class Wire:IWireJson{
 readonly JsonSerializerOptions options=new(){IncludeFields=true};public string Encode<T>(T value)=>JsonSerializer.Serialize(value,options);public T Decode<T>(string text)=>JsonSerializer.Deserialize<T>(text,options)!;
}
