import { sfx } from './audio.js';

const $ = (id) => document.getElementById(id);

const SPEAKERS = {
  Chizuru: { color: '#ff9fcb' },
  You: { color: '#7df9ff' },
  AYA: { color: '#8ff0d8' },
  System: { color: '#ffe066' },
};

const esc = (s) => s.replace(/[&<>]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));
// *action text* -> italics
const fmt = (s) => esc(s).replace(/\*([^*]+)\*/g, '<i>$1</i>');

export class UI {
  constructor() {
    this.box = $('dialogue');
    this.name = $('speaker');
    this.text = $('text');
    this.caret = $('caret');
    this.choicesEl = $('choices');
    this.log = [];
    this.typing = false;
    this.skipRequested = false;
    this.advanceResolve = null;
    this.onSpeaker = () => {};
    this.onTalk = () => {};
    this.auto = false;

    const adv = (e) => {
      if (e && e.target.closest && e.target.closest('button, a, input, select, #topbar, #logpanel, #chapters, #turn')) return;
      this.advance();
    };
    $('stage').addEventListener('click', adv);
    window.addEventListener('keydown', (e) => {
      if (e.code === 'Space' || e.code === 'Enter') { e.preventDefault(); this.advance(); }
      if (e.key === 'l' || e.key === 'L') this.toggleLog();
    });
    $('btn-log').onclick = () => this.toggleLog();
    $('logclose').onclick = () => this.toggleLog(false);
  }

  advance() {
    if (this.typing) { this.skipRequested = true; return; }
    if (this.advanceResolve) { const r = this.advanceResolve; this.advanceResolve = null; this.caret.classList.remove('on'); sfx('next'); r(); }
  }

  toggleLog(force) {
    const p = $('logpanel');
    const show = force ?? !p.classList.contains('show');
    p.classList.toggle('show', show);
    if (show) {
      const body = $('logbody');
      body.innerHTML = this.log.map((l) => `<p><b style="color:${(SPEAKERS[l.who] || {}).color || '#cfd8ff'}">${esc(l.who || '')}</b> ${fmt(l.text)}</p>`).join('');
      body.scrollTop = body.scrollHeight;
    }
  }

  // Show a line and wait for the player to advance. who: speaker name or null (narration)
  async say(who, text, { narr = false } = {}) {
    this.box.classList.add('show');
    this.box.classList.toggle('narr', narr);
    const sp = SPEAKERS[who] || { color: '#cfd8ff' };
    if (narr) { this.name.textContent = ''; this.name.style.display = 'none'; }
    else { this.name.style.display = ''; this.name.textContent = who; this.name.style.setProperty('--c', sp.color); }
    this.box.style.setProperty('--c', sp.color);
    this.log.push({ who: narr ? '' : who, text });
    this.onSpeaker(narr ? null : who);
    this.text.innerHTML = '';
    this.typing = true; this.skipRequested = false;
    const html = fmt(text);
    // typewriter that respects tags
    const tokens = html.match(/<[^>]+>|[^<]/g) || [];
    let out = '', i = 0, n = 0;
    const talker = who === 'Chizuru';
    if (talker) this.onTalk(true);
    await new Promise((res) => {
      const step = () => {
        if (this.skipRequested) { this.text.innerHTML = html; res(); return; }
        const burst = 2;
        for (let k = 0; k < burst && i < tokens.length; k++) { const t = tokens[i++]; out += t; if (t[0] !== '<') { n++; if (n % 3 === 0 && !narr) sfx('blip'); } }
        // close open tags visually by letting the browser fix up
        this.text.innerHTML = out;
        if (i >= tokens.length) res(); else setTimeout(step, 18);
      };
      step();
    });
    this.typing = false;
    if (talker) this.onTalk(false);
    this.caret.classList.add('on');
    await new Promise((r) => { this.advanceResolve = r; });
  }

  async choose(options) {
    this.choicesEl.innerHTML = '';
    this.choicesEl.classList.add('show');
    return new Promise((resolve) => {
      options.forEach((o, i) => {
        const b = document.createElement('button');
        b.className = 'choice'; b.textContent = o.t;
        b.onclick = () => { sfx('choose'); this.choicesEl.classList.remove('show'); this.choicesEl.innerHTML = ''; this.log.push({ who: 'You', text: o.t }); resolve(i); };
        this.choicesEl.appendChild(b);
      });
    });
  }

  hideBox() { this.box.classList.remove('show'); this.onSpeaker(null); }

  // Big centered title card
  async card(title, sub, ms = 2600) {
    const el = $('card');
    el.innerHTML = `<h1>${esc(title)}</h1>${sub ? `<p>${esc(sub)}</p>` : ''}`;
    el.classList.add('show');
    await new Promise((r) => setTimeout(r, ms));
    el.classList.remove('show');
    await new Promise((r) => setTimeout(r, 600));
  }

  // Holographic system notification
  toast(html, ms = 3200) {
    const el = document.createElement('div');
    el.className = 'toast'; el.innerHTML = html;
    $('toasts').appendChild(el);
    requestAnimationFrame(() => el.classList.add('in'));
    setTimeout(() => { el.classList.remove('in'); setTimeout(() => el.remove(), 500); }, ms);
  }

  setLocation(t) { $('loc').textContent = t; }
  setTime(t) { $('clock').textContent = t; }
  setCredits(n) { $('credits').textContent = '¥ ' + Math.round(n).toLocaleString('en-US'); }
  setRapport(n) {
    n = Math.max(0, Math.min(100, n));
    $('rapportfill').style.width = n + '%';
    $('rapportnum').textContent = Math.round(n);
  }
  showHud(v) { $('hud').classList.toggle('show', v); }

  fade(on, ms = 700) {
    const f = $('fade');
    f.style.transitionDuration = ms + 'ms';
    f.classList.toggle('on', on);
    return new Promise((r) => setTimeout(r, ms));
  }

  // End of chapter: hand the turn back to the player
  showTurn(prompt, ideas = [], hasNext = false) {
    $('turn-next').style.display = hasNext ? '' : 'none';
    $('turn-prompt').textContent = prompt;
    $('turn-ideas').innerHTML = ideas.map((i) => `<li>${esc(i)}</li>`).join('');
    $('turn').classList.add('show');
  }
  hideTurn() { $('turn').classList.remove('show'); }
}
