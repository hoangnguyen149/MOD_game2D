using System;
namespace OwnedGame.Net
{
 [Serializable] public sealed class LoginRequest { public string username=""; public string password=""; }
 [Serializable] public sealed class LoginReply { public string token=""; public int expiresIn; }
 [Serializable] public sealed class ServerEntry { public string id=""; public string name=""; public string url=""; public string fault="none"; }
 [Serializable] public sealed class ServerList { public ServerEntry[] servers=new ServerEntry[0]; }
 [Serializable] public sealed class CharacterInfo { public string id=""; public string name=""; public int hp; public int mp; public float x; public float y; }
 [Serializable] public sealed class CharacterList { public CharacterInfo[] characters=new CharacterInfo[0]; }
 [Serializable] public sealed class PositionRequest { public string characterId="";public float x;public float y; }
 [Serializable] public sealed class WorldReply { public string map="";public CharacterInfo character=new CharacterInfo(); }
 [Serializable] public sealed class StatusReply { public string status=""; }
 public interface IWireJson { string Encode<T>(T value);T Decode<T>(string value); }
}
