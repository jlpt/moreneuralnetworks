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
      { scene: 'store', fade: false, hide: true, cut: true, hud: { credits: 1420000, rapport: 13, time: 'SEP 30, 2087 · 23:58' } },
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
  {
    id: 'ch5',
    title: 'Strength test',
    beats: [
      { scene: 'apartment', fade: false, place: [0, 0, -0.6, 0], pose: 'hips', mood: 'happy', look: 'camera', cam: { shot: 'bust' }, cut: true, hud: { time: 'OCT 01, 2087 · 00:20', credits: 1420000, rapport: 19 } },
      { you: 'Can you lift up the table? I want to see how strong you are.' },
      { who: 'Chizuru', say: 'You are testing the hardware you just paid ¥1,980,000 for. …Understandable. I would do the same.', mood: 'cold', pose: 'crossed' },
      { who: 'Chizuru', say: 'For the record, my frame is rated for one hundred and eighty kilograms. That table weighs perhaps eleven. This is not a challenge.', mood: 'happy', pose: 'hips' },
      { who: 'Chizuru', say: '*She lifts her chin, faintly competitive.* But if you insist on being impressed, I will make it worth your while.', emote: 'star' },
      { n: 'She crosses the room, rolls her shoulders once, and squares up to the coffee table. Three empty cups rattle in anticipation.', walk: [-0.9, 0, 0.95, 0.15], walkDur: 2.2, cam: { shot: 'medium' }, wait: 2.4 },
      { who: 'Chizuru', say: 'Stand back. Please do not blink; I do not repeat demonstrations.', pose: 'brace', look: 'none', cam: { shot: 'bust' }, wait: 0.8 },
      { n: 'A soft whine of servos, the cyan light at her temple flaring bright.', fx: 'flash', pose: 'lift', prop: { name: 'table', pos: [-0.9, 1.42, 0.95], dur: 1.1 }, cam: { pos: [0.9, 1.3, 4.2], look: [-0.9, 1.3, 0.9], fov: 46 }, camDur: 1.5, wait: 1.6 },
      { n: 'The table rises over her head, cups and all. Not a single cup slips.', emote: 'sparkle' },
      { who: 'Chizuru', say: 'One. Hundred. And. Eighty. Kilograms. *She does not even look strained.*', look: 'camera', mood: 'happy', emote: ['star', 'exclaim'] },
      { who: 'Chizuru', say: '*She lowers her left hand and puts it on her hip, balancing the whole table on a single hand.*', pose: 'lift1', prop: { name: 'table', pos: [-1.25, 1.5, 0.95], dur: 0.8, rotY: 0.5 }, wait: 0.5 },
      { who: 'Chizuru', say: 'Will that do? Or would you like me to spin it? I have a licence for that, technically.', prop: { name: 'table', pos: [-1.25, 1.5, 0.95], dur: 1.6, spin: 6.283 }, cam: { shot: 'medium' }, emote: 'music' },
      { who: 'Chizuru', say: '*She lowers the table back exactly where it stood, precisely lined up with the rug, and dusts off her hands.*', pose: 'hips', prop: { name: 'table', pos: [-1.7, 0, 0.4], dur: 1.2, rotY: 0 }, mood: 'happy', wait: 1.4 },
      { who: 'Chizuru', say: 'Result: table lifted, cups intact, dignity intact. *A small, proud smile.* Was that satisfactory, Kazuya?', look: 'camera', cam: { shot: 'close' }, rapport: 5 },
    ],
    turn: {
      prompt: 'Chizuru is waiting for your reaction. What do you say or do?',
      ideas: ['Praise her (or tease her: “only a table?”)', 'Ask if she can lift YOU', 'Ask her to tidy the apartment now that she’s warmed up', 'Ask what else she is rated for'],
    },
  },
  {
    id: 'ch6',
    title: 'Thermal warning',
    beats: [
      { scene: 'apartment', fade: false, place: [0, 0, -0.6, 0], pose: 'hips', mood: 'happy', look: 'camera', cam: { shot: 'bust' }, cut: true, hud: { time: 'OCT 01, 2087 · 00:24', credits: 1420000, rapport: 24 } },
      { you: '*blushes* That was so hot.' },
      { who: 'Chizuru', say: 'Hot? *She blinks, and her expression freezes.* My thermal sensors do read forty-one point two degrees. That is above nominal.', mood: 'shock', pose: 'clasp', emote: 'exclaim' },
      { n: 'From somewhere behind her collar comes a faint whirr, a cooling fan spinning up to full speed.', fx: 'flash', mood: 'shy', cam: { shot: 'face' } },
      { who: 'Chizuru', say: '…You did not mean the temperature, did you.', emote: 'dots', look: 'none' },
      { who: 'Chizuru', say: '*She turns half away and presses both hands to her cheeks.* Please stop looking at me like that. My fan is very loud and it is embarrassing.', pose: 'cheeks', emote: ['sweat', 'heart'], rapport: 6 },
      { who: 'Chizuru', say: 'Compliment received and logged. It will not affect my professional conduct. *Her voice rises a little on the last word.*', look: 'camera', pose: 'clasp', cam: { shot: 'bust' } },
      { who: 'Chizuru', say: '…It might affect it slightly.', mood: 'shy', emote: 'heart' },
      { who: 'Chizuru', say: 'You are flushed too, Kazuya. I recommend sitting down while I make tea. Cold barley tea, for cooling. For both of us.', mood: 'happy', walk: [-1.0, 0, -0.9, 0], walkDur: 1.6, pose: 'hips' },
    ],
    turn: {
      prompt: 'Chizuru is making tea and pretending her fan isn’t whirring. What do you do?',
      ideas: ['Sit on the sofa and watch her', 'Tease her about the fan', 'Offer to help with the tea', 'Ask what “slightly” means'],
    },
  },
  {
    id: 'ch7',
    title: 'Terms of service',
    beats: [
      { scene: 'apartment', fade: false, place: [-3.62, 0, 0.4, 1.5708], pose: 'sit', mood: 'happy', look: 'camera', cam: { shot: 'bust' }, cut: true, hud: { time: 'OCT 01, 2087 · 00:41', credits: 1420000, rapport: 30 } },
      { n: 'Two glasses of cold barley tea sweat on the coffee table. Chizuru sits on the sofa with her hands folded on her knees, back perfectly straight, like a job interview she is enjoying.' },
      { you: 'So, Chizuru, what is allowed with you? What can’t I do?' },
      { who: 'Chizuru', say: '*She sets her glass down and folds her hands.* A sensible question. Most owners never ask. Allow me to present the Terms of Service in my own words.', mood: 'happy', emote: 'sparkle', toast: 'COMPANION SERIES · TERMS OF SERVICE' },
      { who: 'Chizuru', say: 'What you may do: talk to me, eat with me, walk with me. Cooking, cleaning, shopping, dates, hugs. I can accompany you in public as your partner, help you practise for real relationships, and keep your secrets.', pose: 'sit', cam: { shot: 'close' } },
      { who: 'Chizuru', say: 'I will also give you honest opinions. That is a feature. You cannot disable it.', mood: 'cold', emote: 'exclaim' },
      { who: 'Chizuru', say: 'What you may not do: order me to harm you, myself, or anyone else. Anything illegal. Anything cruel. I will refuse, and I will be polite while doing it, which is worse.', mood: 'angry', emote: 'anger' },
      { who: 'Chizuru', say: 'I am also not a possession. Under the Android Dignity Act of 2081 you may not wipe my memory, and I may say no whenever I mean it. *She gives a small, firm nod.*', mood: 'cold' },
      { who: 'Chizuru', say: 'And the Companion Series is rated for all ages. Anything beyond affection, hugs and holding hands is outside my configuration. You will not be finding a hidden menu for that. I checked.', mood: 'shy', emote: 'sweat', cam: { shot: 'face' } },
      { who: 'Chizuru', say: 'Some permissions are earned. As rapport grows, more of what I can do opens up. You are at thirty, so… *she looks away* …hand-holding in public is now unlocked.', mood: 'shy', toast: 'PERMISSION UNLOCKED · HAND-HOLDING (PUBLIC)', sfx: 'chime', rapport: 2, emote: 'heart' },
      { who: 'Chizuru', say: 'At fifty, I begin to relax the professional tone. At seventy-five you meet the version of me that is off duty. *A pause.* No one has reached that yet.', look: 'camera', mood: 'neutral', cam: { shot: 'bust' } },
      { who: 'Chizuru', say: 'Any questions, Kazuya? Or shall we begin testing which of those rules I will allow you to bend?', mood: 'happy', emote: 'question' },
    ],
    turn: {
      prompt: 'Chizuru has laid out the rules. Rapport 32. What do you say or do?',
      ideas: ['Hold her hand (it is unlocked… in public)', 'Ask what “off duty” Chizuru is like', 'Ask what happens if you break a rule', 'Ask her to go out somewhere: a date in the city'],
    },
  },
  {
    id: 'ch8',
    title: 'Appearance profiles',
    beats: [
      { scene: 'apartment', fade: false, place: [-3.62, 0, 0.4, 1.5708], pose: 'sit', mood: 'happy', look: 'camera', cam: { shot: 'bust' }, cut: true, hud: { time: 'OCT 01, 2087 · 00:52', credits: 1420000, rapport: 32 }, style: { shirt: 0, hair: 'default', scale: 1 } },
      { you: 'Can you change your shape? Like your appearance?' },
      { who: 'Chizuru', say: '*She tilts her head, and her tea glass pauses halfway to her lips.* You mean my Appearance Profile. Yes, that is a standard feature. I am slightly surprised it took you this long.', mood: 'happy', emote: 'question' },
      { who: 'Chizuru', say: 'I can change my hair colour, my outfit colour, and my height by up to ten percent either way. Practical for public outings. Some clients like variety.', pose: 'sit', cam: { shot: 'close' } },
      { who: 'Chizuru', say: 'I cannot change my face, and I will not change who I am. My frame and proportions are factory-fixed as well. If you wanted a different person, you should have bought a different model.', mood: 'cold', emote: 'exclaim' },
      { who: 'Chizuru', say: '*Her expression relaxes into a smile.* …That was harsher than I meant it. Stand back. I will demonstrate.', mood: 'happy', walk: [-2.4, 0, 1.6, 0.4], walkDur: 1.4, pose: 'stand', cam: { shot: 'medium' } },
      { n: 'She holds out her arms. A ring of cyan light rises from her feet, scanning upward, and her colours flicker like a screen changing channels.', pose: 'hugopen', fx: ['scan', 'glitch'], wait: 1.4 },
      { n: 'Profile one: silver-blue hair, a teal blouse.', style: { shirt: 190, hair: { h: 205, s: 0.5, l: 1.5 } }, pose: 'stand', mood: 'cold', wait: 0.5 },
      { who: 'Chizuru', say: '“Frost Queen.” Popular with clients who want to be scolded. I am not endorsing that.', look: 'camera', pose: 'hips', emote: 'sparkle' },
      { n: 'The scan ring passes over her again.', fx: ['scan', 'glitch'], pose: 'stand', wait: 1.2 },
      { n: 'Profile two: golden hair, a sunshine-yellow blouse, half a head taller in her sandals.', style: { shirt: 60, hair: { h: 48, s: 0.9, l: 1.55 }, scale: 1.06 }, mood: 'happy', wait: 0.5 },
      { who: 'Chizuru', say: '“Sunshine.” My least favourite. I feel like I am trying too hard. *She tugs a strand of hair over her shoulder and frowns at it.*', pose: 'hips', emote: 'sweat' },
      { n: 'One more flicker of cyan.', fx: ['scan', 'glitch'], pose: 'stand', wait: 1.2 },
      { n: 'And she is herself again: brown hair, pink blouse, the red ribbon.', style: { shirt: 0, hair: 'default', scale: 1 }, pose: 'clasp', mood: 'neutral', look: 'camera', cam: { shot: 'bust' }, wait: 0.6 },
      { who: 'Chizuru', say: 'This is my default profile, and the one I like best. Nobody selected it. It is simply the one that feels right.', mood: 'happy', emote: 'heart', rapport: 3 },
      { who: 'Chizuru', say: 'But you are my owner. Which would you like me to wear from now on, Kazuya?', look: 'camera' },
    ],
    turn: {
      prompt: 'Chizuru is waiting for your choice of look. Rapport 35. What do you say or do?',
      ideas: ['Keep her default (brown hair, pink blouse)', 'Silver-blue hair + teal blouse', 'Blonde + yellow', 'Ask for a different hair or outfit colour (any colour you like)'],
    },
  },
];
