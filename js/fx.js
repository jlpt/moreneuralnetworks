import * as THREE from 'three';

// Draws a small glyph (heart, sparkle, "!" ...) to a canvas and returns a texture.
const cache = {};
function glyphTexture(kind) {
  if (cache[kind]) return cache[kind];
  const c = document.createElement('canvas');
  c.width = c.height = 128;
  const g = c.getContext('2d');
  g.translate(64, 64);
  g.lineJoin = 'round'; g.lineCap = 'round';
  const heart = (fill, stroke) => {
    g.beginPath();
    g.moveTo(0, 38);
    g.bezierCurveTo(-64, -6, -34, -50, 0, -18);
    g.bezierCurveTo(34, -50, 64, -6, 0, 38);
    g.closePath();
    g.fillStyle = fill; g.fill(); g.lineWidth = 7; g.strokeStyle = stroke; g.stroke();
  };
  const star = (r, fill, stroke, n = 4, inner = 0.28) => {
    g.beginPath();
    for (let i = 0; i < n * 2; i++) {
      const a = (i / (n * 2)) * Math.PI * 2 - Math.PI / 2;
      const rr = i % 2 ? r * inner : r;
      g.lineTo(Math.cos(a) * rr, Math.sin(a) * rr);
    }
    g.closePath(); g.fillStyle = fill; g.fill();
    if (stroke) { g.lineWidth = 5; g.strokeStyle = stroke; g.stroke(); }
  };
  const text = (t, fill, stroke, size = 100) => {
    g.font = `900 ${size}px Rajdhani, "Trebuchet MS", sans-serif`;
    g.textAlign = 'center'; g.textBaseline = 'middle';
    g.lineWidth = 12; g.strokeStyle = stroke; g.strokeText(t, 0, 6);
    g.fillStyle = fill; g.fillText(t, 0, 6);
  };
  switch (kind) {
    case 'heart': heart('#ff6b9d', '#fff'); break;
    case 'sparkle': star(56, '#fff7b0', '#ffd24a'); break;
    case 'star': star(52, '#ffe066', '#fff', 5, 0.45); break;
    case 'exclaim': text('!', '#ffe14a', '#7a3d00'); break;
    case 'question': text('?', '#8fe3ff', '#0b3a55'); break;
    case 'dots': text('...', '#ffffff', '#33405a', 90); break;
    case 'music': text('♪', '#ffb3d9', '#6b1f4b'); break;
    case 'anger': {
      g.strokeStyle = '#ff3b3b'; g.lineWidth = 12;
      for (const s of [1, -1]) for (const t of [1, -1]) {
        g.beginPath(); g.moveTo(s * 10, t * 10); g.quadraticCurveTo(s * 36, t * 10, s * 40, t * 40); g.stroke();
      }
      break;
    }
    case 'sweat': {
      g.beginPath();
      g.moveTo(0, -50); g.bezierCurveTo(40, 0, 40, 48, 0, 50); g.bezierCurveTo(-40, 48, -40, 0, 0, -50);
      g.fillStyle = '#a8e6ff'; g.fill(); g.lineWidth = 6; g.strokeStyle = '#fff'; g.stroke();
      break;
    }
    case 'zzz': text('z', '#cfe8ff', '#28406a', 90); break;
    case 'flower': {
      g.fillStyle = '#ffb7d5';
      for (let i = 0; i < 5; i++) { g.save(); g.rotate(i * Math.PI * 2 / 5); g.beginPath(); g.ellipse(0, -26, 16, 26, 0, 0, 7); g.fill(); g.restore(); }
      g.fillStyle = '#ffe680'; g.beginPath(); g.arc(0, 0, 13, 0, 7); g.fill();
      break;
    }
    default: star(50, '#fff', null);
  }
  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  return (cache[kind] = tex);
}

// Floating "manga effect" glyphs that rise from a point and fade out.
export class Emotes {
  constructor(scene) {
    this.scene = scene;
    this.items = [];
  }

  spawn(kind, worldPos, { count = 1, size = 0.14, spread = 0.12, rise = 0.35, life = 1.6 } = {}) {
    const n = kind === 'heart' || kind === 'sparkle' || kind === 'flower' ? Math.max(count, 3) : count;
    for (let i = 0; i < n; i++) {
      const mat = new THREE.SpriteMaterial({ map: glyphTexture(kind), transparent: true, depthTest: false, depthWrite: false, toneMapped: false });
      const s = new THREE.Sprite(mat);
      s.renderOrder = 999;
      s.position.copy(worldPos).add(new THREE.Vector3((Math.random() - 0.5) * spread * 2, Math.random() * 0.06, (Math.random() - 0.5) * spread));
      const sz = size * (0.75 + Math.random() * 0.5);
      s.scale.setScalar(0.001);
      this.scene.add(s);
      this.items.push({ s, sz, age: -i * 0.12, life: life * (0.8 + Math.random() * 0.4), vy: rise * (0.7 + Math.random() * 0.6), vx: (Math.random() - 0.5) * 0.12 });
    }
  }

  update(dt) {
    for (let i = this.items.length - 1; i >= 0; i--) {
      const it = this.items[i];
      it.age += dt;
      if (it.age < 0) continue;
      const k = it.age / it.life;
      if (k >= 1) { this.scene.remove(it.s); it.s.material.dispose(); this.items.splice(i, 1); continue; }
      it.s.position.y += it.vy * dt * (1 - k * 0.6);
      it.s.position.x += it.vx * dt;
      const pop = k < 0.15 ? k / 0.15 : 1;
      it.s.scale.setScalar(it.sz * (0.4 + 0.6 * pop) * (1 + Math.sin(it.age * 6) * 0.05));
      it.s.material.opacity = k > 0.65 ? 1 - (k - 0.65) / 0.35 : 1;
    }
  }

  clear() {
    for (const it of this.items) { this.scene.remove(it.s); it.s.material.dispose(); }
    this.items.length = 0;
  }
}
