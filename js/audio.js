// Tiny WebAudio synth so the game has UI blips / chimes without any audio files.
let ctx = null, master = null, muted = false;

function ensure() {
  if (ctx) return ctx;
  try {
    ctx = new (window.AudioContext || window.webkitAudioContext)();
    master = ctx.createGain();
    master.gain.value = 0.5;
    master.connect(ctx.destination);
  } catch (e) { ctx = null; }
  return ctx;
}

function tone(freq, dur, { type = 'sine', vol = 0.15, at = 0, slide = 0 } = {}) {
  if (muted || !ensure()) return;
  const t = ctx.currentTime + at;
  const o = ctx.createOscillator();
  const g = ctx.createGain();
  o.type = type;
  o.frequency.setValueAtTime(freq, t);
  if (slide) o.frequency.exponentialRampToValueAtTime(Math.max(20, freq + slide), t + dur);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.exponentialRampToValueAtTime(vol, t + 0.01);
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  o.connect(g); g.connect(master);
  o.start(t); o.stop(t + dur + 0.05);
}

const SFX = {
  blip: () => tone(660 + Math.random() * 120, 0.04, { type: 'triangle', vol: 0.05 }),
  next: () => tone(520, 0.06, { type: 'triangle', vol: 0.06 }),
  choose: () => { tone(660, 0.08, { type: 'square', vol: 0.05 }); tone(990, 0.1, { type: 'square', vol: 0.05, at: 0.07 }); },
  chime: () => [784, 988, 1319].forEach((f, i) => tone(f, 0.5, { vol: 0.09, at: i * 0.09 })),
  scan: () => tone(300, 1.1, { type: 'sawtooth', vol: 0.04, slide: 1200 }),
  boot: () => { tone(110, 0.9, { type: 'sawtooth', vol: 0.05, slide: 330 }); [440, 554, 659, 880].forEach((f, i) => tone(f, 0.25, { vol: 0.07, at: 0.7 + i * 0.12 })); },
  buy: () => [523, 659, 784, 1047].forEach((f, i) => tone(f, 0.35, { vol: 0.09, at: i * 0.1 })),
  heart: () => { tone(880, 0.12, { vol: 0.07 }); tone(1174, 0.25, { vol: 0.07, at: 0.1 }); },
  bad: () => { tone(220, 0.25, { type: 'sawtooth', vol: 0.06 }); tone(165, 0.35, { type: 'sawtooth', vol: 0.06, at: 0.2 }); },
  door: () => { tone(200, 0.15, { type: 'square', vol: 0.05, slide: 200 }); tone(400, 0.2, { type: 'square', vol: 0.05, at: 0.12 }); },
};

export function sfx(name) { (SFX[name] || SFX.blip)(); }
export function unlockAudio() { ensure(); if (ctx && ctx.state === 'suspended') ctx.resume(); }
export function setMuted(m) { muted = m; }
export function isMuted() { return muted; }
