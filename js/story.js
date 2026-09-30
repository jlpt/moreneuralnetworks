// ---------------------------------------------------------------------------
// STORY SCRIPT — this file is what changes as the roleplay goes on.
//
// A chapter is a list of beats, played top to bottom. Every beat is a plain object;
// state-changing keys apply instantly, and a line of dialogue (say / n / you) pauses
// until the player clicks.
//
//   scene:'store'|'apartment'|'classroom'   switch location (fades)
//   title:'..', subtitle:'..'               full-screen title card
//   place:'pod' | [x,y,z,rotY]              put Chizuru somewhere (scene anchors or coords)
//   walk:'sofa' | [x,y,z,rotY]              make her walk there (walkDur seconds)
//   pose:'stand'|'clasp'|'standby'|'wave'|'bow'|'crossed'|'hips'|'cheeks'|'behind'
//        |'reach'|'chin'|'point'|'sit'
//   mood:'neutral'|'happy'|'shy'|'angry'|'sad'|'shock'|'cold'|'sleepy'   (LED colour, blush, head tilt)
//   look:'camera'|[x,y,z]|'none'            where her eyes/head point
//   emote:'heart'|'sparkle'|'star'|'exclaim'|'question'|'dots'|'music'|'anger'|'sweat'|'zzz'|'flower'
//   fx:'scan'|'boot'|'glitch'|'flash'       android effects       sfx:'chime'|'buy'|'door'|...
//   cam:'face'|'close'|'bust'|'medium'|'full'|'low'|'high'|'side'|'pov'|'behind'|'scene'
//       or { shot:'close', off:[x,y,z] } or { pos:[..], look:[..], fov }     (cut:true = no tween)
//   hud:{ credits, rapport, time, show }    rapport:+5 (relative)   pay:1980000
//   say:'..' (who:'Chizuru' by default)     n:'narration'           you:'your line'
//   choice:[{ t:'label', then:[beats] }]
//   wait: seconds                           toast:'system notification'
//
// Chapter end: turn:{ prompt, ideas } hands control back to the player.
// ---------------------------------------------------------------------------

export const meta = {
  startScene: 'store',
  startCredits: 3400000,
  startRapport: 0,
};

export const chapters = [
  {
    id: 'ch1',
    title: 'REPLICA, Shibuya',
    beats: [
      { scene: 'store', hud: { time: 'SEP 30, 2087 · 23:14', show: true }, place: 'pod', pose: 'standby', mood: 'sleepy', look: 'none', cam: 'scene', cut: true,
        title: 'NEO-SHIBUYA, 2087', subtitle: 'Loneliness is now a product category.' },
      { n: 'Rain hisses against the glass storefront. Inside REPLICA’s flagship showroom the air smells faintly of ozone and vanilla.' },
      { n: 'Rows of pods glow along the walls, each holding a motionless figure. Most are generic models. One, at the very centre, is lit like a shrine.', cam: { shot: 'full' }, camDur: 2.4 },
      { who: 'AYA', say: 'Welcome to REPLICA, valued guest. I am AYA, your concierge. You have been standing before Unit MZ-01 for four minutes and eleven seconds. Shall I take that as interest?' },
      { who: 'AYA', say: 'Unit MZ-01, designation “Chizuru”. Our most requested Companion Series model — engineered for professional, polished, *entirely believable* affection.', fx: 'scan', cam: { shot: 'medium' }, toast: 'SCANNING: MZ-01 · ALL SYSTEMS NOMINAL' },
      { who: 'AYA', say: 'She was tuned from thousands of hours of rental-companion behavior. Polite in public. Composed under pressure. And, our engineers note, occasionally… stubborn.' },
      { who: 'AYA', say: 'She is currently in standby. The price is ¥1,980,000, with a five-year heart-care warranty. I can wake her for a short demonstration first, or proceed directly to purchase.', cam: { shot: 'bust' } },
      { n: 'Behind the glass, the android’s head is bowed, brown hair falling over her shoulders. A faint cyan light pulses at her temple, slow and steady, like a heartbeat.' },
    ],
    turn: {
      prompt: 'AYA is waiting for your answer. What do you say or do? Tell me in the Claude chat and I’ll update the scene.',
      ideas: ['Ask AYA to wake her up for a demonstration', 'Ask about the price / what “heart-care” covers', 'Just buy her — ¥1,980,000, no questions', 'Walk around and look at the other models first'],
    },
  },
];
