using System.Text.Json;
using OwnedGame.Net;
if(Environment.GetEnvironmentVariable("OWNED_PROBE_FAIL")=="1")return 7;
using var api=new GameApi(new ServerEntry{id="meadow",url="http://127.0.0.1:5077/"},new JsonWire());
try{
 // Public synthetic test fixture only, never user credentials. Production client prompts for a password.
 await api.Login(Environment.GetEnvironmentVariable("OWNED_TEST_USER")??"demo","local-demo-only",CancellationToken.None);
 var servers=await api.Servers(CancellationToken.None);if(servers.servers.Length!=1)throw new Exception("server list");
 var chars=await api.Characters("meadow",CancellationToken.None);if(chars.characters.Length!=1)throw new Exception("character list");
 var world=await api.Position(chars.characters[0].id,1.25f,2.5f,CancellationToken.None);
 if(world.character.x!=1.25f||world.character.y!=2.5f)throw new Exception("position");
 var resultPath=Environment.GetEnvironmentVariable("OWNED_PROBE_RESULT");if(!string.IsNullOrEmpty(resultPath))File.WriteAllText(resultPath,"ok");
 Console.WriteLine("PASS client probe: login, servers, characters, position (not Unity)");
 if(Environment.GetEnvironmentVariable("OWNED_PROBE_HOLD")=="1")await Task.Delay(TimeSpan.FromMinutes(2));
 await api.Logout(CancellationToken.None);if(api.Authenticated)throw new Exception("logout");
}catch(Exception){Console.Error.WriteLine("FAIL client probe (details intentionally omitted)");return 1;}
return 0;
sealed class JsonWire:IWireJson{
 readonly JsonSerializerOptions options=new(){IncludeFields=true};
 public string Encode<T>(T value)=>JsonSerializer.Serialize(value,options);public T Decode<T>(string text)=>JsonSerializer.Deserialize<T>(text,options)!;
}
