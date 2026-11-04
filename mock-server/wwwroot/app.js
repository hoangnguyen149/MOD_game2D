'use strict';
(() => {
  const byId = (id) => document.getElementById(id);
  const canvas = byId('game-canvas');
  const ctx = canvas.getContext('2d');
  const loginForm = byId('login-form');
  const loginButton = byId('login-button');
  const serverSelect = byId('server-select');
  const pressed = new Set();
  const controllers = new Set();
  const state = { generation: 0, token: '', connected: false, pending: false, paused: false, fault: 'none', id: '', name: '', x: 0, y: 0, hp: 100, mp: 50, score: 0, facing: 1, walk: 0, pulse: -10 };
  const palette = { ocean: { accent: '#d4ef94', spark: '#d9f8ad', ground: '#34553a', ground2: '#315137' }, sunset: { accent: '#ffd19d', spark: '#ffe6a0', ground: '#5d503e', ground2: '#574a39' } };
  let theme = 'ocean';
  let showFps = true;
  let volume = .35;
  let audio = null;
  let width = 900;
  let height = 560;
  let pixels = 40;
  let last = performance.now();
  let nowSeconds = 0;
  let fps = 60;
  let hudTime = 0;
  let nextSync = 0;
  let syncTask = null;
  let ready = false;
  let motes = [];
  let notice = '';
  let noticeUntil = 0;
  const sparkLocations = [[-6, 3], [-4, -2], [0, 3.5], [3.4, -3], [6.7, 2], [7.5, -3.5], [-7, -3.5], [1.8, 1.7]];

  class FriendlyError extends Error {}
  function message(text, error = false) {
    byId('login-message').textContent = text;
    byId('login-message').classList.toggle('error', error);
  }
  function connection(text, kind) {
    byId('connection-status').textContent = text;
    byId('connection-status').dataset.state = kind;
    byId('connection-dot').className = `status-dot ${kind}`;
  }
  function cancelRequests() {
    for (const controller of controllers) controller.abort();
    controllers.clear();
    syncTask = null;
    pressed.clear();
  }
  async function request(path, options = {}) {
    const controller = new AbortController();
    controllers.add(controller);
    const timer = setTimeout(() => controller.abort(), 3000);
    const headers = { Accept: 'application/json' };
    if (options.token) headers.Authorization = `Bearer ${options.token}`;
    if (options.fault && options.fault !== 'none') headers['X-Demo-Fault'] = options.fault;
    if (options.body !== undefined) headers['Content-Type'] = 'application/json';
    try {
      const response = await fetch(path, { method: options.method || (options.body !== undefined ? 'POST' : 'GET'), headers, body: options.body !== undefined ? JSON.stringify(options.body) : undefined, signal: controller.signal, cache: 'no-store', credentials: 'omit', redirect: 'error', mode: 'same-origin' });
      if (response.status === 401) throw new FriendlyError('Tài khoản/mật khẩu không đúng hoặc phiên đã hết hạn.');
      if (response.status === 429) throw new FriendlyError('Mock server đã có quá nhiều phiên. Hãy khởi động lại server demo.');
      if (!response.ok) throw new FriendlyError(`Mock server từ chối yêu cầu (${response.status}).`);
      let value;
      try { value = await response.json(); } catch { throw new FriendlyError('Phản hồi từ mock server không hợp lệ.'); }
      return value;
    } catch (error) {
      if (error instanceof FriendlyError) throw error;
      if (error && error.name === 'AbortError') throw new FriendlyError('Mock server không phản hồi trong 3 giây. Kiểm tra Docker rồi thử lại.');
      throw new FriendlyError('Mất kết nối mock server. Kiểm tra container trong Docker rồi đăng nhập lại.');
    } finally { clearTimeout(timer); controllers.delete(controller); }
  }
  function setBusy(value) {
    state.pending = value;
    loginButton.disabled = value || !ready;
    byId('refresh-button').disabled = value;
    serverSelect.disabled = value || !ready;
    byId('username').disabled = value;
    byId('password').disabled = value;
    byId('fault-select').disabled = value;
    loginButton.textContent = value ? 'Đang kết nối…' : 'Vào bản đồ ↗';
  }
  async function loadServers() {
    if (state.pending || state.connected) return;
    ready = false;
    setBusy(true);
    message('Đang kiểm tra mock server…');
    try {
      const [list, health] = await Promise.all([request('/servers'), request('/health')]);
      if (!list || !Array.isArray(list.servers) || !list.servers.length || health.status !== 'ok') throw new FriendlyError('Danh sách máy chủ không hợp lệ.');
      serverSelect.replaceChildren();
      for (const server of list.servers.slice(0, 12)) {
        if (!server || typeof server.id !== 'string' || !/^[A-Za-z0-9_-]{1,40}$/.test(server.id)) continue;
        const option = document.createElement('option'); option.value = server.id; option.textContent = typeof server.name === 'string' ? server.name : server.id; serverSelect.add(option);
      }
      if (!serverSelect.options.length) throw new FriendlyError('Không có máy chủ demo hợp lệ.');
      ready = true;
      byId('fault-controls').hidden = !health.demoFaults;
      message('Mock server sẵn sàng. Nhập mật khẩu demo để chơi.');
    } catch (error) {
      serverSelect.replaceChildren(new Option('Chưa kết nối được máy chủ', ''));
      message(error.message, true);
    } finally { setBusy(false); }
  }
  function resetCollectibles() { motes = sparkLocations.map(([x, y], index) => ({ x, y, index, availableAt: 0 })); }
  function renderHud() {
    byId('hp-meter').value = state.hp; byId('hp-value').textContent = String(Math.round(state.hp));
    byId('mp-meter').value = state.mp; byId('mp-value').textContent = String(Math.floor(state.mp));
    byId('collect-count').textContent = String(state.score); byId('session-score').textContent = String(state.score);
    byId('coordinates').textContent = `X ${state.x.toFixed(1)} · Y ${state.y.toFixed(1)}`;
    byId('fps-display').textContent = `FPS ${Math.round(fps)}`;
  }
  function updatePanels() {
    byId('login-panel').hidden = state.connected;
    byId('session-panel').hidden = !state.connected;
    byId('canvas-hud').hidden = !state.connected;
    byId('collect-badge').hidden = !state.connected;
    byId('touch-controls').hidden = !state.connected || state.paused;
    byId('fps-display').hidden = !showFps;
    byId('world-message').hidden = state.connected;
    byId('pause-overlay').hidden = !state.connected || !state.paused;
    byId('pause-button').textContent = state.paused ? '▶ Tiếp tục' : 'Ⅱ Tạm dừng';
  }
  function disconnect(text) {
    state.generation++;
    state.token = '';
    state.connected = false;
    state.paused = false;
    cancelRequests();
    setBusy(false);
    connection('Chưa kết nối', 'error');
    message(text, true);
    updatePanels();
  }
  loginForm.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (state.pending || !ready) return;
    const username = byId('username').value.trim();
    let password = byId('password').value;
    byId('password').value = '';
    if (!username || !password) { message('Nhập tài khoản và mật khẩu demo.', true); return; }
    state.generation++;
    const generation = state.generation;
    cancelRequests();
    state.token = '';
    state.fault = byId('fault-select').value;
    const fault = state.fault;
    setBusy(true);
    connection('Đang đăng nhập…', 'connecting');
    message('Đang đăng nhập mock server…');
    try {
      const session = await request('/login', { body: { username, password }, fault });
      password = '';
      if (generation !== state.generation) return;
      if (!session || typeof session.token !== 'string' || !/^[A-Fa-f0-9]{64}$/.test(session.token)) throw new FriendlyError('Phiên đăng nhập không hợp lệ.');
      state.token = session.token;
      const result = await request(`/characters?serverId=${encodeURIComponent(serverSelect.value)}`, { token: state.token, fault });
      if (generation !== state.generation) return;
      const character = result && Array.isArray(result.characters) && result.characters[0];
      if (!character || typeof character.id !== 'string' || typeof character.name !== 'string' || !Number.isFinite(character.x) || !Number.isFinite(character.y) || Math.abs(character.x) > 9 || Math.abs(character.y) > 5 || !Number.isFinite(character.hp) || !Number.isFinite(character.mp) || character.hp < 0 || character.hp > 100 || character.mp < 0 || character.mp > 50) throw new FriendlyError('Dữ liệu nhân vật không hợp lệ.');
      Object.assign(state, { id: character.id, name: character.name, x: character.x, y: character.y, hp: character.hp, mp: character.mp, score: 0, connected: true, paused: false });
      byId('character-name').textContent = character.name;
      byId('welcome-name').textContent = character.name;
      resetCollectibles();
      nextSync = 0;
      connection('Đã kết nối · Local Meadow', 'connected');
      updatePanels(); renderHud(); canvas.focus();
      notice = 'WASD để di chuyển · Space để dùng kỹ năng'; noticeUntil = nowSeconds + 5;
      unlockAudio();
    } catch (error) {
      if (generation === state.generation) disconnect(error.message);
    } finally {
      password = '';
      if (generation === state.generation) setBusy(false);
    }
  });
  async function syncPosition() {
    if (!state.connected || syncTask) return;
    const task = { generation: state.generation };
    syncTask = task;
    const token = state.token;
    try {
      const result = await request('/position', { body: { characterId: state.id, x: state.x, y: state.y }, token, fault: state.fault });
      if (task.generation !== state.generation) return;
      if (!result || !result.character || result.character.id !== state.id) throw new FriendlyError('Phản hồi cập nhật vị trí không hợp lệ.');
    } catch (error) {
      if (task.generation === state.generation) disconnect(error.message);
    } finally { if (syncTask === task) syncTask = null; }
  }
  byId('logout-button').addEventListener('click', () => {
    const token = state.token;
    disconnect('Đã đăng xuất. Bạn có thể đăng nhập lại để bắt đầu phiên mới.');
    connection('Đã đăng xuất', 'offline');
    // Revocation is best-effort; no token is retained in client state or browser storage.
    if (token) request('/logout', { token, method: 'POST' }).catch(() => {});
  });
  function setPause(value) { if (!state.connected) return; state.paused = value; pressed.clear(); updatePanels(); if (!value) canvas.focus(); }
  byId('pause-button').addEventListener('click', () => setPause(!state.paused));
  byId('resume-overlay').addEventListener('click', () => setPause(false));
  byId('refresh-button').addEventListener('click', loadServers);
  byId('show-fps').addEventListener('change', (event) => { showFps = event.target.checked; updatePanels(); });
  byId('theme-select').addEventListener('change', (event) => { theme = event.target.value === 'sunset' ? 'sunset' : 'ocean'; document.body.dataset.theme = theme; });
  byId('volume').addEventListener('input', (event) => { volume = Number(event.target.value) / 100; byId('volume-value').value = `${Math.round(volume * 100)}%`; });
  byId('fullscreen-button').addEventListener('click', async () => { try { if (document.fullscreenElement) await document.exitFullscreen(); else await byId('canvas-wrap').requestFullscreen(); } catch { /* Fullscreen may be unavailable in embedded browsers; normal canvas remains usable. */ } });
  const movement = new Set(['KeyW', 'KeyA', 'KeyS', 'KeyD', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight']);
  const editing = (target) => target instanceof Element && !!target.closest('input,select,textarea,[contenteditable="true"]');
  window.addEventListener('keydown', (event) => {
    if (!state.connected || editing(event.target)) return;
    if (event.code === 'Escape') { event.preventDefault(); if (!event.repeat) setPause(!state.paused); return; }
    if (movement.has(event.code) || event.code === 'Space') event.preventDefault();
    if (state.paused) return;
    if (event.code === 'Space' && !event.repeat) cast();
    if (movement.has(event.code)) pressed.add(event.code);
  });
  window.addEventListener('keyup', (event) => pressed.delete(event.code));
  window.addEventListener('blur', () => { pressed.clear(); if (state.connected) setPause(true); });
  document.addEventListener('visibilitychange', () => { if (document.hidden) { pressed.clear(); if (state.connected) setPause(true); } });
  canvas.addEventListener('pointerdown', () => { canvas.focus(); unlockAudio(); });
  for (const button of document.querySelectorAll('[data-key]')) {
    button.addEventListener('pointerdown', (event) => { event.preventDefault(); if (!state.connected || state.paused) return; button.setPointerCapture(event.pointerId); if (button.dataset.key === 'Space') cast(); else pressed.add(button.dataset.key); });
    for (const eventName of ['pointerup', 'pointercancel', 'lostpointercapture']) button.addEventListener(eventName, () => pressed.delete(button.dataset.key));
  }
  function unlockAudio() { try { if (!audio) audio = new (window.AudioContext || window.webkitAudioContext)(); if (audio.state === 'suspended') audio.resume().catch(() => {}); } catch { audio = null; } }
  function tone() {
    if (!audio || volume === 0) return;
    try { const oscillator = audio.createOscillator(); const gain = audio.createGain(); oscillator.type = 'sine'; oscillator.frequency.setValueAtTime(660, audio.currentTime); oscillator.frequency.exponentialRampToValueAtTime(330, audio.currentTime + .18); gain.gain.setValueAtTime(.12 * volume, audio.currentTime); gain.gain.exponentialRampToValueAtTime(.001, audio.currentTime + .2); oscillator.connect(gain); gain.connect(audio.destination); oscillator.start(); oscillator.stop(audio.currentTime + .21); } catch { /* Audio is optional. */ }
  }
  function cast() {
    if (!state.connected || state.paused) return;
    unlockAudio();
    if (state.mp < 5) { notice = 'Chưa đủ MP. Nhặt đốm sáng để hồi MP.'; noticeUntil = nowSeconds + 2.5; return; }
    if (nowSeconds - state.pulse < .25) return;
    state.mp -= 5; state.pulse = nowSeconds; tone(); renderHud();
  }
  function resize() {
    const bounds = canvas.getBoundingClientRect(); width = Math.max(1, bounds.width); height = Math.max(1, bounds.height); const dpr = Math.min(window.devicePixelRatio || 1, 2); canvas.width = Math.round(width * dpr); canvas.height = Math.round(height * dpr); ctx.setTransform(dpr, 0, 0, dpr, 0, 0); pixels = Math.min(width / 20.5, height / 12.5);
  }
  new ResizeObserver(resize).observe(canvas);
  const X = (x) => width / 2 + x * pixels;
  const Y = (y) => height / 2 - y * pixels;
  function ellipse(x, y, rx, ry, color) { ctx.fillStyle = color; ctx.beginPath(); ctx.ellipse(x, y, rx, ry, 0, 0, Math.PI * 2); ctx.fill(); }
  function tree(x, y, size = 1) {
    const px = X(x), py = Y(y); ellipse(px + pixels * .12, py + pixels * .12, pixels * .45 * size, pixels * .2 * size, '#173b2e55');
    ctx.fillStyle = '#6e6040'; ctx.fillRect(px - pixels * .07, py - pixels * .7 * size, pixels * .14, pixels * .75 * size);
    ellipse(px, py - pixels * .95 * size, pixels * .55 * size, pixels * .61 * size, '#244a35'); ellipse(px - pixels * .08, py - pixels * 1.14 * size, pixels * .43 * size, pixels * .44 * size, '#3f6943'); ellipse(px - pixels * .18, py - pixels * 1.23 * size, pixels * .21 * size, pixels * .24 * size, '#5b8050');
  }
  function drawWorld(t) {
    const p = palette[theme];
    ctx.fillStyle = '#203e30'; ctx.fillRect(0, 0, width, height);
    for (let y = -6; y <= 6; y++) for (let x = -10; x <= 10; x++) { ctx.fillStyle = (x + y) % 2 === 0 ? p.ground : p.ground2; ctx.fillRect(X(x - .5), Y(y + .5), pixels + .5, pixels + .5); }
    ctx.strokeStyle = '#7b8a5770'; ctx.lineWidth = pixels * 1.1; ctx.lineCap = 'round'; ctx.beginPath(); ctx.moveTo(X(-10), Y(-2.4)); ctx.bezierCurveTo(X(-3), Y(-1.6), X(-2), Y(.5), X(2), Y(-.2)); ctx.bezierCurveTo(X(5), Y(-.7), X(6), Y(2), X(10), Y(2.2)); ctx.stroke();
    ctx.strokeStyle = '#96a06430'; ctx.lineWidth = pixels * .65; ctx.stroke();
    for (let i = 0; i < 90; i++) { const x = ((i * 43 + 17) % 190) / 10 - 9.5; const y = ((i * 29 + 8) % 110) / 10 - 5.5; const sway = Math.sin(t * 1.5 + i) * pixels * .025; ctx.strokeStyle = i % 3 ? '#86a96b40' : '#a4c57c66'; ctx.lineWidth = 1; ctx.beginPath(); ctx.moveTo(X(x), Y(y)); ctx.lineTo(X(x) - pixels * .035 + sway, Y(y) - pixels * .12); ctx.moveTo(X(x), Y(y)); ctx.lineTo(X(x) + pixels * .07 + sway, Y(y) - pixels * .08); ctx.stroke(); }
    ctx.strokeStyle = '#b5d18633'; ctx.lineWidth = 1; ctx.setLineDash([4, 8]); ctx.strokeRect(X(-9), Y(5), pixels * 18, pixels * 10); ctx.setLineDash([]);
    for (const [x, y, size] of [[-9.3, 4.5, 1.2], [-7.8, 5.2, 1], [-4, 5.8, .9], [8.8, 4.7, 1.2], [9.7, 2.5, .9], [-9.5, -1, 1.1], [9.6, -4.5, 1.2], [-7.8, -5.6, .8]]) tree(x, y, size);
    for (const [x, y] of [[-4.2, 2], [5.6, 3.7], [-2.8, -3.6]]) { ellipse(X(x), Y(y) + pixels * .07, pixels * .27, pixels * .13, '#203b2c66'); ellipse(X(x), Y(y), pixels * .23, pixels * .16, '#6e8061'); ellipse(X(x) - pixels * .05, Y(y) - pixels * .05, pixels * .13, pixels * .07, '#97a385'); }
    for (const mote of motes) {
      if (mote.availableAt > t) continue;
      const bob = Math.sin(t * 2 + mote.index) * pixels * .07; const px = X(mote.x), py = Y(mote.y) + bob;
      const glow = ctx.createRadialGradient(px, py, 0, px, py, pixels * .5); glow.addColorStop(0, '#edffbb4d'); glow.addColorStop(1, '#edffbb00'); ctx.fillStyle = glow; ctx.fillRect(px - pixels * .5, py - pixels * .5, pixels, pixels);
      ctx.fillStyle = p.spark; ctx.beginPath(); ctx.moveTo(px, py - pixels * .13); ctx.lineTo(px + pixels * .08, py); ctx.lineTo(px, py + pixels * .13); ctx.lineTo(px - pixels * .08, py); ctx.closePath(); ctx.fill();
    }
    for (let i = 0; i < 16; i++) { const x = Math.sin(t * .12 + i * 3.3) * 9; const y = Math.cos(t * .1 + i * 7) * 5; ellipse(X(x), Y(y), 1.1, 1.1, `rgba(217,244,158,${.2 + .2 * Math.sin(t + i)})`); }
  }
  function drawPlayer(t) {
    const px = X(state.x), py = Y(state.y); const size = pixels * .75; const bob = Math.sin(state.walk) * pixels * .035;
    ellipse(px, py + pixels * .23, pixels * .29, pixels * .11, '#112c227d');
    const pulseAge = t - state.pulse;
    if (pulseAge >= 0 && pulseAge < .65) { ctx.strokeStyle = `rgba(226,255,170,${1 - pulseAge / .65})`; ctx.lineWidth = Math.max(1, 4 * (1 - pulseAge)); ctx.beginPath(); ctx.ellipse(px, py, pixels * (.25 + pulseAge * 2.8), pixels * (.13 + pulseAge * 1.7), 0, 0, Math.PI * 2); ctx.stroke(); }
    ctx.fillStyle = '#182f2a'; ctx.fillRect(px - size * .26, py + size * .04 + bob, size * .19, size * .31); ctx.fillRect(px + size * .07, py + size * .04 + bob, size * .19, size * .31);
    ctx.fillStyle = '#b4d683'; ctx.beginPath(); ctx.moveTo(px - size * .3, py - size * .42 + bob); ctx.lineTo(px + size * .3, py - size * .42 + bob); ctx.lineTo(px + size * .4, py + size * .2 + bob); ctx.lineTo(px - size * .4, py + size * .2 + bob); ctx.closePath(); ctx.fill();
    ctx.fillStyle = '#749759'; ctx.fillRect(px - size * .12, py - size * .4 + bob, size * .22, size * .61);
    ellipse(px, py - size * .54 + bob, size * .23, size * .25, '#e4c99a');
    ctx.fillStyle = '#284c38'; ctx.fillRect(px - size * .28, py - size * .8 + bob, size * .55, size * .16); ctx.fillRect(px - size * .19, py - size * .96 + bob, size * .36, size * .18);
    ctx.fillStyle = '#d6ed9d'; ctx.fillRect(px - size * .08, py - size * .87 + bob, size * .09, size * .12);
    ctx.fillStyle = '#3e4b37'; ctx.fillRect(px + state.facing * size * .085 - size * .025, py - size * .59 + bob, size * .045, size * .06);
    ctx.fillStyle = '#e4d8ac'; ctx.fillRect(px + state.facing * size * .32, py - size * .14 + bob, size * .1, size * .19);
    if (state.connected) { ctx.font = `${Math.max(9, pixels * .24)}px "Segoe UI", sans-serif`; ctx.textAlign = 'center'; ctx.fillStyle = '#14251b'; ctx.fillText(state.name, px + 1, py - size * 1.17 + 1); ctx.fillStyle = '#e6f0d0'; ctx.fillText(state.name, px, py - size * 1.17); }
  }
  function frame(timestamp) {
    const dt = Math.max(0, Math.min(.1, (timestamp - last) / 1000)); last = timestamp; nowSeconds = timestamp / 1000;
    fps += ((dt > 0 ? 1 / dt : 60) - fps) * .06;
    if (state.connected && !state.paused) {
      let dx = (pressed.has('KeyD') || pressed.has('ArrowRight') ? 1 : 0) - (pressed.has('KeyA') || pressed.has('ArrowLeft') ? 1 : 0);
      let dy = (pressed.has('KeyW') || pressed.has('ArrowUp') ? 1 : 0) - (pressed.has('KeyS') || pressed.has('ArrowDown') ? 1 : 0);
      const length = Math.hypot(dx, dy); if (length > 1) { dx /= length; dy /= length; }
      state.x = Math.max(-9, Math.min(9, state.x + dx * 4 * dt)); state.y = Math.max(-5, Math.min(5, state.y + dy * 4 * dt));
      if (dx) state.facing = dx > 0 ? 1 : -1; if (length) state.walk += dt * 14;
      for (const mote of motes) if (mote.availableAt <= nowSeconds && Math.hypot(state.x - mote.x, state.y - mote.y) < .46) { mote.availableAt = nowSeconds + 12; state.score++; state.mp = Math.min(50, state.mp + 3); }
    }
    if (state.connected && timestamp >= nextSync && !syncTask) { nextSync = timestamp + 400; syncPosition(); }
    drawWorld(nowSeconds); drawPlayer(nowSeconds);
    if (state.connected && nowSeconds < noticeUntil && !state.paused) { ctx.font = `${Math.max(10, Math.min(13, width / 45))}px "Segoe UI",sans-serif`; ctx.textAlign = 'center'; ctx.fillStyle = '#12251edb'; const w = Math.min(width - 30, 370); ctx.fillRect(width / 2 - w / 2, height - 47, w, 27); ctx.fillStyle = palette[theme].accent; ctx.fillText(notice, width / 2, height - 29); }
    canvas.dataset.playerX = state.x.toFixed(3); canvas.dataset.playerY = state.y.toFixed(3); canvas.dataset.connected = String(state.connected); canvas.dataset.paused = String(state.paused);
    if (timestamp >= hudTime) { renderHud(); hudTime = timestamp + 100; }
    requestAnimationFrame(frame);
  }
  resetCollectibles(); resize(); updatePanels(); requestAnimationFrame(frame); loadServers();
})();
