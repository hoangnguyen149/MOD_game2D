# Loopback demo protocol v1

Contract source compiled by both Unity and .NET tests: `game-client/Assets/Scripts/Network/Contracts.cs`. The `shared/Core` project links that exact source along with `GameApi.cs`, `ClientConfiguration.cs` and game core; no manually copied DLL or second DTO model is used in Unity. Unity uses JsonUtility, .NET uses System.Text.Json (IncludeFields=true). DTO fields use lower camel case. Unity's actual JSON/runtime integration is still unverified.

Base URL: `http://127.0.0.1:5077/` only. Client validates host, scheme, port, root path, no userinfo/query/fragment; redirects disabled. Proxy disabled **only for this loopback client**, never for external downloads/authentication. HTTP here carries public synthetic demo credentials on the local machine; it is not a production login design. Do not expose the port, insert real credentials, or substitute a real game endpoint.

| Request | Input | Success | Errors |
|---|---|---|---|
| GET /health | none | status, mode, demoFaults | service unavailable |
| GET /servers | none | servers: [{id,name,url,fault}] | service unavailable |
| POST /login | username, password | token, expiresIn | 400 invalid JSON; 401 invalid credentials; 429 session cap |
| GET /characters?serverId=meadow | Bearer token | characters: [{id,name,hp,mp,x,y}] | 401 absent/expired session; 400 unknown server |
| GET /world | Bearer token | map, character | 401 |
| POST /position | Bearer token; characterId,x,y | map, character | 400 nonfinite/out-of-bounds/wrong-owner input; 401 |
| POST /logout | Bearer token | status | idempotent; token removed if present |

Token is a random 32-byte value encoded as 64 hex characters, expires after 30 minutes, stored server-side in RAM. Client stores it only in memory. Server validates body max 8 KiB and coordinates x in [-9,9], y in [-5,5]. It maintains independent per-session positions (relogin resets the demo position). There is no persistent world, anti-cheat, multiplayer replication or authority over local ability/debug HP/MP. Character HP/MP in mock replies are static sample data. Position update is the minimal server-managed state.

Synthetic accounts: `demo`, `demo2`; the public demo password is documented in the run guide. Server stores a PBKDF2-SHA256 verifier (100,000 iterations, fixed **demo-only** salt), not a plaintext password. Production account storage is out of scope. Fixed public fixture salt/credential must not be reused in production.

Start server with `--DemoFaults=true` to allow request header `X-Demo-Fault`:

- `login-error`: login returns 401.
- `timeout`: login waits six seconds; shared client timeout is three seconds.
- `disconnect`: server aborts the position request connection after successful login.

Without the explicit flag, those headers have no effect. Unity config supplies matching named demo choices. No endpoint accesses the reference game's server.

Launcher starts a selected client using ProcessStartInfo.ArgumentList with window dimensions, a profile GUID and server ID. Only the non-secret demo username hint goes through `OWNED_TEST_USER`. No password/token goes through args, environment, config or launcher logs in the production launcher. Client owns password entry and login, so session IPC is unnecessary. Test-only ClientProbe has a public synthetic credential fixture; it is not the Unity executable and is not packaged as a game.
