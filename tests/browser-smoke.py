"""Real browser checks for the independent Canvas client, against our own local mock.
Requires Playwright; current cloud uses system Chromium via CHROMIUM_PATH.
"""
import argparse,os,sys,pathlib,time
from playwright.sync_api import sync_playwright
parser=argparse.ArgumentParser();parser.add_argument('--screenshot',type=pathlib.Path);args=parser.parse_args()
count=0
def check(ok,name):
 global count
 assert ok,name
 count+=1;print('PASS',name,flush=True)
with sync_playwright() as p:
 options=dict(headless=True)
 if os.environ.get('CHROMIUM_PATH'):options['executable_path']=os.environ['CHROMIUM_PATH']
 if sys.platform=='linux':options['args']=['--no-sandbox']
 browser=p.chromium.launch(**options)
 context=browser.new_context(viewport={'width':1440,'height':1000})
 page=context.new_page();errors=[];console=[];positions=[];issued=[]
 page.on('pageerror',lambda error:errors.append(str(error)))
 page.on('console',lambda msg:console.append(msg.text))
 def response_seen(response):
  if response.url.endswith('/login') and response.status==200:
   try:issued.append(response.json()['token'])
   except Exception:pass
  if response.url.endswith('/position') and response.status==200:
   try:positions.append(response.json()['character'])
   except Exception:pass
 page.on('response',response_seen)
 # No CDN, remote API or other outside origin is needed by this game.
 page.route('**/*',lambda route:route.continue_() if route.request.url.startswith('http://127.0.0.1:5077/') else route.abort())
 response=page.goto('http://127.0.0.1:5077/',wait_until='networkidle')
 check(response.status==200 and 'text/html' in response.headers['content-type'],'Docker serves browser client at root URL')
 page.wait_for_function("!document.querySelector('#login-button').disabled")
 check(page.locator('#server-select option').count()==1,'server choices loaded from mock API')
 check(page.locator('#password').input_value()=='','password is not prefilled')
 check(page.locator('#fault-controls').is_hidden(),'demo faults hidden in normal server mode')
 canvas=page.locator('#game-canvas')
 check(canvas.bounding_box()['width']>400,'canvas is rendered at desktop size')
 page.locator('#password').fill('wrong');page.locator('#login-button').click()
 page.wait_for_function("document.querySelector('#connection-status').dataset.state==='error'")
 check(page.locator('#login-panel').is_visible(),'incorrect login stays on login screen')
 check('không đúng' in page.locator('#login-message').inner_text(),'incorrect login has a useful error')
 page.locator('#password').fill('local-demo-only');page.locator('#login-button').click()
 page.wait_for_function("document.querySelector('#game-canvas').dataset.connected==='true'")
 check(page.locator('#character-name').inner_text()=='Meadow Hero','browser login loads character name')
 check(page.locator('#hp-value').inner_text()=='100' and page.locator('#mp-value').inner_text()=='50','initial HP and MP displayed')
 check(page.locator('#password').input_value()=='','password field cleared after login')
 check(page.evaluate('localStorage.length===0 && sessionStorage.length===0'),'credentials and sessions not written to browser storage')
 check(context.cookies()==[],'mock session does not use cookies')
 canvas.focus();x=float(canvas.get_attribute('data-player-x'))
 page.keyboard.down('d');page.wait_for_timeout(600);page.keyboard.up('d')
 check(float(canvas.get_attribute('data-player-x'))>x+.3,'WASD moves the rendered character')
 y=float(canvas.get_attribute('data-player-y'));page.keyboard.down('ArrowUp');page.wait_for_timeout(400);page.keyboard.up('ArrowUp')
 check(float(canvas.get_attribute('data-player-y'))>y+.3,'arrow keys move character')
 page.wait_for_timeout(500)
 check(any(item['x']>.3 and item['y']>.3 for item in positions),'movement is sent to server through position API')
 before=int(page.locator('#mp-value').inner_text());page.keyboard.press('Space');page.wait_for_timeout(150)
 check(int(page.locator('#mp-value').inner_text())==before-5,'skill consumes exactly five MP')
 page.keyboard.press('Escape');page.wait_for_function("document.querySelector('#game-canvas').dataset.paused==='true'")
 check(page.locator('#pause-overlay').is_visible(),'Escape shows pause overlay')
 x=float(canvas.get_attribute('data-player-x'));page.keyboard.down('d');page.wait_for_timeout(400);page.keyboard.up('d')
 check(abs(float(canvas.get_attribute('data-player-x'))-x)<.01,'paused movement is stopped')
 page.locator('#resume-overlay').click();page.wait_for_function("document.querySelector('#game-canvas').dataset.paused==='false'")
 check(page.locator('#pause-overlay').is_hidden(),'resume returns to play')
 page.locator('#show-fps').uncheck();check(page.locator('#fps-display').is_hidden(),'FPS can be hidden')
 page.locator('#show-fps').check();check(page.locator('#fps-display').is_visible(),'FPS can be enabled')
 page.locator('#theme-select').select_option('sunset');check(page.locator('body').get_attribute('data-theme')=='sunset','HUD theme can change')
 page.locator('#theme-select').select_option('ocean')
 page.locator('#volume').focus();page.keyboard.press('Home');check(page.locator('#volume-value').inner_text()=='0%','volume control accepts mute')
 # Focus back on the game after editing settings.
 canvas.focus();page.keyboard.down('d');page.wait_for_timeout(2800);page.keyboard.up('d')
 check(float(canvas.get_attribute('data-player-x'))==9,'character stays within map boundary')
 if args.screenshot:
  args.screenshot.parent.mkdir(parents=True,exist_ok=True);page.screenshot(path=str(args.screenshot),full_page=True)
 page.set_viewport_size({'width':640,'height':900});page.wait_for_timeout(150)
 check(page.evaluate('document.documentElement.scrollWidth <= innerWidth + 1'),'narrow viewport has no horizontal overflow')
 page.set_viewport_size({'width':1440,'height':1000})
 # Actual browser fetch is blocked to exercise the same path as a lost connection.
 page.route('**/position',lambda route:route.abort())
 page.wait_for_function("document.querySelector('#game-canvas').dataset.connected==='false'",timeout=6000)
 check(page.locator('#login-panel').is_visible(),'connection failure returns to login without freezing')
 check('Mất kết nối' in page.locator('#login-message').inner_text(),'connection failure shows recovery message')
 page.unroute('**/position');page.locator('#password').fill('local-demo-only');page.locator('#login-button').click()
 page.wait_for_function("document.querySelector('#game-canvas').dataset.connected==='true'")
 check(page.locator('#session-panel').is_visible(),'login works again after connection failure')
 page.locator('#logout-button').click();page.wait_for_function("document.querySelector('#game-canvas').dataset.connected==='false'")
 check(page.locator('#login-panel').is_visible(),'logout returns to login')
 # Token must have been revoked by the async /logout request.
 page.wait_for_timeout(300)
 check(len(issued)>=2,'browser used actual mock login sessions')
 check(context.request.get('http://127.0.0.1:5077/world',headers={'Authorization':'Bearer '+issued[-1]}).status==401,'logout revokes server session')
 check(not errors,'no unhandled browser JavaScript errors')
 check(all('local-demo-only' not in text and all(token not in text for token in issued) for text in console),'browser console omits passwords and issued tokens')
 browser.close()
print(f'{count} browser checks passed',flush=True)
