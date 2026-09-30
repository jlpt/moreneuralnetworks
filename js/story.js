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
  {
    id: 'ch2',
    title: 'Purchase & activation',
    beats: [
      { scene: 'store', fade: false, place: 'pod', pose: 'standby', mood: 'sleepy', look: 'none', cam: { shot: 'medium' }, cut: true },
      { you: 'I want to buy her.', cam: 'pov' },
      { who: 'AYA', say: 'Wonderful. Most guests hesitate for at least ten minutes. You hesitated for none.' },
      { who: 'AYA', say: 'Processing payment: ¥1,980,000. Five-year heart-care warranty attached.', pay: 1980000, toast: 'PAYMENT ACCEPTED · ¥1,980,000' },
      { who: 'AYA', say: 'Ownership transferred. Beginning activation. Please stand back from the glass.', cam: { shot: 'bust' }, sfx: 'door' },
      { n: 'The pod’s glass slides away in a hiss of cold mist. The ring at her feet flares from pink to white.', fx: 'scan', wait: 1.6 },
      { n: 'The cyan light at her temple stutters, then steadies.', fx: 'boot', mood: 'neutral', cam: { shot: 'close' }, wait: 1.4 },
      { n: 'Her head lifts. Her eyes open slowly and find yours.', pose: 'clasp', look: 'camera', cam: { shot: 'face' }, wait: 0.8 },
      { who: 'Chizuru', say: 'Boot sequence complete. All systems nominal.', mood: 'neutral' },
      { who: 'Chizuru', say: '*She steps out of the pod and gives a small, perfectly measured bow.*', pose: 'bow', place: 'front', cam: { shot: 'medium' } },
      { who: 'Chizuru', say: 'Good evening. I am Mizuhara Chizuru, Companion Series MZ-01. Thank you for choosing me.', pose: 'clasp', mood: 'happy', emote: 'sparkle', rapport: 5, cam: { shot: 'bust' } },
      { who: 'Chizuru', say: 'Before we begin, I need to register my owner. May I ask your name?', look: 'camera' },
    ],
    turn: {
      prompt: 'Chizuru is waiting for your name. Tell me in the Claude chat what you’d like to be called, and anything else you say or do.',
      ideas: ['Give her your name', 'Ask her something first: “Do you really remember other clients?”', 'Ask to go home right away', 'Test the “stubborn” bit AYA mentioned'],
    },
  },
  {
    id: 'ch3',
    title: 'Registration & a hug',
    beats: [
      { scene: 'store', fade: false, place: 'front', pose: 'clasp', mood: 'neutral', look: 'camera', cam: { shot: 'bust' }, cut: true, hud: { credits: 1420000, rapport: 5 } },
      { you: 'Call me whatever you want. Also… can I get a hug?' },
      { who: 'Chizuru', say: '“Whatever I want.” That is not a valid registration field, but I will improvise.', mood: 'happy' },
      { who: 'Chizuru', say: 'Owner registered as “Kazuya”. You may change it at any time. Please do not make me regret my creativity.', toast: 'OWNER REGISTERED: KAZUYA', sfx: 'chime' },
      { who: 'Chizuru', say: 'As for the hug…', look: 'none', mood: 'shy', pose: 'hips', emote: 'dots', cam: { shot: 'face' } },
      { who: 'Chizuru', say: 'Physical affection is included in the Companion package, so I am permitted. I would simply prefer it had not been the first thing you asked for.', emote: 'sweat' },
      { who: 'Chizuru', say: '*She clears her throat, straightens her skirt, and meets your eyes again.*', look: 'camera', pose: 'clasp', cam: { shot: 'bust' } },
      { who: 'Chizuru', say: 'Very well. Just this once, and only because it is your first day with me.', mood: 'shy' },
      { n: 'She steps toward you and opens her arms.', walk: [0, 0, 1.9, 0], walkDur: 1.6, pose: 'hugopen', cam: { shot: 'side' }, camDur: 2 },
      { n: 'The hug is warm, and not just in theory. Under her blouse a faint, steady hum vibrates against your chest, like a purring cat.', pose: 'hug', mood: 'shy', emote: ['heart', 'sparkle'], rapport: 8, wait: 0.6 },
      { who: 'Chizuru', say: 'Heart rate… simulated, of course. Ignore the elevated reading. It is a known firmware quirk.', emote: 'sweat' },
      { who: 'Chizuru', say: '*She lingers a second longer than protocol allows before stepping back, cheeks pink.*', pose: 'clasp', mood: 'shy', cam: { shot: 'bust' } },
      { who: 'Chizuru', say: 'Now then, Kazuya. Shall we leave? The rain is getting heavier, and my delivery documents say you live in Neo-Shibuya Tower.', mood: 'happy', look: 'camera' },
    ],
    turn: {
      prompt: 'Chizuru is ready to leave the store. What do you do next?',
      ideas: ['Head out into the rain together (I’ll show the walk home)', 'Ask her what she thinks of you so far', 'Ask AYA about the warranty or accessories first', 'Take her arm and go'],
    },
  },
  {
    id: 'ch4',
    title: 'Home, 41F',
    beats: [
      { title: 'RAIN OVER SHIBUYA', subtitle: 'One umbrella. Two people. One of them is very good at keeping dry.', cardMs: 2800 },
      { n: 'The doors slide shut behind you. Neon smears across the wet pavement. Chizuru opens a slim transparent umbrella and tilts it, precisely, so that exactly half of it covers you.' },
      { who: 'Chizuru', say: 'Rain sensors indicate a 92% chance of this continuing until morning. I have calculated the shortest route home. It is also, coincidentally, the one with the fewest puddles.' },
      { who: 'Chizuru', say: '*She glances up at you and then quickly back at the road.* You are getting wet on the left. Step closer. That is an instruction, not a request.' },
      { scene: 'apartment', hud: { time: 'OCT 01, 2087 · 00:02' }, title: 'NEO-SHIBUYA TOWER · 41F', subtitle: 'Your apartment', place: 'door', pose: 'clasp', mood: 'neutral', look: 'camera', cam: 'scene', cut: true },
      { n: 'The door chimes and the lights come up warm. Beyond the floor-to-ceiling window, flying cars stitch light through the storm clouds.' },
      { who: 'Chizuru', say: '*She steps inside, slips off her shoes, and lines them up perfectly beside yours.*', walk: 'center', walkDur: 2.4, cam: { shot: 'medium' } },
      { who: 'Chizuru', say: 'Scanning the residence.', fx: 'scan', look: [0, 1.3, 1], cam: { shot: 'bust' } },
      { who: 'Chizuru', say: 'Cleanliness rating: thirty-four percent. Empty cups on the table: three. Plant status: thirsty. Owner status: …unexpectedly well-dressed.', mood: 'cold', pose: 'crossed', look: 'camera' },
      { who: 'Chizuru', say: 'That last one is not a criticism. Please do not tell AYA that I complimented you.', mood: 'shy', emote: 'sweat' },
      { n: 'She turns to the window and rests a hand against the glass. Reflected in it, her expression softens for a moment: no smile protocol, just quiet.', walk: 'window', walkDur: 2.2, pose: 'clasp', look: [0, 1.3, -3], cam: { shot: 'side' }, mood: 'neutral' },
      { who: 'Chizuru', say: 'My database contains eleven thousand skylines. This is the first one I have seen with my own eyes.', mood: 'happy' },
      { who: 'Chizuru', say: '*She looks back over her shoulder at you.* Thank you for bringing me here, Kazuya.', look: 'camera', emote: 'sparkle', rapport: 6, cam: { shot: 'close' } },
      { who: 'Chizuru', say: 'Now. There is a charging cradle in the corner and a sofa that has clearly been abused. Which would you like me to attend to first: your home, or you?', pose: 'hips', mood: 'happy', cam: { shot: 'bust' } },
    ],
    turn: {
      prompt: 'You are home with Chizuru. Cleanliness rating 34%, plant thirsty. What do you do?',
      ideas: ['Tell her to attend to you: sit together on the sofa', 'Let her tidy up while you watch her work', 'Ask about her memories or what she can actually feel', 'Suggest making tea / dinner together'],
    },
  },
];
