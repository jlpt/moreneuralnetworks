import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

// ---------------------------------------------------------------------------
// Pose library.
// Each entry is a per-bone rotation *delta* [x, y, z] in radians, expressed in the
// character's own frame (she faces +Z, +X is her left, +Y is up) and applied on top of
// the parent bone's result. The rig ships in a T-pose, so "arms down" is a Z rotation.
// ---------------------------------------------------------------------------
const B = {
  // arms hanging relaxed
  stand: {
    aL: [0.05, 0, -1.32], eL: [-0.2, -0.1, 0], aR: [0.05, 0, 1.32], eR: [-0.2, 0.1, 0],
  },
};

export const POSES = {
  stand: B.stand,
  // polite, hands clasped low in front (her "professional rental girlfriend" stance)
  clasp: {
    aL: [-0.1, 0, -1.28], eL: [-1.45, -0.75, 0], wL: [0, 0, 0],
    aR: [-0.1, 0, 1.28], eR: [-1.45, 0.75, 0],
    head: [0.02, 0, 0],
  },
  // android "standby": limp, head lowered
  standby: {
    aL: [0.08, 0, -1.3], eL: [-0.1, 0, 0], aR: [0.08, 0, 1.3], eR: [-0.1, 0, 0],
    head: [0.42, 0, 0], neck: [0.15, 0, 0], spine: [0.05, 0, 0],
  },
  wave: {
    aL: [0.05, 0, -1.2], eL: [-0.22, 0, 0],
    aR: [-0.4, 0, 0.3], eR: [0, 0, -1.75], wave: 'R',
  },
  bow: {
    aL: [-0.05, 0, -1.3], eL: [-1.35, -0.6, 0], aR: [-0.05, 0, 1.3], eR: [-1.35, 0.6, 0],
    spine: [0.5, 0, 0], chest: [0.28, 0, 0], neck: [0.1, 0, 0], head: [0.1, 0, 0],
  },
  crossed: {
    aL: [-0.35, 0, -1.15], eL: [-1.45, -1.35, 0.0],
    aR: [-0.35, 0, 1.15], eR: [-1.45, 1.35, 0.0],
    head: [0, 0, 0.04],
  },
  hips: {
    aL: [0.1, 0, -0.5], eL: [-0.5, 0, -1.75], wL: [0, 0, 0.2],
    aR: [0.1, 0, 0.5], eR: [-0.5, 0, 1.75], wR: [0, 0, -0.2],
    spine: [0, 0.0, 0.03], hips: [0, 0, 0.0],
  },
  cheeks: {
    aL: [-0.3, 0, -1.15], eL: [-2.4, -1.0, 0],
    aR: [-0.3, 0, 1.15], eR: [-2.4, 1.0, 0],
    head: [0.1, 0, 0], spine: [0.05, 0, 0],
  },
  behind: {
    aL: [0.55, 0, -1.05], eL: [0.35, 0.65, 0], aR: [0.55, 0, 1.05], eR: [0.35, -0.65, 0],
    spine: [-0.05, 0, 0], chest: [-0.05, 0, 0],
  },
  reach: {
    aL: [0.05, 0, -1.2], eL: [-0.22, 0, 0],
    aR: [0.3, 1.35, 0], eR: [-0.4, 0.15, 0], wR: [0, 0, 0.0],
  },
  chin: {
    aL: [-0.35, 0, -1.2], eL: [-1.3, -0.9, 0],
    aR: [-0.5, 0, 1.1], eR: [-2.45, 1.45, 0],
    head: [0.05, 0, -0.08],
  },
  point: {
    aL: [0.05, 0, -1.2], eL: [-0.22, 0, 0],
    aR: [0.05, 1.4, 0], eR: [0, 0, 0],
  },
  // arms opening / closing for a hug
  hugopen: {
    aL: [0.25, -1.3, 0], eL: [-0.35, -0.3, 0], aR: [0.25, 1.3, 0], eR: [-0.35, 0.3, 0],
    head: [0.05, 0, 0.06],
  },
  hug: {
    aL: [0.3, -1.05, 0], eL: [-0.9, -1.25, 0], aR: [0.3, 1.05, 0], eR: [-0.9, 1.25, 0],
    head: [0.12, 0, 0.1], spine: [0.06, 0, 0],
  },
  // eating: right hand (fork) up to the mouth, left hand under the plate
  eat: {
    aL: [-0.3, 0, -1.2], eL: [-1.4, -0.7, 0],
    aR: [-0.3, 0, 1.15], eR: [-2.4, 1.0, 0],
    head: [0.06, 0, 0.05], spine: [0.04, 0, 0],
  },
  // holding something overhead with both hands
  lift: {
    aL: [0, 0, 0.95], eL: [0, 0, 0.45], aR: [0, 0, -0.95], eR: [0, 0, -0.45],
    spine: [-0.05, 0, 0], head: [-0.2, 0, 0],
  },
  // one hand overhead (right), left hand on hip
  lift1: {
    aR: [0, 0, -1.4], eR: [0, 0, -0.15], wR: [0, 0, 0],
    aL: [0.1, 0, -0.5], eL: [-0.5, 0, -1.75], wL: [0, 0, 0.2],
    spine: [0, 0, 0.06], head: [-0.15, 0, 0.05],
  },
  // arms forward, bracing to pick something up
  brace: {
    aL: [0.75, -0.55, 0], eL: [-0.6, -0.4, 0], aR: [0.75, 0.55, 0], eR: [-0.6, 0.4, 0],
    spine: [0.35, 0, 0], head: [-0.1, 0, 0],
  },
  // side splits, upright torso, hands on hips
  splits: {
    drop: 0.8,
    legL: [0, 0, 1.53], legR: [0, 0, -1.53], ankL: [0, 0, -1.2], ankR: [0, 0, 1.2],
    aL: [0.1, 0, -0.5], eL: [-0.5, 0, -1.75], wL: [0, 0, 0.2],
    aR: [0.1, 0, 0.5], eR: [-0.5, 0, 1.75], wR: [0, 0, -0.2],
  },
  // side splits while holding something overhead with both hands
  splitlift: {
    drop: 0.8,
    legL: [0, 0, 1.53], legR: [0, 0, -1.53], ankL: [0, 0, -1.2], ankR: [0, 0, 1.2],
    aL: [0, 0, 0.95], eL: [0, 0, 0.45], aR: [0, 0, -0.95], eR: [0, 0, -0.45],
    head: [-0.2, 0, 0],
  },
  // dance: arms pumping, hips swinging (animated in update via poseDef.dance)
  dance: {
    dance: 'pop',
    aL: [0.1, 0, -0.25], eL: [-0.3, 0, 0.9], aR: [0.1, 0, 0.25], eR: [-0.3, 0, -0.9],
    head: [0.03, 0, 0],
  },
  dance2: {
    dance: 'sway',
    aL: [0, 0, 0.95], eL: [0, 0, 0.45], aR: [0, 0, -0.95], eR: [0, 0, -0.45],
    head: [-0.1, 0, 0],
  },
  // mid-air tuck for flips
  tuck: {
    legL: [-1.5, 0, 0.05], kneeL: [2.1, 0, 0], legR: [-1.5, 0, -0.05], kneeR: [2.1, 0, 0], ankL: [0.3, 0, 0], ankR: [0.3, 0, 0],
    aL: [-0.6, 0, -0.9], eL: [-1.6, -0.9, 0], aR: [-0.6, 0, 0.9], eR: [-1.6, 0.9, 0],
    spine: [0.35, 0, 0], head: [0.2, 0, 0],
  },
  // coiled just before a jump
  crouch: {
    drop: 0.28,
    legL: [-0.75, 0, 0.1], kneeL: [1.5, 0, 0], legR: [-0.75, 0, -0.1], kneeR: [1.5, 0, 0], ankL: [0.3, 0, 0], ankR: [0.3, 0, 0],
    aL: [0.9, 0, -0.35], eL: [-0.3, 0, 0], aR: [0.9, 0, 0.35], eR: [-0.3, 0, 0],
    spine: [0.35, 0, 0], head: [-0.2, 0, 0],
  },
  // seated, drowsy, head tipped to one side
  sitnap: {
    drop: 0.26,
    legL: [-1.5, 0.08, 0.0], kneeL: [1.5, 0, 0], ankL: [0.15, 0, 0],
    legR: [-1.5, -0.08, 0.0], kneeR: [1.5, 0, 0], ankR: [0.15, 0, 0],
    aL: [-0.25, 0, -1.2], eL: [-1.15, -0.85, 0],
    aR: [-0.25, 0, 1.2], eR: [-1.15, 0.85, 0],
    spine: [0.05, 0, 0.04], head: [0.3, 0, 0.25], neck: [0.1, 0, 0.08],
  },
  // seated on a sofa / chair, hands resting on lap
  sit: {
    drop: 0.26,
    hips: [0, 0, 0],
    legL: [-1.5, 0.08, 0.0], kneeL: [1.5, 0, 0], ankL: [0.15, 0, 0],
    legR: [-1.5, -0.08, 0.0], kneeR: [1.5, 0, 0], ankR: [0.15, 0, 0],
    aL: [-0.25, 0, -1.2], eL: [-1.15, -0.85, 0],
    aR: [-0.25, 0, 1.2], eR: [-1.15, 0.85, 0],
    spine: [-0.08, 0, 0],
  },
};

// Per-bone Euler orders (arms/legs swing about Z then Y then X, everything else X then Y).
const ORDER = { aL: 'XYZ', aR: 'XYZ', legL: 'XYZ', legR: 'XYZ' };

// Bone name prefixes. (The glTF exporter turned spaces into underscores.)
const BONES = {
  hips: 'Hips_', spine: 'Spine_', chest: 'Chest_', neck: 'Neck_', head: 'Head_',
  eyeL: 'Eye_L_0', eyeR: 'Eye_R_0',
  sL: 'Left_shoulder_', aL: 'Left_arm_', eL: 'Left_elbow_', wL: 'Left_wrist_',
  sR: 'Right_shoulder_', aR: 'Right_arm_', eR: 'Right_elbow_', wR: 'Right_wrist_',
  legL: 'Left_leg_', kneeL: 'Left_knee_', ankL: 'Left_ankle_',
  legR: 'Right_leg_', kneeR: 'Right_knee_', ankR: 'Right_ankle_',
};
// top-down evaluation order
const CHAIN = ['hips', 'spine', 'chest', 'neck', 'head', 'sL', 'aL', 'eL', 'wL', 'sR', 'aR', 'eR', 'wR', 'legL', 'kneeL', 'ankL', 'legR', 'kneeR', 'ankR'];

const MOODS = {
  neutral: { led: 0x66e0ff, blush: 0, tilt: [0, 0, 0] },
  happy: { led: 0xffd0e8, blush: 0.25, tilt: [0, 0, 0.08] },
  shy: { led: 0xff8fc7, blush: 1, tilt: [0.18, 0, 0.1], away: 0.5 },
  angry: { led: 0xff4b4b, blush: 0.15, tilt: [0.05, 0, -0.05] },
  sad: { led: 0x6a8cff, blush: 0, tilt: [0.28, 0, 0.05] },
  shock: { led: 0xfff06a, blush: 0, tilt: [-0.08, 0, 0] },
  cold: { led: 0x9fd8ff, blush: 0, tilt: [-0.05, 0, 0] },
  sleepy: { led: 0x8a7bff, blush: 0.1, tilt: [0.25, 0, 0.1] },
};

const _q = new THREE.Quaternion();
const _q2 = new THREE.Quaternion();
const _e = new THREE.Euler();
const _v = new THREE.Vector3();
const _rootQ = new THREE.Quaternion();
const _rootQi = new THREE.Quaternion();
const _pw = new THREE.Quaternion();
const _pwi = new THREE.Quaternion();

export class Chizuru {
  constructor() {
    this.group = new THREE.Group(); // world placement (position + yaw)
    this.group.name = 'ChizuruRoot';
    this.model = null;
    this.bones = {};
    this.rest = {};        // rest local quaternions
    this.cur = {};         // current smoothed delta per bone key
    this.target = {};      // target delta per bone key
    this.poseName = 'stand';
    this.poseDef = POSES.stand;
    this.drop = 0;         // seated drop (smoothed)
    this.dropTarget = 0;
    this.lookTarget = 'camera';
    this.moodName = 'neutral';
    this.talking = false;
    this.time = Math.random() * 10;
    this.blush = 0;
    this.blushTarget = 0;
    this.moving = 0;       // 0..1 walking blend
    this.walk = null;
    this.headYaw = 0; this.headPitch = 0;
    this.eyeYaw = 0; this.eyePitch = 0;
    this.ledColor = new THREE.Color(0x66e0ff);
    this.ledTarget = new THREE.Color(0x66e0ff);
    this.androidParts = new THREE.Group();
    this.flash = 0;
    this.bright = 1;
    this.tintColor = new THREE.Color(1, 1, 1);
  }

  async load(url, onProgress) {
    const gltf = await new GLTFLoader().loadAsync(url, onProgress);
    this.model = gltf.scene;
    // pivot at hip height so we can rotate her for flips; the model hangs below it
    this.pivot = new THREE.Group();
    this.group.add(this.pivot);
    this.pivot.add(this.model);
    this.model.traverse((o) => {
      if (o.isBone) for (const k in BONES) if (!this.bones[k] && o.name.startsWith(BONES[k])) this.bones[k] = o;
      if (o.isSkinnedMesh) {
        o.frustumCulled = false;
        const m = o.material;
        m.toneMapped = false;
        // the export marks everything as alpha-blended, which sorts badly; use cut-outs instead
        m.transparent = false; m.alphaTest = 0.3; m.depthWrite = true;
        if (m.name === 'eyeline') o.renderOrder = 3;
        if (m.name === 'mouth') o.renderOrder = 4;
        if (m.name === 'material') o.renderOrder = 2;
        (this.mats ||= []).push(m);
      }
    });
    this.model.updateMatrixWorld(true);
    for (const k of CHAIN) {
      const b = this.bones[k];
      if (!b) { console.warn('missing bone', k); continue; }
      this.rest[k] = b.quaternion.clone();
      this.cur[k] = [0, 0, 0];
      this.target[k] = [0, 0, 0];
    }
    this.hipsRestZ = this.bones.hips.position.z; // the pelvis bone's local Z is world-up
    this.group.updateMatrixWorld(true);
    this.pivotY = this.bones.hips.getWorldPosition(new THREE.Vector3()).y;
    this.pivot.position.y = this.pivotY; this.model.position.y = -this.pivotY;
    this.buildFace();
    this.buildAndroidParts();
    this.setPose('stand', true);
    return this;
  }

  // Overlay planes for the animated mouth + blush, parented to the head bone.
  buildFace() {
    const head = this.bones.head;
    const mk = (draw, w, h, pos) => {
      const c = document.createElement('canvas'); c.width = 128; c.height = 64;
      draw(c.getContext('2d'));
      const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace;
      const m = new THREE.MeshBasicMaterial({ map: t, transparent: true, opacity: 0, depthWrite: false, toneMapped: false });
      const p = new THREE.Mesh(new THREE.PlaneGeometry(w, h), m);
      p.position.set(...pos); p.renderOrder = 6;
      head.add(p);
      return p;
    };
    const F = this.faceCfg = { z: 0.108, mouthY: -0.0145, cheekY: 0.018, cheekX: 0.046 };
    this.mouth = mk((g) => {
      g.fillStyle = '#7a2436';
      g.beginPath(); g.ellipse(64, 32, 50, 26, 0, 0, 7); g.fill();
      g.fillStyle = '#ff7f96';
      g.beginPath(); g.ellipse(64, 46, 30, 12, 0, 0, 7); g.fill();
      g.fillStyle = '#fff';
      g.beginPath(); g.ellipse(64, 15, 34, 7, 0, 0, 7); g.fill();
    }, 0.026, 0.02, [0, F.mouthY, F.z]);
    this.mouth.material.opacity = 0;
    const blushTex = (g) => {
      const gr = g.createRadialGradient(64, 32, 2, 64, 32, 60);
      gr.addColorStop(0, 'rgba(255,90,120,0.85)'); gr.addColorStop(1, 'rgba(255,90,120,0)');
      g.fillStyle = gr; g.fillRect(0, 0, 128, 64);
    };
    this.blushL = mk(blushTex, 0.05, 0.026, [F.cheekX, F.cheekY, F.z - 0.004]);
    this.blushR = mk(blushTex, 0.05, 0.026, [-F.cheekX, F.cheekY, F.z - 0.004]);
    this.blushL.rotation.y = 0.25; this.blushR.rotation.y = -0.25;
  }

  // Cyan "sensor" ear-pieces, a halo ring and a neck LED so she reads as an android.
  buildAndroidParts() {
    const head = this.bones.head;
    this.ledMat = new THREE.MeshBasicMaterial({ color: 0x66e0ff, toneMapped: false });
    const ear = (side) => {
      const g = new THREE.Group();
      const shell = new THREE.Mesh(new THREE.CapsuleGeometry(0.0095, 0.03, 4, 12), new THREE.MeshStandardMaterial({ color: 0xf2f5fa, metalness: 0.6, roughness: 0.25 }));
      const glow = new THREE.Mesh(new THREE.CapsuleGeometry(0.0055, 0.03, 4, 12), this.ledMat);
      glow.position.x = side * 0.007;
      g.add(shell, glow);
      g.position.set(side * 0.079, 0.046, -0.003);
      g.rotation.z = side * 0.05;
      return g;
    };
    this.earL = ear(1); this.earR = ear(-1);
    head.add(this.earL, this.earR);

    this.haloMat = new THREE.MeshBasicMaterial({ color: 0x66e0ff, transparent: true, opacity: 0.85, toneMapped: false, side: THREE.DoubleSide });
    this.halo = new THREE.Mesh(new THREE.TorusGeometry(0.115, 0.0035, 8, 64), this.haloMat);
    this.halo.position.set(0, 0.155, -0.02);
    this.halo.rotation.x = Math.PI / 2 - 0.25;
    head.add(this.halo);
    // scan ring (used by the `fx: 'scan'` beat)
    this.scanRing = new THREE.Mesh(new THREE.TorusGeometry(0.34, 0.006, 8, 64), new THREE.MeshBasicMaterial({ color: 0x66e0ff, transparent: true, opacity: 0, toneMapped: false }));
    this.scanRing.rotation.x = Math.PI / 2;
    this.group.add(this.scanRing);
    this.scan = 0;
    this.haloOn = true;
  }

  setAndroid(on) {
    this.haloOn = on;
    for (const o of [this.earL, this.earR, this.halo]) o.visible = on;
  }

  // -------------------------------------------------------------------------
  setPose(name, instant = false) {
    const def = POSES[name] || POSES.stand;
    this.poseName = name; this.poseDef = def;
    for (const k of CHAIN) this.target[k] = (def[k] || [0, 0, 0]).slice();
    // the base "arms hang down" pose is used when a pose doesn't specify arms
    for (const k of ['aL', 'aR', 'eL', 'eR']) if (!def[k]) this.target[k] = POSES.stand[k].slice();
    this.dropTarget = def.drop || 0;
    if (instant) { for (const k of CHAIN) this.cur[k] = this.target[k].slice(); this.drop = this.dropTarget; }
  }

  setMood(name) {
    const m = MOODS[name] || MOODS.neutral;
    this.moodName = name in MOODS ? name : 'neutral';
    this.ledTarget.setHex(m.led);
    this.blushTarget = m.blush;
  }

  setLook(t) { this.lookTarget = t; }
  setTalking(v) { this.talking = v; }
  setTint(c, bright = 1) { this.tintColor.set(c); this.bright = bright; }

  place(x, y, z, ry) {
    this.group.position.set(x, y, z);
    if (ry !== undefined) this.group.rotation.y = ry;
    this.walk = null;
  }

  walkTo(x, y, z, dur = 2, ry) {
    this.walk = { from: this.group.position.clone(), to: new THREE.Vector3(x, y, z), t: 0, dur, ry, fromRy: this.group.rotation.y };
  }

  faceYaw(ry) { this.group.rotation.y = ry; }

  headWorld(out = new THREE.Vector3()) { return this.bones.head.getWorldPosition(out); }

  // -------------------------------------------------------------------------
  update(dt, camera, lookPoint) {
    this.time += dt;
    const t = this.time;
    const k = 1 - Math.exp(-dt * 7);

    // walking
    let walkPhase = 0;
    if (this.walk) {
      const w = this.walk;
      w.t += dt;
      const u = Math.min(1, w.t / w.dur);
      const e = u * u * (3 - 2 * u);
      this.group.position.lerpVectors(w.from, w.to, e);
      const dir = _v.subVectors(w.to, w.from);
      if (dir.lengthSq() > 1e-4 && u < 1) {
        const want = Math.atan2(dir.x, dir.z);
        let d = want - this.group.rotation.y; d = Math.atan2(Math.sin(d), Math.cos(d));
        this.group.rotation.y += d * (1 - Math.exp(-dt * 8));
      }
      walkPhase = w.t * 7.5;
      this.moving += ((u < 1 ? 1 : 0) - this.moving) * (1 - Math.exp(-dt * 8));
      if (u >= 1) { if (w.ry !== undefined) { this.walk = { ...w, from: w.to.clone(), t: 0, dur: 0.001, faceOnly: true }; } else this.walk = null; }
    } else this.moving += (0 - this.moving) * (1 - Math.exp(-dt * 8));

    // flip animation (runs on the pivot at her hips)
    if (this.flip) {
      const f = this.flip; f.t += dt;
      if (f.t < 0) { if (f.phase < 1) { this.setPose('crouch'); f.phase = 1; } this.pivot.rotation.x = 0; this.pivot.position.y = this.pivotY; }
      else {
        const u = Math.min(1, f.t / f.dur);
        if (f.phase < 2) { this.setPose('tuck'); f.phase = 2; }
        const e = u < 0.1 ? 0 : u > 0.9 ? 1 : (u - 0.1) / 0.8; // hold upright briefly at take-off and landing
        this.pivot.rotation.x = f.dir * Math.PI * 2 * f.turns * (e * e * (3 - 2 * e) * 0.35 + e * 0.65);
        this.pivot.position.y = this.pivotY + f.height * 4 * u * (1 - u);
        if (u > 0.82 && f.phase < 3) { this.setPose('crouch'); f.phase = 3; }
        if (u >= 1) { this.pivot.rotation.x = 0; this.pivot.position.y = this.pivotY; this.setPose(f.prev === 'tuck' || f.prev === 'crouch' ? 'stand' : f.prev); this.flip = null; this.flash = 0.5; }
      }
    }
    // smooth pose
    for (const key of CHAIN) {
      const c = this.cur[key], tg = this.target[key];
      for (let i = 0; i < 3; i++) c[i] += (tg[i] - c[i]) * k;
    }
    this.drop += (this.dropTarget - this.drop) * k;

    // procedural overlays (breathing, sway, talking, walking, wave)
    const ov = {};
    const add = (key, x, y, z) => { const o = (ov[key] ||= [0, 0, 0]); o[0] += x; o[1] += y; o[2] += z; };
    const breathe = Math.sin(t * 1.9);
    add('chest', breathe * 0.012, 0, 0);
    add('spine', 0, Math.sin(t * 0.55) * 0.02, Math.sin(t * 0.7) * 0.012);
    add('head', Math.sin(t * 0.8) * 0.01, 0, Math.sin(t * 0.6) * 0.012);
    add('aL', 0, 0, breathe * 0.008); add('aR', 0, 0, -breathe * 0.008);
    this.bobOff = 0;
    if (this.poseDef.dance) {
      this.danceT = (this.danceT || 0) + dt * 4 * Math.PI; // 2 beats per second
      const p = this.danceT, s = Math.sin(p / 2), b = Math.sin(p);
      this.bobOff = (1 - Math.cos(p)) * 0.022;
      add('hips', 0, s * 0.3, b * 0.05);
      add('spine', 0, -s * 0.2, -b * 0.04);
      add('head', b * 0.05, -s * 0.12, s * 0.08);
      add('kneeL', Math.max(0, s) * 0.55 + 0.12, 0, 0); add('kneeR', Math.max(0, -s) * 0.55 + 0.12, 0, 0);
      add('legL', -Math.max(0, s) * 0.3 - 0.06, 0, 0); add('legR', -Math.max(0, -s) * 0.3 - 0.06, 0, 0);
      if (this.poseDef.dance === 'pop') { add('aL', 0, 0, s * 0.75); add('aR', 0, 0, s * 0.75); add('eL', 0, 0, -b * 0.25); add('eR', 0, 0, b * 0.25); }
      else { add('aL', 0, 0, -s * 0.35); add('aR', 0, 0, -s * 0.35); add('eL', 0, 0, b * 0.2); add('eR', 0, 0, -b * 0.2); }
    }
    if (this.poseDef.wave) {
      const w = Math.sin(t * 9);
      add(this.poseDef.wave === 'R' ? 'eR' : 'eL', 0, 0, (this.poseDef.wave === 'R' ? -1 : 1) * w * 0.35);
    }
    if (this.talking) {
      add('head', Math.sin(t * 5.3) * 0.03, Math.sin(t * 2.9) * 0.04, 0);
      add('spine', Math.sin(t * 3.1) * 0.012, 0, 0);
    }
    if (this.moving > 0.01) {
      const m = this.moving;
      const s = Math.sin(walkPhase);
      add('legL', s * 0.55 * m, 0, 0); add('legR', -s * 0.55 * m, 0, 0);
      add('kneeL', Math.max(0, -s) * 0.7 * m, 0, 0); add('kneeR', Math.max(0, s) * 0.7 * m, 0, 0);
      add('aL', -s * 0.25 * m, 0, 0); add('aR', s * 0.25 * m, 0, 0);
      add('spine', 0, s * 0.06 * m, 0);
      add('hips', 0, -s * 0.05 * m, 0);
    }

    // mood tilt
    const mood = MOODS[this.moodName] || MOODS.neutral;
    add('head', mood.tilt[0], mood.tilt[1], mood.tilt[2]);
    if (this.moodName === 'shy') { add('spine', 0.05, 0, 0); }

    // look-at (head + eyes) — computed in the character's frame
    let yaw = 0, pitch = 0;
    if (this.lookTarget) {
      const tgt = (this.lookTarget === 'camera' || this.lookTarget === 'player') ? camera.position
        : Array.isArray(this.lookTarget) ? _v.set(...this.lookTarget) : this.lookTarget;
      const hp = this.headWorld(new THREE.Vector3());
      const d = new THREE.Vector3().subVectors(tgt, hp);
      this.pivot.getWorldQuaternion(_rootQ); _rootQi.copy(_rootQ).invert();
      d.applyQuaternion(_rootQi);
      yaw = Math.atan2(d.x, d.z);
      pitch = -Math.atan2(d.y, Math.hypot(d.x, d.z));
      // don't spin the head all the way round if the target is behind us
      if (Math.abs(yaw) > 2.2) { yaw = 0; pitch = 0; }
    }
    if (mood.away && this.lookTarget) { yaw += mood.away * 0.55; pitch += 0.15; }
    const ky = 1 - Math.exp(-dt * 5);
    this.headYaw += (THREE.MathUtils.clamp(yaw, -1.05, 1.05) - this.headYaw) * ky;
    this.headPitch += (THREE.MathUtils.clamp(pitch, -0.5, 0.55) - this.headPitch) * ky;
    add('neck', this.headPitch * 0.3, this.headYaw * 0.35, 0);
    add('head', this.headPitch * 0.55, this.headYaw * 0.55, 0);
    const eyeY = THREE.MathUtils.clamp(yaw - this.headYaw, -0.4, 0.4), eyeP = THREE.MathUtils.clamp(pitch - this.headPitch, -0.25, 0.25);
    this.eyeYaw += (eyeY - this.eyeYaw) * ky; this.eyePitch += (eyeP - this.eyePitch) * ky;

    // apply to bones (top-down so each parent's result is final before its children)
    this.pivot.getWorldQuaternion(_rootQ);
    const hips = this.bones.hips;
    hips.position.z = this.hipsRestZ - this.drop - (this.bobOff || 0);
    this.group.updateMatrixWorld(true);
    for (const key of CHAIN) {
      const b = this.bones[key]; if (!b) continue;
      const c = this.cur[key], o = ov[key] || [0, 0, 0];
      _e.set(c[0] + o[0], c[1] + o[1], c[2] + o[2], ORDER[key] || 'YXZ');
      _q.setFromEuler(_e);                                   // delta in character frame
      _q2.copy(_rootQ).multiply(_q).multiply(_rootQi.copy(_rootQ).invert()); // -> world frame
      b.parent.updateWorldMatrix(true, false);
      b.parent.getWorldQuaternion(_pw); _pwi.copy(_pw).invert();
      b.quaternion.copy(_pwi).multiply(_q2).multiply(_pw).multiply(this.rest[key]);
      b.updateMatrixWorld(true);
    }

    // eyes (rotate the little eye bones a touch)
    for (const [key, s] of [['eyeL', 1], ['eyeR', 1]]) {
      const b = this.bones[key]; if (!b) continue;
      if (!this.rest[key]) this.rest[key] = b.quaternion.clone();
      _e.set(this.eyePitch * 0.4, this.eyeYaw * 0.4 * s, 0, 'YXZ');
      _q.setFromEuler(_e);
      b.quaternion.copy(this.rest[key]).multiply(_q);
    }

    // face overlays
    const talkOpen = this.mouthForce != null ? this.mouthForce : this.talking ? (0.35 + 0.65 * Math.abs(Math.sin(t * 13) * Math.sin(t * 7.3))) : 0;
    this.mouthOpen = (this.mouthOpen || 0) + (talkOpen - (this.mouthOpen || 0)) * (1 - Math.exp(-dt * 25));
    this.mouth.material.opacity = Math.min(1, this.mouthOpen * 2.2);
    this.mouth.scale.set(0.7 + this.mouthOpen * 0.3, 0.25 + this.mouthOpen * 0.9, 1);
    this.blush += (this.blushTarget - this.blush) * (1 - Math.exp(-dt * 4));
    this.blushL.material.opacity = this.blushR.material.opacity = this.blush * 0.7;

    // android accents
    this.ledColor.lerp(this.ledTarget, 1 - Math.exp(-dt * 5));
    this.ledMat.color.copy(this.ledColor).multiplyScalar(1.6);
    this.haloMat.color.copy(this.ledColor).multiplyScalar(1.4);
    this.halo.rotation.z += dt * 0.6;
    this.halo.material.opacity = 0.55 + Math.sin(t * 2.2) * 0.2;
    if (this.scan > 0) {
      this.scan = Math.max(0, this.scan - dt / 2.2);
      const u = 1 - this.scan;
      this.scanRing.position.y = u * 1.65;
      this.scanRing.material.opacity = Math.sin(u * Math.PI) * 0.95;
      this.scanRing.scale.setScalar(1 + Math.sin(u * 6) * 0.05);
    } else this.scanRing.material.opacity = 0;

    // tint / brightness (scene lighting is faked because the toon materials are unlit)
    this.flash = Math.max(0, this.flash - dt * 2.5);
    const br = this.bright + this.flash * 0.6;
    for (const m of this.mats) m.color.copy(this.tintColor).multiplyScalar(br);
  }

  // -------------------------------------------------------------------------
  // Appearance profiles. The model is texture-based, so we recolour the source
  // textures in a canvas: shirt/ribbon hue shift, hair colour, and body scale.
  //   style: { shirt: hueShiftDegrees, hair: 'default' | {h:0-360, s:0-1, l:0-1}, scale: 1 }
  setStyle(style = {}) {
    if (!this.styleSrc) {
      const grab = (name) => {
        const m = this.mats.find((x) => x.name === name);
        const img = m.map.image;
        const c = document.createElement('canvas'); c.width = img.width; c.height = img.height;
        const g = c.getContext('2d', { willReadFrequently: true });
        g.drawImage(img, 0, 0);
        return { g, w: c.width, h: c.height, data: g.getImageData(0, 0, c.width, c.height), canvas: c, mats: this.mats.filter((x) => x.map && x.map.image === img) };
      };
      this.styleSrc = { cloth: grab('cloth'), hair: grab('backhair') };
      this.styleSrc.hair.mats = this.mats.filter((x) => x.name === 'hair' || x.name === 'backhair');
    }
    const S = this.styleSrc;
    const shift = style.shirt || 0;
    const hair = style.hair && style.hair !== 'default' ? style.hair : null;
    const recolor = (src, fn) => {
      const out = new ImageData(new Uint8ClampedArray(src.data.data), src.w, src.h);
      const d = out.data;
      for (let i = 0; i < d.length; i += 4) {
        if (d[i + 3] < 8) continue;
        const r = d[i] / 255, g = d[i + 1] / 255, b = d[i + 2] / 255;
        const mx = Math.max(r, g, b), mn = Math.min(r, g, b), l = (mx + mn) / 2, dl = mx - mn;
        let h = 0, s = 0;
        if (dl > 1e-5) {
          s = dl / (1 - Math.abs(2 * l - 1));
          h = mx === r ? ((g - b) / dl + (g < b ? 6 : 0)) : mx === g ? (b - r) / dl + 2 : (r - g) / dl + 4;
          h *= 60;
        }
        const res = fn(h, s, l);
        if (!res) continue;
        const [nh, ns, nl] = res;
        const c = (1 - Math.abs(2 * nl - 1)) * ns, x = c * (1 - Math.abs(((nh / 60) % 2) - 1)), m = nl - c / 2;
        const k = Math.floor((((nh % 360) + 360) % 360) / 60);
        const rgb = [[c, x, 0], [x, c, 0], [0, c, x], [0, x, c], [x, 0, c], [c, 0, x]][k];
        d[i] = (rgb[0] + m) * 255; d[i + 1] = (rgb[1] + m) * 255; d[i + 2] = (rgb[2] + m) * 255;
      }
      src.g.putImageData(out, 0, 0);
    };
    const swap = (src) => {
      const tex = new THREE.CanvasTexture(src.canvas);
      tex.flipY = false; tex.colorSpace = THREE.SRGBColorSpace; tex.anisotropy = 8;
      tex.wrapS = tex.wrapT = THREE.RepeatWrapping;
      for (const m of src.mats) { m.map = tex; m.needsUpdate = true; }
    };
    // shirt + ribbon are the saturated red/pink pixels; skin and skirt are left alone
    recolor(S.cloth, (h, s, l) => ((h >= 335 || h <= 12) && s > 0.3 ? [h + shift, s, l] : null));
    swap(S.cloth);
    if (hair) recolor(S.hair, (h, s, l) => [hair.h, Math.min(1, s * (hair.s ?? 1) + (hair.sAdd ?? 0)), Math.min(0.95, l * (hair.l ?? 1) + (hair.lAdd ?? 0))]);
    else S.hair.g.putImageData(S.hair.data, 0, 0);
    swap(S.hair);
    this.group.scale.setScalar(style.scale || 1);
    this.glitch();
  }

  // Backflip(s). turns: 1 = single, 2 = double. dir -1 = backwards, +1 = forwards.
  startFlip({ dur = 1.15, height = 0.95, turns = 1, dir = -1 } = {}) {
    this.flip = { t: -0.35, dur, height, turns, dir, prev: this.poseName, phase: 0 };
  }

  startScan() { this.scan = 1; }
  glitch() { this.flash = 1; }
}
