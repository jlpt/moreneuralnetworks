// Headless playtest driver for Second Life.
// usage: node run.mjs <rom> <script.json> <screenshot dir> <syms.json>
import fs from 'fs';
import { createRequire } from 'module';
const require = createRequire(import.meta.url);
import { NES, Controller } from 'jsnes';
const { PNG } = require('pngjs');
const [,, romPath, scriptPath, outDir, symPath] = process.argv;
fs.mkdirSync(outDir, { recursive: true });
let fb = null;
const nes = new NES({ onFrame: f => { fb = f; }, onAudioSample: () => {} });
nes.loadROM(fs.readFileSync(romPath).toString('binary'));
const B = { A: Controller.BUTTON_A, B: Controller.BUTTON_B, SELECT: Controller.BUTTON_SELECT, START: Controller.BUTTON_START,
  UP: Controller.BUTTON_UP, DOWN: Controller.BUTTON_DOWN, LEFT: Controller.BUTTON_LEFT, RIGHT: Controller.BUTTON_RIGHT };
let held = new Set();
function setHeld(list) {
  for (const k of Object.keys(B)) nes.buttonUp(1, B[k]);
  held = new Set(list || []);
  for (const k of held) nes.buttonDown(1, B[k]);
}
function frames(n) { for (let i = 0; i < n; i++) nes.frame(); }
function shot(name) {
  const png = new PNG({ width: 256, height: 240 });
  for (let i = 0; i < 256 * 240; i++) {
    const c = fb[i];
    png.data[i * 4] = c & 0xFF; png.data[i * 4 + 1] = (c >> 8) & 0xFF; png.data[i * 4 + 2] = (c >> 16) & 0xFF; png.data[i * 4 + 3] = 255;
  }
  fs.writeFileSync(`${outDir}/${name}.png`, PNG.sync.write(png));
}
function ram(a) { return nes.cpu.mem[a]; }
const script = JSON.parse(fs.readFileSync(scriptPath));
const sym = JSON.parse(fs.readFileSync(symPath));
let failures = 0;
function dump(names) {
  const o = {};
  for (const n of names) o[n] = sym[n] !== undefined ? (n.startsWith('e_') ? Array.from({length:12},(_, i)=>ram(sym[n]+i)) : ram(sym[n])) : '?';
  return o;
}
for (const s of script) {
  if (s.frames) { setHeld(s.hold); frames(s.frames); }
  if (s.press) { setHeld([s.press]); frames(2); setHeld([]); frames(s.after || 4); }
  if (s.mash) { for (let i = 0; i < s.mash; i++) { setHeld([s.key || 'A']); frames(2); setHeld([]); frames(s.gap || 6); } }
  if (s.goto) {
    // walk toward target pixel position; axis order "xy" or "yx"
    const [tx, ty] = s.goto; const order = s.order || 'xy'; const max = s.max || 900;
    const startRoom = ram(sym.room);
    for (let i = 0; i < max; i++) {
      if (s.room !== undefined && ram(sym.room) === s.room) break;
      if (s.room === undefined && ram(sym.room) !== startRoom) break;
      const px = ram(sym.p_x), py = ram(sym.p_y);
      const dx = tx - px, dy = ty - py;
      const btn = [];
      const hx = dx > 1 ? 'RIGHT' : dx < -1 ? 'LEFT' : null;
      const hy = dy > 1 ? 'DOWN' : dy < -1 ? 'UP' : null;
      if (order === 'xy') { if (hx) btn.push(hx); else if (hy) btn.push(hy); }
      else { if (hy) btn.push(hy); else if (hx) btn.push(hx); }
      if (!btn.length && s.room === undefined) break;
      if (s.push) btn.push(s.push);
      if (ram(sym.dlg_state)) { setHeld([]); frames(1); setHeld(['A']); frames(1); setHeld([]); frames(3); continue; }
      setHeld(btn); frames(1);
    }
    setHeld([]); frames(2);
  }
  if (s.talk) {
    setHeld([]); frames(2);
    if (s.face) { setHeld([s.face]); frames(1); setHeld([]); frames(2); }
    setHeld(['A']); frames(2); setHeld([]); frames(4);
    for (let i = 0; i < 4000 && ram(sym.dlg_state); i++) { setHeld(['A']); frames(1); setHeld([]); frames(3); }
    frames(4);
  }
  if (s.skipdlg) { for (let i = 0; i < 4000 && ram(sym.dlg_state); i++) { setHeld(['A']); frames(1); setHeld([]); frames(3); } frames(4); }
  if (s.cast) { for (let i = 0; i < s.cast; i++) { if (s.face) { setHeld([s.face]); frames(1); } setHeld(['B']); frames(2); setHeld([]); frames(s.gap || 16); if (ram(sym.dlg_state)) { for (let j = 0; j < 400 && ram(sym.dlg_state); j++) { setHeld(['A']); frames(1); setHeld([]); frames(3); } } } }
  if (s.hunt) {
    let stuck = 0, lastp = -1, wander = 0, wdir = 'UP';
    const dirs = ['UP', 'DOWN', 'LEFT', 'RIGHT'];
    const hroom = ram(sym.room);
    for (let i = 0; i < (s.max || 3000); i++) {
      if (ram(sym.mode) !== 2 || ram(sym.room) !== hroom) break;
      if (ram(sym.dlg_state)) { setHeld([]); frames(1); setHeld(['A']); frames(1); setHeld([]); frames(3); continue; }
      const ids = [];
      for (let k = 0; k < 12; k++) if (s.hunt.includes(ram(sym.e_type + k))) ids.push(k);
      if (!ids.length) break;
      const px = ram(sym.p_x), py = ram(sym.p_y);
      let best = ids[0], bd = 1e9;
      for (const k of ids) { const d = Math.abs(ram(sym.e_x + k) - px) + Math.abs(ram(sym.e_y + k) - py); if (d < bd) { bd = d; best = k; } }
      const big = ram(sym.e_type + best) === 9 ? 8 : 0;
      const ex = ram(sym.e_x + best) + big, ey = ram(sym.e_y + best) + big;
      const dx = ex - px, dy = ey - py;
      let btn = [];
      if (px < 10) btn = ['RIGHT']; else if (px > 226) btn = ['LEFT']; else if (py < 34) btn = ['DOWN']; else if (py > 212) btn = ['UP'];
      else if (wander > 0) { wander--; btn = [wdir]; }
      else if (!s.vertical && (Math.abs(dy) <= Math.abs(dx) || s.horizontal)) {
        if (Math.abs(dy) > 5) btn = [dy > 0 ? 'DOWN' : 'UP'];
        else if (Math.abs(dx) < 28) btn = [dx > 0 ? 'LEFT' : 'RIGHT'];
        else { btn = [dx > 0 ? 'RIGHT' : 'LEFT']; if (ram(sym.p_cast) === 0 && (i & 1)) btn.push('B'); }
      } else {
        if (Math.abs(dx) > 5) btn = [dx > 0 ? 'RIGHT' : 'LEFT'];
        else if (Math.abs(dy) < (s.vertical ? 90 : 28)) btn = [dy > 0 ? 'UP' : 'DOWN'];
        else { btn = [dy > 0 ? 'DOWN' : 'UP']; if (ram(sym.p_cast) === 0 && (i & 1)) btn.push('B'); }
      }
      const pos = px * 256 + py;
      if (pos === lastp && !btn.includes('B')) { if (++stuck > 20) { wander = 20; wdir = dirs[(i >> 3) & 3]; stuck = 0; } } else stuck = 0;
      lastp = pos;
      setHeld(btn); frames(1);
      if (ram(sym.p_hp) === 0) break;
    }
    setHeld([]); frames(2);
  }
  if (s.walk || s.exit) {
    const SOLID = new Set([1, 3, 4, 5, 6, 7, 8, 13, 14, 16]);
    const blocked = new Set();
    for (let k = 0; k < 12; k++) { const t = ram(sym.e_type + k); if (t >= 1 && t <= 5) { const c = (ram(sym.e_x + k) + 8) >> 4, r = (ram(sym.e_y + k) + 8 - 32) >> 4; blocked.add(r * 16 + c); } }
    const free = (c, r) => c >= 0 && c < 16 && r >= 0 && r < 13 && !SOLID.has(ram(sym.map + r * 16 + c)) && !blocked.has(r * 16 + c);
    const pc = (ram(sym.p_x) + 8) >> 4, pr = Math.max(0, Math.min(12, (ram(sym.p_y) + 12 - 32) >> 4));
    const goal = (c, r) => s.walk ? (c === s.walk[0] && r === s.walk[1]) :
      (s.exit === 'N' ? r === 0 : s.exit === 'S' ? r === 12 : s.exit === 'W' ? c === 0 : c === 15);
    const prev = new Map(); const q = [[pc, pr]]; prev.set(pr * 16 + pc, null); let end = null;
    while (q.length) { const [c, r] = q.shift(); if (goal(c, r)) { end = [c, r]; break; }
      for (const [dc, dr] of [[1,0],[-1,0],[0,1],[0,-1]]) { const nc = c + dc, nr = r + dr; if (free(nc, nr) && !prev.has(nr * 16 + nc)) { prev.set(nr * 16 + nc, [c, r]); q.push([nc, nr]); } } }
    if (!end) { console.log('NO PATH from', pc, pr, 'to', s.walk || s.exit); }
    else {
      const path = []; let cur = end; while (cur) { path.unshift(cur); cur = prev.get(cur[1] * 16 + cur[0]); }
      const room0 = ram(sym.room);
      for (const [c, r] of path) {
        const tx = c * 16, ty = 32 + r * 16 - 4;
        for (let i = 0; i < 300; i++) {
          if (ram(sym.dlg_state)) { setHeld([]); frames(1); setHeld(['A']); frames(1); setHeld([]); frames(3); continue; }
          const dx = tx - ram(sym.p_x), dy = ty - ram(sym.p_y);
          if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) break;
          const btn = Math.abs(dx) > 1 ? [dx > 0 ? 'RIGHT' : 'LEFT'] : [dy > 0 ? 'DOWN' : 'UP'];
          setHeld(btn); frames(1);
        }
      }
      if (s.exit) { const d = { N: 'UP', S: 'DOWN', W: 'LEFT', E: 'RIGHT' }[s.exit];
        for (let i = 0; i < 200 && ram(sym.room) === room0; i++) { setHeld([d]); frames(1); }
        setHeld([]); frames(3); }
    }
    setHeld([]); frames(2);
  }
  if (s.poke) for (const [k, v] of Object.entries(s.poke)) nes.cpu.mem[sym[k]] = v;
  if (s.expect) for (const [k, v] of Object.entries(s.expect)) {
    const got = ram(sym[k]);
    if (got !== v) { failures++; console.log(`FAIL ${s.label || ''}: ${k} = ${got}, expected ${v}`); }
    else console.log(`ok   ${s.label || ''}: ${k} = ${v}`);
  }
  if (s.shot) shot(s.shot);
  if (s.dump) console.log(JSON.stringify(dump(s.dump)));
}

if (failures) { console.log(failures + ' check(s) failed'); process.exit(1); }
console.log('all checks passed');
