# Browser client verification

Scope: independent HTML/CSS/JS Canvas prototype served by the existing local Docker mock server. Not a Unity/WebGL export or recovered reference game. Procedural graphics and audio are authored here; no reference-game assets/binaries are used.

| Operation | Evidence |
|---|---|
| Server build | PASS, 0 warnings/errors |
| Docker image with wwwroot + app-owned files | PASS |
| Health/startup | PASS |
| Chromium 155.0.8059.79 browser | 33/33 checks PASS |
| Docker API/port/non-root/read-only/credentials-in-logs checks after update | 14/14 PASS |
| Screenshot | docs/media/web-play.png |
| Cleanup | Own Docker test service/network removed |
| Actual Windows/Cốc Cốc/Edge, Unity GUI | Not run in this environment |

Browser checks exercise the real DOM, keyboard, Canvas state and HTTP API: root page loads, mock server list, incorrect/correct login, named character/HP/MP, password cleared, no localStorage/sessionStorage/cookies for session, WASD/arrows, server position update, skill MP cost, pause/resume, FPS/theme/volume, map bounds, narrow viewport, network failure/relogin, logout server revocation, no unhandled JS errors and no credential/token console output.

Browser connection failure is injected by blocking /position in Playwright; it verifies the browser error/recovery path but is not an actual Docker stop in that test. Separate Docker/API failure checks remain in DOCKER-VALIDATION.md. No claims that Unity integration or Windows GUI passed are derived from these checks. HP/MP, skills and collectibles in this browser prototype are local simulation; mock server stores only session/position state.

An initial browser request found HTTP 500 because source wwwroot files had mode 600 while the runtime ran as app. Fixed COPY ownership with --chown=app:app for published files, retained non-root/read-only, rebuilt and reran browser tests successfully. Health API alone did not establish page readiness; browser root/assets and actual play were checked afterward. No assertions were disabled.

Tools for reproduction: tests/requirements-browser.txt and tests/browser-smoke.py. Cloud used system Chromium with the container-compatible sandbox launch option, accessed only the local mock origin; no original EXE/DLL or remote game server was executed/accessed. Browser and Docker test processes were closed. Evidence: web-server-build.log, web-docker-build.log, web-docker-start.log, browser-smoke.log, web-docker-api.log, web-docker-stop.log.
