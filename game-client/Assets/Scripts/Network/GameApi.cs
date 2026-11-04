using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace OwnedGame.Net
{
 public sealed class GameApiException:Exception
 {
  public readonly string Code;
  public GameApiException(string code):base(code){Code=code;}
 }
 // Same client service is compiled into Unity and the exercised .NET headless client.
 public sealed class GameApi:IDisposable
 {
  readonly HttpClient http;readonly IWireJson json;readonly string fault;string token="";
  public bool Authenticated { get { return token.Length>0; } }
  public GameApi(ServerEntry server,IWireJson serializer)
  {
   ValidateServer(server);json=serializer;fault=server.fault;
   http=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false});
   http.BaseAddress=new Uri(server.url.TrimEnd('/')+"/");http.Timeout=TimeSpan.FromSeconds(3);
  }
  public static void ValidateServer(ServerEntry s)
  {
   Uri u;
   if(s==null || string.IsNullOrWhiteSpace(s.id) || !Uri.TryCreate(s.url,UriKind.Absolute,out u) || u.Scheme!="http" || u.Host!="127.0.0.1" || u.Port!=5077 || u.AbsolutePath!="/" || u.UserInfo!="" || u.Query!="" || u.Fragment!="")throw new GameApiException("Invalid local server configuration");
   if(s.fault!="none" && s.fault!="login-error" && s.fault!="timeout" && s.fault!="disconnect")throw new GameApiException("Invalid demo fault configuration");
  }
  public async Task Login(string username,string password,CancellationToken cancel)
  {
   token="";
   if(string.IsNullOrWhiteSpace(username) || username.Length>40 || password==null || password.Length<1 || password.Length>128)throw new GameApiException("Enter a valid test username and password");
   var result=await Send<LoginReply>("login",new LoginRequest{username=username,password=password},cancel);
   if(result==null || result.token==null || result.token.Length!=64)throw new GameApiException("Invalid server response");token=result.token;
  }
  public Task<ServerList> Servers(CancellationToken c){return Send<ServerList>("servers",null,c);}
  public Task<CharacterList> Characters(string serverId,CancellationToken c){return Send<CharacterList>("characters?serverId="+Uri.EscapeDataString(serverId),null,c);}
  public Task<WorldReply> Position(string id,float x,float y,CancellationToken c){return Send<WorldReply>("position",new PositionRequest{characterId=id,x=x,y=y},c);}
  public async Task Logout(CancellationToken c){try{await Send<StatusReply>("logout",null,c,true);}finally{token="";}}
  async Task<T> Send<T>(string path,object body,CancellationToken cancel,bool post=false)
  {
   try {
    using(var request=new HttpRequestMessage(body==null&&!post?HttpMethod.Get:HttpMethod.Post,path)){
     if(token.Length>0)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
     if(fault!="none")request.Headers.Add("X-Demo-Fault",fault);
     if(body!=null){
      string payload;
      if(body is LoginRequest)payload=json.Encode((LoginRequest)body);else payload=json.Encode((PositionRequest)body);
      request.Content=new StringContent(payload,Encoding.UTF8,"application/json");
     }
     using(var response=await http.SendAsync(request,HttpCompletionOption.ResponseContentRead,cancel)){
      if(response.StatusCode==HttpStatusCode.Unauthorized){token="";throw new GameApiException("Login rejected or session expired");}
      if(!response.IsSuccessStatusCode)throw new GameApiException("Server rejected request ("+(int)response.StatusCode+")");
      var text=await response.Content.ReadAsStringAsync();if(text.Length>65536)throw new GameApiException("Server response too large");
      try{return json.Decode<T>(text);}catch(Exception){throw new GameApiException("Invalid server response");}
     }
    }
   }catch(OperationCanceledException){token="";throw new GameApiException(cancel.IsCancellationRequested?"Request cancelled":"Local server timeout");}
   catch(HttpRequestException){token="";throw new GameApiException("Local server disconnected or unavailable");}
  }
  public void Dispose(){token="";http.Dispose();}
 }
}
