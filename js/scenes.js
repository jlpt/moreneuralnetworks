import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

// ---------------------------------------------------------------------------
// Scene builders. Each returns:
//   { group, update(dt,t,ctx), bg, fog, cam:{pos,look,fov}, anchors:{name:[x,y,z,rotY]}, tint, bright, name, title }
// Add new locations by adding a builder to BUILDERS — the story refers to them by key.
// ---------------------------------------------------------------------------

const V = (x, y, z) => new THREE.Vector3(x, y, z);

function canvasTex(w, h, draw, { repeat, srgb = true } = {}) {
  const c = document.createElement('canvas'); c.width = w; c.height = h;
  draw(c.getContext('2d'), w, h);
  const t = new THREE.CanvasTexture(c);
  if (srgb) t.colorSpace = THREE.SRGBColorSpace;
  t.anisotropy = 8;
  if (repeat) { t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(...repeat); }
  return t;
}

function signTexture(lines, { w = 1024, h = 256, color = '#7df9ff', glow = '#00cfff', bg = null, font = '900 120px Rajdhani, sans-serif' } = {}) {
  return canvasTex(w, h, (g) => {
    if (bg) { g.fillStyle = bg; g.fillRect(0, 0, w, h); }
    g.textAlign = 'center'; g.textBaseline = 'middle';
    const n = lines.length;
    lines.forEach((ln, i) => {
      const [text, f] = Array.isArray(ln) ? ln : [ln, font];
      g.font = f;
      g.shadowColor = glow; g.shadowBlur = 28; g.fillStyle = color;
      g.fillText(text, w / 2, (h / (n + 1)) * (i + 1));
      g.shadowBlur = 8; g.fillText(text, w / 2, (h / (n + 1)) * (i + 1));
    });
  });
}

function glowPlane(tex, w, h, opacity = 1) {
  const m = new THREE.Mesh(new THREE.PlaneGeometry(w, h), new THREE.MeshBasicMaterial({ map: tex, transparent: true, opacity, toneMapped: false, depthWrite: false, side: THREE.DoubleSide }));
  return m;
}

const std = (color, o = {}) => new THREE.MeshStandardMaterial({ color, roughness: 0.6, metalness: 0.1, ...o });
const emis = (color, k = 1.5) => new THREE.MeshBasicMaterial({ color: new THREE.Color(color).multiplyScalar(k), toneMapped: false });
function box(w, h, d, mat, x = 0, y = 0, z = 0) { const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat); m.position.set(x, y, z); return m; }

function mannequin(color = 0xdfe5ee, accent = 0x66e0ff) {
  const g = new THREE.Group();
  const skin = std(color, { roughness: 0.35, metalness: 0.5 });
  const led = emis(accent, 1.4);
  const part = (geo, mat, x, y, z, rx = 0, rz = 0) => { const m = new THREE.Mesh(geo, mat); m.position.set(x, y, z); m.rotation.set(rx, 0, rz); g.add(m); return m; };
  part(new THREE.CapsuleGeometry(0.16, 0.42, 6, 14), skin, 0, 1.2, 0);           // torso
  part(new THREE.SphereGeometry(0.115, 20, 16), skin, 0, 1.68, 0);              // head
  part(new THREE.TorusGeometry(0.12, 0.004, 6, 32), led, 0, 1.68, 0, Math.PI / 2, 0);
  for (const s of [-1, 1]) {
    part(new THREE.CapsuleGeometry(0.045, 0.5, 4, 10), skin, s * 0.24, 1.12, 0, 0, s * 0.08); // arms
    part(new THREE.CapsuleGeometry(0.06, 0.62, 4, 10), skin, s * 0.09, 0.55, 0);              // legs
    part(new THREE.BoxGeometry(0.012, 0.36, 0.012), led, s * 0.24, 1.1, 0.045, 0, s * 0.08);
  }
  part(new THREE.BoxGeometry(0.24, 0.008, 0.012), led, 0, 1.3, 0.16);
  return g;
}

function skylineTexture(seed = 1) {
  return canvasTex(128, 256, (g, w, h) => {
    g.fillStyle = '#0b1020'; g.fillRect(0, 0, w, h);
    let s = seed * 9301 + 49297;
    const rnd = () => ((s = (s * 9301 + 49297) % 233280) / 233280);
    for (let y = 6; y < h - 4; y += 10) for (let x = 6; x < w - 4; x += 9) {
      if (rnd() > 0.45) {
        const c = rnd();
        g.fillStyle = c < 0.6 ? '#ffe9a8' : c < 0.85 ? '#8fdcff' : '#ff9fd6';
        g.globalAlpha = 0.5 + rnd() * 0.5;
        g.fillRect(x, y, 5, 6);
      }
    }
    g.globalAlpha = 1;
  }, { repeat: [1, 1] });
}

function buildSkyline(group, { rings = true } = {}) {
  const sky = new THREE.Mesh(
    new THREE.SphereGeometry(400, 32, 16),
    new THREE.MeshBasicMaterial({
      side: THREE.BackSide, toneMapped: false, fog: false,
      map: canvasTex(8, 256, (g, w, h) => {
        const gr = g.createLinearGradient(0, 0, 0, h);
        gr.addColorStop(0, '#05061a'); gr.addColorStop(0.45, '#241548'); gr.addColorStop(0.62, '#7a2f7d'); gr.addColorStop(0.72, '#ff7aa8'); gr.addColorStop(1, '#ffb27a');
        g.fillStyle = gr; g.fillRect(0, 0, w, h);
      }),
    }));
  sky.position.y = 0;
  group.add(sky);
  // moon / planet
  const moon = new THREE.Mesh(new THREE.SphereGeometry(28, 32, 24), new THREE.MeshBasicMaterial({ color: 0xfff1d6, toneMapped: false, fog: false }));
  moon.position.set(-90, 120, -300);
  group.add(moon);
  if (rings) {
    const ring = new THREE.Mesh(new THREE.RingGeometry(40, 60, 64), new THREE.MeshBasicMaterial({ color: 0xffd6e8, transparent: true, opacity: 0.45, side: THREE.DoubleSide, toneMapped: false, fog: false }));
    ring.position.copy(moon.position); ring.rotation.set(1.2, 0.2, 0.3);
    group.add(ring);
  }
  // stars
  const sp = []; for (let i = 0; i < 500; i++) { const a = Math.random() * Math.PI * 2, e = Math.random() * 0.9 + 0.05; sp.push(Math.cos(a) * Math.cos(e) * 380, Math.sin(e) * 380 + 20, Math.sin(a) * Math.cos(e) * 380); }
  const sg = new THREE.BufferGeometry(); sg.setAttribute('position', new THREE.Float32BufferAttribute(sp, 3));
  group.add(new THREE.Points(sg, new THREE.PointsMaterial({ size: 1.6, color: 0xffffff, fog: false, toneMapped: false })));

  // towers
  const towers = new THREE.Group();
  const texs = [skylineTexture(1), skylineTexture(2), skylineTexture(3)];
  const darkMat = std(0x0e1226, { roughness: 0.9 });
  for (let i = 0; i < 90; i++) {
    const w = 6 + Math.random() * 12, d = 6 + Math.random() * 12, h = 20 + Math.random() * 110;
    const x = (Math.random() - 0.5) * 320, z = -35 - Math.random() * 190;
    const tex = texs[i % 3].clone(); tex.needsUpdate = true; tex.repeat.set(Math.max(1, Math.round(w / 8)), Math.max(1, Math.round(h / 16)));
    const mat = new THREE.MeshBasicMaterial({ map: tex, color: 0x9a9ac0, toneMapped: false });
    const b = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), [mat, mat, darkMat, darkMat, mat, mat]);
    b.position.set(x, h / 2 - 5, z);
    towers.add(b);
    if (Math.random() < 0.25) { // rooftop beacon
      const l = new THREE.Mesh(new THREE.SphereGeometry(0.7, 8, 8), emis(0xff4060, 2)); l.position.set(x, h - 4, z); towers.add(l);
    }
  }
  group.add(towers);
  // holo billboards
  const boards = [];
  [['NEO-SHIBUYA', '#ff5fa8'], ['レンタル彼女', '#7df9ff'], ['REPLICA', '#ffd86b'], ['ANDROID LOVE', '#b58cff']].forEach(([t, c], i) => {
    const p = glowPlane(signTexture([[t, '900 150px Rajdhani, sans-serif']], { color: c, glow: c, w: 1024, h: 256 }), 30, 7.5, 0.95);
    p.position.set(-70 + i * 45, 45 + (i % 2) * 22, -75 - i * 8);
    group.add(p); boards.push(p);
  });
  // flying cars
  const cars = [];
  for (let i = 0; i < 14; i++) {
    const car = new THREE.Group();
    car.add(box(1.6, 0.4, 0.6, emis(0xffffff, 1.5)));
    const tail = box(0.25, 0.25, 0.4, emis(i % 2 ? 0xff3355 : 0x33e0ff, 3), i % 2 ? -0.9 : 0.9, 0, 0);
    car.add(tail);
    const trail = new THREE.Mesh(new THREE.PlaneGeometry(9, 0.22), new THREE.MeshBasicMaterial({ color: i % 2 ? 0xff3355 : 0x33e0ff, transparent: true, opacity: 0.35, toneMapped: false, depthWrite: false }));
    trail.position.x = i % 2 ? -5.4 : 5.4;
    car.add(trail);
    car.userData = { speed: (i % 2 ? -1 : 1) * (8 + Math.random() * 14), y: 12 + Math.random() * 60, z: -25 - Math.random() * 60 };
    car.position.set((Math.random() - 0.5) * 240, car.userData.y, car.userData.z);
    if (car.userData.speed < 0) car.rotation.y = Math.PI;
    group.add(car); cars.push(car);
  }
  return {
    update(dt, t) {
      for (const c of cars) { c.position.x += c.userData.speed * dt; if (c.position.x > 130) c.position.x = -130; if (c.position.x < -130) c.position.x = 130; }
      for (const b of boards) b.material.opacity = 0.85 + Math.sin(t * 2 + b.position.x) * 0.12;
    },
  };
}

// ---------------------------------------------------------------------------
// 1. Android showroom — "REPLICA Companion Robotics"
// ---------------------------------------------------------------------------
function buildStore() {
  const group = new THREE.Group();
  const lights = new THREE.Group(); group.add(lights);

  // floor: dark glossy tiles with glowing seams
  const floorTex = canvasTex(512, 512, (g, w, h) => {
    g.fillStyle = '#080c16'; g.fillRect(0, 0, w, h);
    g.strokeStyle = '#1b3d5c'; g.lineWidth = 3; g.strokeRect(0, 0, w, h);
    g.strokeStyle = 'rgba(80,220,255,0.35)'; g.lineWidth = 1.5; g.strokeRect(3, 3, w - 6, h - 6);
  }, { repeat: [14, 14] });
  const floor = new THREE.Mesh(new THREE.PlaneGeometry(28, 28), new THREE.MeshStandardMaterial({ map: floorTex, roughness: 0.18, metalness: 0.85, envMapIntensity: 1.2 }));
  floor.rotation.x = -Math.PI / 2; group.add(floor);

  // back wall + neon sign
  const wallMat = std(0x0c1224, { roughness: 0.5, metalness: 0.6 });
  const back = new THREE.Mesh(new THREE.PlaneGeometry(28, 9), wallMat); back.position.set(0, 4.5, -5.2); group.add(back);
  const sign = glowPlane(signTexture([['REPLICA', '900 170px Rajdhani, sans-serif'], ['COMPANION  ROBOTICS  ·  レプリカ', '600 56px Rajdhani, sans-serif']], { w: 1400, h: 380 }), 7, 1.9);
  sign.position.set(0, 3.6, -5.1); group.add(sign);
  // wall light strips
  for (let i = -3; i <= 3; i++) group.add(box(0.06, 5, 0.06, emis(i % 2 ? 0xff5fa8 : 0x33e0ff, 1.6), i * 3.2, 2.5, -5.1));
  // side walls
  for (const s of [-1, 1]) { const w = new THREE.Mesh(new THREE.PlaneGeometry(14, 9), wallMat); w.position.set(s * 9, 4.5, 0); w.rotation.y = -s * Math.PI / 2; group.add(w); }
  // ceiling with light bars
  const ceil = new THREE.Mesh(new THREE.PlaneGeometry(28, 28), std(0x070a14, { roughness: 0.9 })); ceil.rotation.x = Math.PI / 2; ceil.position.y = 4.2; group.add(ceil);
  for (let z = -4; z <= 6; z += 2.5) for (const x of [-4.2, 0, 4.2]) group.add(box(2.6, 0.05, 0.14, emis(0xdff6ff, 1.8), x, 4.15, z));

  // display pods
  const pods = [];
  const pod = (x, z, big = false) => {
    const g = new THREE.Group(); g.position.set(x, 0, z);
    const r = big ? 0.85 : 0.62;
    const base = new THREE.Mesh(new THREE.CylinderGeometry(r, r + 0.06, 0.12, 40), std(0x151c2e, { metalness: 0.9, roughness: 0.25 }));
    base.position.y = 0.06; g.add(base);
    const ring = new THREE.Mesh(new THREE.TorusGeometry(r - 0.05, 0.012, 8, 64), emis(big ? 0xff8fc7 : 0x33e0ff, 2)); ring.rotation.x = Math.PI / 2; ring.position.y = 0.13; g.add(ring);
    const glass = new THREE.Mesh(new THREE.CylinderGeometry(r, r, 2.55, 40, 1, true), new THREE.MeshPhysicalMaterial({ color: 0x9fd8ff, transparent: true, opacity: 0.04, roughness: 0.05, side: THREE.DoubleSide, depthWrite: false }));
    glass.position.y = 1.38; g.add(glass);
    const cap = new THREE.Mesh(new THREE.CylinderGeometry(r + 0.05, r + 0.05, 0.12, 40), std(0x151c2e, { metalness: 0.9, roughness: 0.25 })); cap.position.y = 2.72; g.add(cap);
    const capRing = new THREE.Mesh(new THREE.TorusGeometry(r, 0.012, 8, 64), emis(big ? 0xff8fc7 : 0x33e0ff, 2)); capRing.rotation.x = Math.PI / 2; capRing.position.y = 2.65; g.add(capRing);
    const beam = new THREE.Mesh(new THREE.CylinderGeometry(r * 0.95, r * 0.95, 2.5, 32, 1, true), new THREE.MeshBasicMaterial({ color: big ? 0xff8fc7 : 0x33e0ff, transparent: true, opacity: big ? 0.018 : 0.02, side: THREE.DoubleSide, depthWrite: false, blending: THREE.AdditiveBlending, toneMapped: false }));
    beam.position.y = 1.38; g.add(beam);
    group.add(g); pods.push({ g, ring, capRing });
    return g;
  };
  const hero = pod(0, -1.5, true);
  [[-2.7, -2.6], [2.7, -2.6], [-4.7, -2.0], [4.7, -2.0], [-6.7, -1.4], [6.7, -1.4]].forEach(([x, z], i) => {
    pod(x, z);
    const m = mannequin(i % 2 ? 0xe9dfe8 : 0xdfe8ee, i % 2 ? 0xff8fc7 : 0x66e0ff);
    m.position.set(x, 0.12, z); m.rotation.y = -Math.sign(x) * 0.35; group.add(m);
  });

  // hologram price tag beside the hero pod
  const tag = new THREE.Group();
  const tagTex = canvasTex(640, 800, (g, w, h) => {
    g.fillStyle = 'rgba(8,20,40,0.72)'; g.fillRect(0, 0, w, h);
    g.strokeStyle = '#7df9ff'; g.lineWidth = 6; g.strokeRect(10, 10, w - 20, h - 20);
    g.fillStyle = '#7df9ff'; g.textAlign = 'left';
    g.font = '600 38px Rajdhani, sans-serif'; g.fillText('COMPANION SERIES · MZ-01', 44, 82);
    g.fillStyle = '#ffffff'; g.font = '900 96px Rajdhani, sans-serif'; g.fillText('CHIZURU', 44, 190);
    g.fillStyle = '#ff9fd0'; g.font = '700 44px Rajdhani, sans-serif'; g.fillText('水原 千鶴  ·  "Rental Girlfriend"', 44, 250);
    g.fillStyle = '#cfe9ff'; g.font = '500 36px Rajdhani, sans-serif';
    ['Height 161 cm · Weight 47 kg', 'Personality: Polite / Composed', 'Firmware: Professional Smile 3.2', 'Battery: 72 h · Waterproof: IP67', 'Memory: 8 PB affective core', 'Warranty: 5 years + heart-care'].forEach((t, i) => g.fillText(t, 44, 330 + i * 56));
    g.fillStyle = '#ffe066'; g.font = '900 84px Rajdhani, sans-serif'; g.fillText('¥ 1,980,000', 44, 730);
  });
  const tp = new THREE.Mesh(new THREE.PlaneGeometry(1.5, 1.875), new THREE.MeshBasicMaterial({ map: tagTex, transparent: true, opacity: 0.95, toneMapped: false, depthWrite: false, side: THREE.DoubleSide }));
  tag.add(tp); tag.position.set(2.35, 1.55, -1.0); tag.rotation.y = -0.35; group.add(tag);

  // AYA — the shop's AI concierge, a floating orb
  const aya = new THREE.Group();
  const orb = new THREE.Mesh(new THREE.SphereGeometry(0.16, 32, 24), emis(0x7df9ff, 1.6)); aya.add(orb);
  const r1 = new THREE.Mesh(new THREE.TorusGeometry(0.26, 0.008, 8, 64), emis(0xffffff, 1.5)); aya.add(r1);
  const r2 = new THREE.Mesh(new THREE.TorusGeometry(0.33, 0.006, 8, 64), emis(0xff8fc7, 1.5)); aya.add(r2);
  aya.position.set(-1.9, 1.55, 0.9); group.add(aya);
  const ayaLight = new THREE.PointLight(0x7df9ff, 1.2, 4); aya.add(ayaLight);

  // ambient + accent lighting
  lights.add(new THREE.HemisphereLight(0x8fb8ff, 0x101830, 0.55));
  const key = new THREE.SpotLight(0xffffff, 60, 12, 0.5, 0.6, 1.5); key.position.set(0, 4, 1.2); key.target.position.set(0, 1, -1.5); lights.add(key, key.target);
  const pink = new THREE.PointLight(0xff5fa8, 14, 9); pink.position.set(-3, 2.5, 1); lights.add(pink);
  const cyan = new THREE.PointLight(0x33e0ff, 14, 9); cyan.position.set(3, 2.5, 1); lights.add(cyan);

  // drifting dust
  const dp = []; for (let i = 0; i < 160; i++) dp.push((Math.random() - 0.5) * 14, Math.random() * 4, -5 + Math.random() * 10);
  const dg = new THREE.BufferGeometry(); dg.setAttribute('position', new THREE.Float32BufferAttribute(dp, 3));
  const dust = new THREE.Points(dg, new THREE.PointsMaterial({ size: 0.03, color: 0xbfe8ff, transparent: true, opacity: 0.5, toneMapped: false }));
  group.add(dust);

  return {
    group, name: 'store', title: 'REPLICA Companion Robotics — Shibuya',
    bg: 0x05070f, fog: [0x05070f, 0.045],
    cam: { pos: [0.6, 1.5, 4.4], look: [0, 1.2, -1.5], fov: 45 },
    anchors: { pod: [0, 0.12, -1.5, 0], front: [0, 0, 0.6, 0], counter: [1.2, 0, 0.6, -0.3] },
    tint: 0xe6eeff, bright: 0.95,
    hero, tag,
    update(dt, t, ctx) {
      aya.position.y = 1.55 + Math.sin(t * 1.4) * 0.05;
      const talking = ctx.speaker === 'AYA';
      const s = 1 + (talking ? Math.sin(t * 16) * 0.09 : Math.sin(t * 2) * 0.02);
      orb.scale.setScalar(s); r1.rotation.x += dt * 1.6; r1.rotation.y += dt * 0.7; r2.rotation.y += dt * 1.2; r2.rotation.z += dt * 0.5;
      ayaLight.intensity = talking ? 3 : 1.2;
      tag.position.y = 1.55 + Math.sin(t * 1.1) * 0.03;
      dust.rotation.y = t * 0.02; dust.position.y = Math.sin(t * 0.3) * 0.1;
      for (const p of pods) { p.ring.material.color.multiplyScalar(1); }
    },
  };
}

// ---------------------------------------------------------------------------
// 2. Your apartment — high-rise, floor-to-ceiling window over Neo-Shibuya
// ---------------------------------------------------------------------------
function buildApartment() {
  const group = new THREE.Group();
  const sky = buildSkyline(group);

  // floor (wood planks)
  const plank = canvasTex(512, 512, (g, w, h) => {
    for (let i = 0; i < 8; i++) {
      const l = 44 + Math.random() * 10;
      g.fillStyle = `hsl(28, 38%, ${l}%)`; g.fillRect(0, i * 64, w, 62);
      g.fillStyle = 'rgba(0,0,0,0.25)'; g.fillRect(0, i * 64 + 62, w, 2);
      for (let k = 0; k < 40; k++) { g.fillStyle = 'rgba(60,30,10,0.07)'; g.fillRect(Math.random() * w, i * 64 + Math.random() * 60, 60 + Math.random() * 80, 1); }
      g.fillStyle = 'rgba(0,0,0,0.2)'; g.fillRect(((i * 197) % 480) + 20, i * 64, 2, 62);
    }
  }, { repeat: [3, 2.4] });
  const floor = new THREE.Mesh(new THREE.PlaneGeometry(9, 7), new THREE.MeshStandardMaterial({ map: plank, roughness: 0.4, metalness: 0.05 }));
  floor.rotation.x = -Math.PI / 2; floor.position.set(0, 0, 0.5); group.add(floor);

  const wall = std(0xe9e2da, { roughness: 0.9 });
  const trim = std(0x20242e, { roughness: 0.5, metalness: 0.4 });
  // back wall with big window opening (x -3.2..3.2, y 0.25..2.6)
  const zb = -3, H = 2.9;
  group.add(box(9, 0.25, 0.2, wall, 0, 0.125, zb));
  group.add(box(9, H - 2.6, 0.2, wall, 0, (2.6 + H) / 2, zb));
  group.add(box(1.3, 2.35, 0.2, wall, -3.85, 1.425, zb));
  group.add(box(1.3, 2.35, 0.2, wall, 3.85, 1.425, zb));
  for (const x of [-3.2, -1.07, 1.07, 3.2]) group.add(box(0.06, 2.35, 0.08, trim, x, 1.425, zb + 0.02));
  group.add(box(6.5, 0.06, 0.08, trim, 0, 2.6, zb + 0.02), box(6.5, 0.06, 0.08, trim, 0, 0.25, zb + 0.02));
  // side walls, ceiling
  for (const s of [-1, 1]) { const w = new THREE.Mesh(new THREE.PlaneGeometry(7, H), wall); w.position.set(s * 4.5, H / 2, 0.5); w.rotation.y = -s * Math.PI / 2; group.add(w); }
  const back2 = new THREE.Mesh(new THREE.PlaneGeometry(9, H), wall); back2.position.set(0, H / 2, 4); back2.rotation.y = Math.PI; group.add(back2);
  const ceil = new THREE.Mesh(new THREE.PlaneGeometry(9, 7), std(0xf4f0ea, { roughness: 1 })); ceil.rotation.x = Math.PI / 2; ceil.position.set(0, H, 0.5); group.add(ceil);
  group.add(box(3, 0.04, 0.3, emis(0xfff2dc, 1.4), 0, H - 0.03, 0.6));

  // rug
  const rug = new THREE.Mesh(new THREE.CircleGeometry(1.7, 48), std(0x6a4f7a, { roughness: 1 })); rug.rotation.x = -Math.PI / 2; rug.position.set(-0.6, 0.006, 0.3); group.add(rug);
  const rug2 = new THREE.Mesh(new THREE.RingGeometry(1.3, 1.36, 48), std(0xf1c9de, { roughness: 1 })); rug2.rotation.x = -Math.PI / 2; rug2.position.set(-0.6, 0.008, 0.3); group.add(rug2);

  // sofa (faces +x)
  const sofa = new THREE.Group(); sofa.position.set(-3.6, 0, 0.4); sofa.rotation.y = Math.PI / 2;
  const fab = std(0x3d4a6b, { roughness: 0.95 });
  sofa.add(box(2.2, 0.32, 0.95, fab, 0, 0.26, 0), box(2.2, 0.55, 0.22, fab, 0, 0.62, -0.42), box(0.22, 0.5, 0.95, fab, -1.0, 0.45, 0), box(0.22, 0.5, 0.95, fab, 1.0, 0.45, 0));
  sofa.add(box(0.8, 0.14, 0.9, std(0x565f80), -0.38, 0.5, 0.02), box(0.8, 0.14, 0.9, std(0x565f80), 0.42, 0.5, 0.02));
  sofa.add(box(0.4, 0.4, 0.12, std(0xf0a7c5, { roughness: 1 }), 0.65, 0.68, -0.3));
  group.add(sofa);
  // coffee table
  const tbl = new THREE.Group(); tbl.position.set(-1.7, 0, 0.4);
  tbl.add(box(1.1, 0.05, 0.6, std(0x2b2320, { roughness: 0.3 }), 0, 0.4, 0));
  for (const [x, z] of [[-0.48, -0.24], [0.48, -0.24], [-0.48, 0.24], [0.48, 0.24]]) tbl.add(box(0.04, 0.4, 0.04, trim, x, 0.2, z));
  const cup = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.035, 0.09, 16), std(0xffffff)); cup.position.set(0.2, 0.47, 0.05); tbl.add(cup);
  const holo = new THREE.Mesh(new THREE.ConeGeometry(0.12, 0.3, 24, 1, true), new THREE.MeshBasicMaterial({ color: 0x66e0ff, transparent: true, opacity: 0.25, side: THREE.DoubleSide, blending: THREE.AdditiveBlending, depthWrite: false, toneMapped: false }));
  holo.position.set(-0.25, 0.58, 0); holo.rotation.x = Math.PI; tbl.add(holo);
  group.add(tbl);
  // charging cradle
  const dock = new THREE.Group(); dock.position.set(3.7, 0, -1.9);
  dock.add(box(0.9, 0.08, 0.7, trim, 0, 0.04, 0), box(0.9, 2.15, 0.1, std(0x171c2a, { metalness: 0.7, roughness: 0.3 }), 0, 1.15, -0.3));
  const dr = new THREE.Mesh(new THREE.TorusGeometry(0.4, 0.012, 8, 48), emis(0x66e0ff, 2)); dr.rotation.x = Math.PI / 2; dr.position.set(0, 0.1, 0); dock.add(dr);
  dock.add(box(0.03, 1.9, 0.03, emis(0x66e0ff, 2), -0.4, 1.1, -0.24), box(0.03, 1.9, 0.03, emis(0x66e0ff, 2), 0.4, 1.1, -0.24));
  const dl = glowPlane(signTexture([['CHARGING CRADLE', '700 76px Rajdhani, sans-serif'], ['READY', '900 96px Rajdhani, sans-serif']], { w: 640, h: 320 }), 0.7, 0.35);
  dl.position.set(0, 1.85, -0.24); dock.add(dl);
  group.add(dock);
  // plant + lamp
  const pot = new THREE.Mesh(new THREE.CylinderGeometry(0.22, 0.17, 0.4, 16), std(0xd8d0c8)); pot.position.set(3.7, 0.2, 1.8); group.add(pot);
  for (let i = 0; i < 9; i++) { const l = new THREE.Mesh(new THREE.SphereGeometry(0.2, 10, 8), std(0x3f9a5c, { roughness: 0.8 })); l.scale.set(0.5, 1.6, 0.12); const a = i / 9 * Math.PI * 2; l.position.set(3.7 + Math.cos(a) * 0.17, 0.85, 1.8 + Math.sin(a) * 0.17); l.rotation.set(Math.sin(a) * 0.5, 0, -Math.cos(a) * 0.5); group.add(l); }
  const lamp = new THREE.Group(); lamp.position.set(-3.9, 0, -2.4);
  lamp.add(box(0.05, 1.5, 0.05, trim, 0, 0.75, 0)); const shade = new THREE.Mesh(new THREE.ConeGeometry(0.22, 0.3, 20, 1, true), new THREE.MeshBasicMaterial({ color: 0xffd9a0, side: THREE.DoubleSide, toneMapped: false })); shade.position.y = 1.55; lamp.add(shade);
  group.add(lamp);

  const lights = new THREE.Group(); group.add(lights);
  lights.add(new THREE.HemisphereLight(0xffe6d0, 0x2b2540, 0.9));
  const warm = new THREE.PointLight(0xffb070, 22, 9); warm.position.set(-3.6, 1.5, -2.2); lights.add(warm);
  const ceilL = new THREE.PointLight(0xfff0dd, 16, 9); ceilL.position.set(0, 2.6, 0.6); lights.add(ceilL);
  const cool = new THREE.DirectionalLight(0x8fb8ff, 1.4); cool.position.set(0, 3, -6); lights.add(cool);

  return {
    group, name: 'apartment', title: 'Your apartment — Neo-Shibuya Tower, 41F',
    bg: 0x0b0e1a, fog: null,
    cam: { pos: [0.3, 1.5, 3.4], look: [0, 1.15, -1.0], fov: 46 },
    anchors: { center: [0, 0, -0.6, 0], window: [0, 0, -2.0, 0], door: [1.2, 0, 3.0, Math.PI], dock: [3.7, 0.08, -1.55, -0.2], sofa: [-3.55, 0, 0.5, Math.PI / 2], table: [-0.9, 0, 0.9, 0.2] },
    tint: 0xfff0e6, bright: 0.96,
    update(dt, t, ctx) { sky.update(dt, t); dr.material.color.setHex(0x66e0ff).multiplyScalar(1.6 + Math.sin(t * 2) * 0.5); },
  };
}

// ---------------------------------------------------------------------------
// 3. Classroom (your GLB) — inches -> metres, SketchUp scale-figure removed
// ---------------------------------------------------------------------------
let classroomGLB = null;
export async function loadClassroom(url, onProgress) {
  const gltf = await new GLTFLoader().loadAsync(url, onProgress);
  classroomGLB = gltf.scene;
  classroomGLB.traverse((o) => {
    if (o.isMesh) {
      if (/^Sumele/.test(o.material?.name || '')) o.visible = false; // SketchUp human scale figure
      const m = o.material;
      if (m && m.map) m.map.anisotropy = 8;
    }
  });
  classroomGLB.scale.setScalar(0.0254);
  classroomGLB.updateMatrixWorld(true);
  const box3 = new THREE.Box3();
  classroomGLB.traverse((o) => { if (o.isMesh && o.visible) box3.expandByObject(o); });
  // put the room's floor centre at the origin
  const c = box3.getCenter(new THREE.Vector3());
  classroomGLB.position.set(-c.x, -box3.min.y, -c.z);
  classroomGLB.userData.box = box3.clone().translate(classroomGLB.position);
  return classroomGLB;
}

function buildClassroom() {
  const group = new THREE.Group();
  const room = classroomGLB;
  group.add(room);
  const b = room.userData.box;
  const lights = new THREE.Group(); group.add(lights);
  lights.add(new THREE.HemisphereLight(0xfff4ec, 0xb9a89a, 1.6));
  const sun = new THREE.DirectionalLight(0xffe2b8, 1.6); sun.position.set(6, 6, -3); lights.add(sun);
  const c = b.getCenter(new THREE.Vector3());
  return {
    group, name: 'classroom', title: 'Classroom — a memory-simulation module',
    bg: 0x9fb7d8, fog: null,
    cam: { pos: [b.min.x + 1.2, 1.6, b.max.z - 1.2], look: [0, 1.1, 0], fov: 55 },
    anchors: { front: [0, 0, 0, 0] },
    tint: 0xfff6ee, bright: 1.0,
    box: b,
    update() {},
  };
}

// ---------------------------------------------------------------------------
export const BUILDERS = {
  store: buildStore,
  apartment: buildApartment,
  classroom: buildClassroom,
};
