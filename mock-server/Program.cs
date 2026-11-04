using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using OwnedGame.Net;
var builder=WebApplication.CreateBuilder(args);
// Docker port forwarding reaches the bridge interface. Native mode remains loopback-only.
bool containerDemo=builder.Configuration.GetValue<bool>("ContainerDemo");
builder.WebHost.UseUrls(containerDemo?"http://0.0.0.0:5077":"http://127.0.0.1:5077");
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=8192);
builder.Services.ConfigureHttpJsonOptions(o=>o.SerializerOptions.IncludeFields=true);
// No request body/header logging; never include exception text derived from credentials.
builder.Logging.ClearProviders();builder.Logging.AddSimpleConsole(o=>o.SingleLine=true);builder.Logging.SetMinimumLevel(LogLevel.Warning);
var app=builder.Build();
bool enableFaults=builder.Configuration.GetValue<bool>("DemoFaults");
var sessions=new ConcurrentDictionary<string,Session>();
var salt=Encoding.UTF8.GetBytes("OwnedAdventure-demo-only");
var expected=Convert.FromHexString("65362b97377c6633ae64bc328476c7c6d297b61b1e0f078020b0afb7a0758731");
app.Use(async(context,next)=>{
 if(context.Connection.RemoteIpAddress is not { } ip || (!containerDemo && !IPAddress.IsLoopback(ip))){context.Response.StatusCode=403;return;}
 if(enableFaults){
  var fault=context.Request.Headers["X-Demo-Fault"].ToString();
  if(fault=="login-error" && context.Request.Path=="/login"){context.Response.StatusCode=401;return;}
  if(fault=="timeout" && context.Request.Path=="/login"){
   try{await Task.Delay(6000,context.RequestAborted);}catch(OperationCanceledException){return;}
  }
  if(fault=="disconnect" && context.Request.Path=="/position"){context.Abort();return;}
 }
 await next(context);
});
// Serve the independent browser client from the same loopback origin as the mock API.
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions{OnPrepareResponse=context=>context.Context.Response.Headers.CacheControl="no-cache"});
app.MapGet("/health",()=>Results.Ok(new {status="ok",mode="local-mock",demoFaults=enableFaults}));
app.MapGet("/servers",()=>Results.Ok(new ServerList{servers=[new ServerEntry{id="meadow",name="Local Meadow",url="http://127.0.0.1:5077/"}]}));
app.MapPost("/login",(LoginRequest? login)=>{
 if(login==null || login.username is not ("demo" or "demo2") || string.IsNullOrEmpty(login.password) || login.password.Length>128)return Results.Unauthorized();
 var candidate=Rfc2898DeriveBytes.Pbkdf2(login.password,salt,100000,HashAlgorithmName.SHA256,32);
 var valid=CryptographicOperations.FixedTimeEquals(expected,candidate);CryptographicOperations.ZeroMemory(candidate);
 if(!valid)return Results.Unauthorized();
 foreach(var p in sessions)if(p.Value.Expires<DateTimeOffset.UtcNow)sessions.TryRemove(p.Key,out _);
 if(sessions.Count>=100)return Results.StatusCode(429);
 var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
 sessions[token]=new Session(login.username,DateTimeOffset.UtcNow.AddMinutes(30));
 return Results.Ok(new LoginReply{token=token,expiresIn=1800});
});
Session? Authenticate(HttpRequest request){
 var h=request.Headers.Authorization.ToString();
 return h.StartsWith("Bearer ",StringComparison.Ordinal) && sessions.TryGetValue(h[7..],out var s) && s.Expires>DateTimeOffset.UtcNow?s:null;
}
CharacterInfo Character(Session s)=>new(){id=s.User+"-1",name=s.User=="demo"?"Meadow Hero":"Meadow Scout",hp=100,mp=50,x=s.X,y=s.Y};
app.MapGet("/characters",(HttpRequest req,string? serverId)=>{
 var s=Authenticate(req);if(s==null)return Results.Unauthorized();
 if(serverId!="meadow")return Results.BadRequest(new {code="unknown_server"});
 lock(s)return Results.Ok(new CharacterList{characters=[Character(s)]});
});
app.MapGet("/world",(HttpRequest req)=>{
 var s=Authenticate(req);if(s==null)return Results.Unauthorized();
 lock(s)return Results.Ok(new WorldReply{map="meadow",character=Character(s)});
});
app.MapPost("/position",(HttpRequest req,PositionRequest? pos)=>{
 var s=Authenticate(req);if(s==null)return Results.Unauthorized();
 if(pos==null || pos.characterId!=s.User+"-1" || !float.IsFinite(pos.x) || !float.IsFinite(pos.y) || Math.Abs(pos.x)>9 || Math.Abs(pos.y)>5)return Results.BadRequest(new {code="invalid_position"});
 lock(s){s.X=pos.x;s.Y=pos.y;return Results.Ok(new WorldReply{map="meadow",character=Character(s)});}
});
app.MapPost("/logout",(HttpRequest req)=>{
 var h=req.Headers.Authorization.ToString();if(h.StartsWith("Bearer ",StringComparison.Ordinal))sessions.TryRemove(h[7..],out _);
 return Results.Ok(new {status="logged_out"});
});
app.Run();
sealed class Session(string user,DateTimeOffset expires){public string User{get;}=user;public DateTimeOffset Expires{get;}=expires;public float X;public float Y;}
