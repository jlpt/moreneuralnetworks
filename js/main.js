import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import { OutputPass } from 'three/addons/postprocessing/OutputPass.js';

import { Chizuru, POSES } from './chizuru.js';
import { BUILDERS, PROPS, loadClassroom } from './scenes.js';
import { Emotes } from './fx.js';
import { UI } from './ui.js';
import { sfx, music, unlockAudio, setMuted, isMuted } from './audio.js';
import { chapters, meta } from './story.js';

const $ = (id) => document.getElementById(id);
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const params = new URLSearchParams(location.search);

// ---------------------------------------------------------------- renderer
const canvas = $('c');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: false, powerPreference: 'high-performance', preserveDrawingBuffer: params.has('debug') });
renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.75));
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.NoToneMapping;

const scene = new THREE.Scene();
const pmrem = new THREE.PMREMGenerator(renderer);
scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;
scene.environmentIntensity = 0.35;

const camera = new THREE.PerspectiveCamera(45, 1, 0.05, 900);
camera.position.set(0, 1.5, 4);
const controls = new OrbitControls(camera, canvas);
controls.enableDamping = true; controls.dampingFactor = 0.08;
controls.enablePan = false; controls.minDistance = 0.5; controls.maxDistance = 9;
controls.maxPolarAngle = Math.PI * 0.62; controls.rotateSpeed = 0.55; controls.zoomSpeed = 0.6;

const composer = new EffectComposer(renderer, new THREE.WebGLRenderTarget(4, 4, { type: THREE.HalfFloatType, samples: 4 }));
composer.addPass(new RenderPass(scene, camera));
const bloom = new UnrealBloomPass(new THREE.Vector2(256, 256), 0.5, 0.65, 0.92);
composer.addPass(bloom);
composer.addPass(new OutputPass());

function resize() {
  const w = window.innerWidth, h = window.innerHeight;
  renderer.setSize(w, h, false);
  composer.setPixelRatio(renderer.getPixelRatio());
  composer.setSize(w, h);
  camera.aspect = w / h; camera.updateProjectionMatrix();
}
window.addEventListener('resize', resize); resize();

// ---------------------------------------------------------------- world
const chizuru = new Chizuru();
scene.add(chizuru.group);
const emotes = new Emotes(scene);
const ui = new UI();
const ctx = { speaker: null };
let current = null; // current scene object
const state = { credits: meta.startCredits ?? 0, rapport: meta.startRapport ?? 0 };

ui.onSpeaker = (who) => { ctx.speaker = who; };
ui.onTalk = (v) => chizuru.setTalking(v);

// ------------------------------------------------------------- scene mgmt
function setScene(key) {
  if (current) { scene.remove(current.group); }
  const b = BUILDERS[key];
  if (!b) { console.warn('unknown scene', key); return; }
  current = b();
  current.key = key;
  scene.add(current.group);
  scene.background = new THREE.Color(current.bg);
  scene.fog = current.fog ? new THREE.FogExp2(current.fog[0], current.fog[1]) : null;
  chizuru.setTint(current.tint, current.bright);
  ui.setLocation(current.title);
  emotes.clear();
  // reset camera to the scene default
  const c = current.cam;
  camera.position.set(...c.pos); controls.target.set(...c.look); camera.fov = c.fov; camera.updateProjectionMatrix();
  camTween = null;
  return current;
}

function anchor(name) {
  const a = current && current.anchors[name];
  return a || null;
}

// -------------------------------------------------------------- camera
// Shots are defined in Chizuru's local frame (she faces +Z).
const SHOTS = {
  face:   { p: [0.1, 1.47, 0.72],  l: [0, 1.43, 0], fov: 30 },
  close:  { p: [0.22, 1.42, 1.15], l: [0, 1.36, 0], fov: 32 },
  bust:   { p: [0.3, 1.36, 1.7],   l: [0, 1.28, 0], fov: 34 },
  medium: { p: [0.45, 1.3, 2.6],   l: [0, 1.1, 0],  fov: 36 },
  full:   { p: [0.6, 1.05, 3.8],   l: [0, 0.85, 0], fov: 38 },
  low:    { p: [0.35, 0.45, 2.5],  l: [0, 1.15, 0], fov: 40 },
  high:   { p: [0.4, 2.1, 2.3],    l: [0, 1.0, 0],  fov: 40 },
  side:   { p: [2.1, 1.3, 1.3],    l: [0, 1.15, 0], fov: 34 },
  pov:    { p: [0, 1.62, 2.3],     l: [0, 1.25, 0], fov: 40 },
  behind: { p: [0.3, 1.55, -1.8],  l: [0, 1.3, 1.5], fov: 42 },
};
const _v = new THREE.Vector3();
function resolveCam(spec) {
  if (!spec) return null;
  if (spec === 'scene') { const c = current.cam; return { pos: new THREE.Vector3(...c.pos), look: new THREE.Vector3(...c.look), fov: c.fov }; }
  if (typeof spec === 'string') spec = { shot: spec };
  if (spec.pos) return { pos: new THREE.Vector3(...spec.pos), look: new THREE.Vector3(...spec.look), fov: spec.fov || 45 };
  const s = SHOTS[spec.shot] || SHOTS.medium;
  const d = chizuru.dropTarget * 0.92;
  const off = spec.off || [0, 0, 0];
  const pos = chizuru.group.localToWorld(_v.set(s.p[0] + off[0], s.p[1] - d + off[1], s.p[2] + off[2]).clone());
  const look = chizuru.group.localToWorld(new THREE.Vector3(s.l[0], s.l[1] - d, s.l[2]));
  return { pos, look, fov: spec.fov || s.fov };
}

let camTween = null;
function moveCamera(spec, dur = 1.6) {
  const to = resolveCam(spec); if (!to) return;
  if (dur <= 0) { camera.position.copy(to.pos); controls.target.copy(to.look); camera.fov = to.fov; camera.updateProjectionMatrix(); camTween = null; return; }
  camTween = { t: 0, dur, fp: camera.position.clone(), fl: controls.target.clone(), ff: camera.fov, to };
}
function updateCamera(dt) {
  if (camTween) {
    const c = camTween; c.t += dt;
    const u = Math.min(1, c.t / c.dur), e = u * u * (3 - 2 * u);
    camera.position.lerpVectors(c.fp, c.to.pos, e);
    controls.target.lerpVectors(c.fl, c.to.look, e);
    camera.fov = c.ff + (c.to.fov - c.ff) * e; camera.updateProjectionMatrix();
    if (u >= 1) camTween = null;
  }
  controls.update();
}

// ------------------------------------------------------------- story runner
let runId = 0;
const propTweens = [];
function updateProps(dt) {
  for (let i = propTweens.length - 1; i >= 0; i--) {
    const p = propTweens[i]; p.t += dt;
    const u = Math.min(1, p.t / p.dur), e = u * u * (3 - 2 * u);
    p.o.position.lerpVectors(p.fp, p.tp, e);
    p.o.rotation.y = p.fr + (p.tr - p.fr) * e + p.spin * Math.sin(u * Math.PI) ;
    if (p.ts !== undefined) p.o.scale.setScalar(p.fs + (p.ts - p.fs) * e);
    if (u >= 1) { propTweens.splice(i, 1); if (p.then) p.then(); }
  }
}

function applyState(b) {
  if (b.hud) {
    if (b.hud.credits !== undefined) { state.credits = b.hud.credits; ui.setCredits(state.credits); }
    if (b.hud.rapport !== undefined) { state.rapport = b.hud.rapport; ui.setRapport(state.rapport); }
    if (b.hud.time) ui.setTime(b.hud.time);
    if (b.hud.show !== undefined) ui.showHud(b.hud.show);
  }
  if (b.rapport) { state.rapport += b.rapport; ui.setRapport(state.rapport); if (b.rapport > 0) sfx('heart'); }
  if (b.pay) { state.credits -= b.pay; ui.setCredits(state.credits); sfx('buy'); }
  if (b.place !== undefined) {
    const p = typeof b.place === 'string' ? anchor(b.place) : b.place;
    if (p) chizuru.place(p[0], p[1], p[2], p[3] ?? chizuru.group.rotation.y);
    chizuru.group.visible = true;
  }
  if (b.walk !== undefined) {
    const p = typeof b.walk === 'string' ? anchor(b.walk) : b.walk;
    if (p) chizuru.walkTo(p[0], p[1], p[2], b.walkDur || 2.2, p[3]);
  }
  if (b.face !== undefined) chizuru.faceYaw(b.face);
  if (b.hide) chizuru.group.visible = false;
  if (b.show) chizuru.group.visible = true;
  if (b.android !== undefined) chizuru.setAndroid(b.android);
  if (b.tint) chizuru.setTint(b.tint, b.bright ?? current.bright);
  if (b.music !== undefined) music(b.music === 'on');
  if (b.party !== undefined) { ctx.party = !!b.party; bloom.strength = b.party ? 0.75 : 0.5; }
  if (b.flip) { chizuru.startFlip(b.flip === true ? {} : b.flip); sfx('scan'); }
  if (b.style) chizuru.setStyle(b.style);
  if (b.pose) chizuru.setPose(b.pose, !!b.instant);
  if (b.mood) chizuru.setMood(b.mood);
  if (b.look !== undefined) chizuru.setLook(b.look === 'none' ? null : b.look);
  if (b.fx) {
    const list = Array.isArray(b.fx) ? b.fx : [b.fx];
    for (const f of list) {
      if (f === 'scan') { chizuru.startScan(); sfx('scan'); }
      else if (f === 'glitch') { chizuru.glitch(); sfx('bad'); }
      else if (f === 'flash') { chizuru.glitch(); }
      else if (f === 'boot') { sfx('boot'); chizuru.glitch(); }
    }
  }
  if (b.sfx) sfx(b.sfx);
  if (b.emote) {
    const list = Array.isArray(b.emote) ? b.emote : [b.emote];
    const hp = chizuru.headWorld(new THREE.Vector3());
    list.forEach((k, i) => {
      const pos = hp.clone().add(new THREE.Vector3(0.12 * i - 0.06 * (list.length - 1), 0.24, 0.05).applyQuaternion(chizuru.group.quaternion));
      emotes.spawn(k, pos, { count: k === 'heart' || k === 'sparkle' ? 4 : 1, size: k === 'heart' ? 0.13 : 0.16 });
    });
    if (list.includes('heart')) sfx('heart');
  }
  if (b.prop) {
    const o = current.props && current.props[b.prop.name];
    if (o && b.prop.pose && o.userData.setPose) o.userData.setPose(b.prop.pose);
    if (o) propTweens.push({ o, t: 0, dur: b.prop.dur ?? 1, fp: o.position.clone(), tp: new THREE.Vector3(...(b.prop.pos || o.position.toArray())), fr: o.rotation.y, tr: b.prop.rotY ?? o.rotation.y, spin: b.prop.spin || 0, fs: o.scale.x, ts: b.prop.scale ?? o.scale.x, then: b.prop.remove ? () => { o.parent && o.parent.remove(o); delete current.props[b.prop.name]; } : null });
  }
  if (b.spawn && PROPS[b.spawn.name]) {
    const o = PROPS[b.spawn.name]();
    o.position.set(...b.spawn.pos); if (b.spawn.rotY) o.rotation.y = b.spawn.rotY; o.scale.setScalar(b.spawn.noPop ? 1 : 0.01);
    if (o.userData.setPose) o.userData.setPose(b.spawn.pose || 'stand', true);
    current.group.add(o); (current.props ||= {})[b.spawn.name] = o;
    propTweens.push({ o, t: 0, dur: b.spawn.dur ?? 0.9, fp: o.position.clone(), tp: o.position.clone(), fr: o.rotation.y, tr: o.rotation.y, spin: 0, fs: o.scale.x, ts: b.spawn.noPop ? 1 : (b.spawn.scale ?? 1) });
    if (!b.spawn.noPop) {
      emotes.spawn('sparkle', new THREE.Vector3(b.spawn.pos[0], b.spawn.pos[1] + 0.15, b.spawn.pos[2]), { count: 6, size: 0.08, spread: 0.15, rise: 0.25 });
      sfx('chime');
    }
  }
  if (b.mouth !== undefined) chizuru.mouthForce = b.mouth === null ? null : b.mouth;
  if (b.cam !== undefined) moveCamera(b.cam, b.cut ? 0 : (b.camDur ?? 1.6));
  if (b.toast) ui.toast(b.toast);
  if (b.brightness !== undefined) { bloom.strength = b.brightness; }
}

async function exec(b, id) {
  if (id !== runId) throw new Error('aborted');
  if (b.scene) {
    if (b.fade !== false) await ui.fade(true, 600);
    ui.hideBox();
    setScene(b.scene);
    chizuru.group.visible = b.show !== false && !b.hide;
    if (b.place === undefined && current.anchors.pod) { /* scene default handled by story */ }
  }
  if (b.title) { ui.hideBox(); if (b.scene) await sleep(50); await ui.fade(true, 1); await ui.card(b.title, b.subtitle, b.cardMs); }
  applyState(b.scene ? { ...b, scene: undefined, show: undefined } : b);
  if (b.scene || b.title) {
    await sleep(60);
    await ui.fade(false, 800);
  }
  if (b.wait) await sleep(b.wait * 1000);
  if (id !== runId) throw new Error('aborted');
  if (b.say !== undefined) await ui.say(b.who || 'Chizuru', b.say);
  else if (b.n !== undefined) await ui.say(null, b.n, { narr: true });
  else if (b.you !== undefined) await ui.say('You', b.you);
  if (b.choice) {
    ui.hideBox();
    const i = await ui.choose(b.choice);
    const then = b.choice[i].then || [];
    for (const nb of then) await exec(nb, id);
  }
  if (b.after) { applyState(b.after); }
}

let chapterIdx = 0;
async function playFrom(idx) {
  const id = ++runId;
  ui.hideTurn();
  try {
    for (let i = idx; i < chapters.length; i++) {
      chapterIdx = i;
      try { const seen = +(localStorage.getItem('chizuru.seen') ?? -1); if (i > seen) localStorage.setItem('chizuru.seen', String(i)); } catch (e) { /* storage unavailable */ }
      updateChapterMenu();
      const ch = chapters[i];
      for (const b of ch.beats) await exec(b, id);
      ui.hideBox();
      const hasNext = i < chapters.length - 1;
      if (ch.turn) {
        ui.showTurn(ch.turn.prompt, ch.turn.ideas, hasNext);
        if (hasNext) await new Promise((r) => { $('turn-next').onclick = () => { unlockAudio(); r(); }; });
        ui.hideTurn();
      }
    }
  } catch (e) { if (e.message !== 'aborted') console.error(e); }
}

function updateChapterMenu() {
  const sel = $('chapter-select');
  sel.value = String(chapterIdx);
}

function buildChapterMenu() {
  const sel = $('chapter-select');
  sel.innerHTML = chapters.map((c, i) => `<option value="${i}">${i + 1}. ${c.title}</option>`).join('');
  sel.onchange = () => { ui.toggleLog(false); playFrom(+sel.value); };
  $('btn-latest').onclick = () => playFrom(chapters.length - 1);
  $('btn-mute').onclick = () => { setMuted(!isMuted()); $('btn-mute').textContent = isMuted() ? '🔇' : '🔊'; };
}

// -------------------------------------------------------------- main loop
const clock = new THREE.Clock();
let t = 0;
function frame() {
  const dt = Math.min(clock.getDelta(), 0.05);
  t += dt;
  if (current) current.update(dt, t, ctx);
  chizuru.update(dt, camera);
  emotes.update(dt);
  updateProps(dt);
  if (current && current.props) for (const o of Object.values(current.props)) if (o.userData.update) o.userData.update(dt, t);
  updateCamera(dt);
  composer.render();
  requestAnimationFrame(frame);
}

// ---------------------------------------------------------------- boot
async function boot() {
  const bar = $('loadbar');
  const setP = (p, label) => { bar.style.width = (p * 100) + '%'; if (label) $('loadtxt').textContent = label; };
  try {
    setP(0.1, 'Loading Chizuru…');
    await chizuru.load('assets/chizuru/scene.json');
    setP(0.55, 'Loading classroom…');
    await loadClassroom('assets/classroom-data.wasm');
    setP(1, 'Ready');
  } catch (e) {
    console.error(e);
    $('loadtxt').textContent = 'Failed to load assets: ' + e.message;
    return;
  }
  buildChapterMenu();
  ui.showHud(false);
  ui.setCredits(state.credits); ui.setRapport(state.rapport);
  setScene(meta.startScene || 'store');
  chizuru.group.visible = false;
  frame();

  // debug hooks (used for automated screenshots)
  window.__g = { chizuru, camera, controls, THREE, setScene, moveCamera, applyState, ui, emotes, get scene() { return current; }, playFrom, POSES, bloom };
  window.done = 1;

  const startBtn = $('start');
  startBtn.disabled = false; startBtn.classList.add('ready');
  $('loader').classList.add('loaded');
  startBtn.onclick = async () => {
    unlockAudio();
    $('title').classList.add('gone');
    ui.showHud(true);
    playFrom(+(params.get('ch') || 0));
  };
  window.addEventListener('pointerdown', unlockAudio, { once: true });
  // Resume: after a republish, jump straight to the first chapter you haven't seen yet.
  let seen = -1;
  try { seen = +(localStorage.getItem('chizuru.seen') ?? -1); } catch (e) { /* ignore */ }
  if (params.has('ch')) { /* explicit chapter wins */ }
  else if (seen >= 0 && !params.has('fresh')) {
    startBtn.textContent = 'CONTINUE';
    $('title').classList.add('gone');
    ui.showHud(true);
    playFrom(Math.min(seen + 1, chapters.length - 1));
  }
  if (params.has('skiptitle')) { $('title').classList.add('gone'); }
}
boot();
