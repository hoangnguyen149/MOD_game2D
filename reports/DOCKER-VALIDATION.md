# Docker verification

Executed on Linux x64, Docker Engine 29.9.2, Compose v2.40.3. No Unity or reference-game EXE/DLL was executed. Windows Docker Desktop/GUI has not been exercised here.

| Check | Result |
|---|---|
| Compose config validation | PASS |
| Server multi-stage build (SDK 10.0.403, ASP.NET runtime 10.0) | PASS |
| Startup with healthcheck + `up --wait` | PASS |
| Probe runtime image and ClientProbe login/server/character/position/logout | PASS, process exit 0 |
| Published port API, auth/ownership/logout, app user, read-only FS, loopback publish, logs | 14/14 PASS |
| Opt-in demo login rejection, timeout and aborted connection, server stays healthy, logs | 8/8 PASS |
| Native build after opt-in change | PASS, 0 warnings/errors |
| Native API/configuration checks | 24/24 PASS |
| Native launcher/profile/process checks | 23/23 PASS |
| Native HTTP/log checks | 15/15 PASS |
| Cleanup | Own test containers and network removed with Compose down |

Container checks total 22; the native regression run totals 62. Core/debug tests were not rerun for this focused network/deployment change. Earlier v2 results remain in VALIDATION-V2.md.

Initial Docker fault harness run hit a variable-shadowing bug in the Python exception handler after the server correctly aborted a connection. Renamed the HTTP opener variable and reran the affected fault checks: 8/8 pass. No product assertions were disabled.

Default server binding/IP check remains loopback-only outside Compose. Explicit ContainerDemo permits bridge traffic inside the container; Compose publishes only host IPv4 loopback. This is a local demo deployment, not production security. Shared client destination validation was not relaxed.

SDK build uses only framework packs shipped in the SDK image; docker/NuGet.Config clears external package sources for this current dependency-free build. The publish RUN uses `--network=none`, SDK telemetry is opted out, and no TLS verification was disabled. Base images were pulled through the managed daemon's existing proxy/trust configuration. If PackageReference dependencies are later added, revisit restore/trust configuration instead of bypassing TLS.

Evidence: docker-build.log, docker-start.log, docker-probe.log, docker-host-checks.log, docker-demo-start.log, docker-fault-checks.log, docker-native-build.log, docker-native-regression.log, docker-stop.log. These outputs are command evidence, not Unity gameplay verification. Image cache remains local; no image registry publication was performed.
