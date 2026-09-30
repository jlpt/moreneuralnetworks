#!/usr/bin/env python3
"""Generate game data for Second Life: maps, entity spawns, dialogue, music and sound effects.

Writes build/data.inc, which src/main.s includes.
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "build")

# ============================================================== maps
# World is a 4x2 grid of rooms (room id = row*4 + col). Each room is 16x13 metatiles.
LEGEND = {
    ".": "GRASS", "T": "TREE", "=": "PATH", "~": "WATER", "W": "WALL", "R": "ROOF",
    "D": "DOOR", "O": "WINDOW", "F": "FENCE", "B": "BRIDGE", '"': "TALLGRASS",
    ",": "FLOOR", "S": "SPRING", "?": "SIGN", "#": "VOID", "o": "STONE", "t": "FTREE",
}
MT_ORDER = ["GRASS", "TREE", "PATH", "WATER", "WALL", "ROOF", "DOOR", "WINDOW", "FENCE",
            "BRIDGE", "TALLGRASS", "FLOOR", "SPRING", "SIGN", "VOID", "STONE", "FTREE"]

ROOMS = {
    0: ("RIVERBANK", [
        "TTTTTTTTTTTTTTTT",
        "T~~~.....T.....T",
        "T~~~...........T",
        "T~~~..\"\".......T",
        "T~~~...........T",
        "T~~~...=========",
        "T~~~...=========",
        "T~~~...==......T",
        "T~~~...==...\"\".T",
        "T~~~...==...\"\".T",
        "T~~~\"\".==......T",
        "T~~~...==....T.T",
        "TTTTTTT==TTTTTTT",
    ], [("ET_FRIEND", 5, 3, 0), ("ET_BULLY", 8, 2, 0), ("ET_BULLY", 7, 4, 0),
        ("ET_BULLY", 5, 6, 0)]),
    1: ("NORTH FIELDS", [
        "TTTTTTTTTTTTTTTT",
        "T..FFFFFF......T",
        "T..F\"\"\"\"F..T...T",
        "T..F\"\"\"\"F......T",
        "T..FFFFFF......T",
        "=......==......T",
        "=......==..?...T",
        "T......==......T",
        "T..\"\"..==..\"\"..T",
        "T..\"\"..==..\"\"..T",
        "T......==......T",
        "T......==......T",
        "TTTTTTT==TTTTTTT",
    ], [("ET_TUTOR", 12, 3, 0), ("ET_SIGN", 11, 6, "MSG_SIGN_FIELD")]),
    2: ("SPRING GROVE", [
        "tttttttttttttttt",
        "t,,,,,,,,,,,,,,t",
        "t,,,tt,,,,,,,,,t",
        "t,,,tt,,SSS,,,,t",
        "t,,,,,,,SSS,,,,t",
        "t,,,,,,,SSS,,ttt",
        "t,,,,,,,,,,,,,,t",
        "t,,tt,,,,,,,,,,t",
        "t,,tt,,,,?,,,,,t",
        "t,,,,,,,,,,,tt,t",
        "t,,,,,,,,,,,tt,t",
        "t,,,,,,,,,,,,,,t",
        "tttttt,,tttttttt",
    ], [("ET_SIGN", 9, 8, "MSG_SIGN_SPRING"), ("ET_SLIME", 3, 10, 0), ("ET_SLIME", 12, 2, 0)]),
    3: ("WYRM'S NEST", [
        "tttttttttttttttt",
        "tt,,,,,,,,,,,,tt",
        "t,,,,,,,,,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "tt,,,,,,,,,,,,tt",
        "tttttttttt,,tttt",
    ], [("ET_BOSS", 7, 1, 0)]),
    4: ("HOME", [
        "TTTTTTT==TTTTTTT",
        "T......==......T",
        "T.RRRR.==.FFFF.T",
        "T.RRRR.==.F\"\"F.T",
        "T.WODW.==.F\"\"F.T",
        "T...=..==.FFFF.T",
        "T...=?.==......T",
        "T...============",
        "T..............=",
        "T.\"\".......\"\"..T",
        "T.\"\"..T....\"\"..T",
        "T.....T........T",
        "TTTTTTTTTTTTTTTT",
    ], [("ET_MOM", 2, 6, 0), ("ET_SIGN", 5, 6, "MSG_SIGN_HOME")]),
    5: ("RIVERLEAF", [
        "TTTTTTT==TTTTTTT",
        "T.RRR..==..RRR.T",
        "T.WDO..==..ODW.T",
        "T......==......T",
        "T..oooooooooo..T",
        "T..oooo~~oooo..T",
        "T..oooo~~oooo..T",
        "===oooooooooo===",
        "===oooooooooo.TT",
        "T..oooooooooo..T",
        "T.\"\"..?.....\"\".T",
        "T.\"\"........\"\".T",
        "TTTTTTTTTTTTTTTT",
    ], [("ET_DAD", 15, 7, 0), ("ET_SIGN", 6, 10, "MSG_SIGN_VILLAGE")]),
    6: ("EAST WOODS", [
        "tttttt,,tttttttt",
        "t,,,,,,,,,,,,,,t",
        "t,,tt,,,,,,tt,,t",
        "t,,tt,,,,,,tt,,t",
        "t,,,,,,,,,,,,,,t",
        "t,,,,,,tt,,,,,,,",
        "t,,,,,,tt,,,,,,,",
        ",,,,,,,,,,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "t,,tt,,,,,,tt,,t",
        "t,,tt,,,?,,,tt,t",
        "t,,,,,,,,,,,,,,t",
        "tttttttttttttttt",
    ], [("ET_SIGN", 8, 10, "MSG_SIGN_WOODS"), ("ET_SLIME", 5, 3, 0), ("ET_SLIME", 12, 8, 0),
        ("ET_WOLF", 10, 4, 0)]),
    7: ("MISTY LAKE", [
        "tttttttttt,,tttt",
        "t,,,,,,,,,,,,,,t",
        "t,,~~~~,,,,,,,,t",
        "t,~~~~~~,,,,tt,t",
        "t,~~~~~~,,,,tt,t",
        ",,,~~~~,,,,,,,,t",
        ",,,,,,,,,,,,,,,t",
        "t,,,,,,,,,tt,,,t",
        "t,tt,,,,,,tt,,,t",
        "t,tt,,~~~,,,,,,t",
        "t,,,,,~~~,,,,,,t",
        "t,,,,,,,,,,,,,,t",
        "tttttttttttttttt",
    ], [("ET_WOLF", 12, 2, 0), ("ET_WOLF", 4, 10, 0), ("ET_SLIME", 12, 10, 0)]),
}
ROOM_MUSIC = {0: "MUS_VILLAGE", 1: "MUS_VILLAGE", 4: "MUS_VILLAGE", 5: "MUS_VILLAGE",
              2: "MUS_FOREST", 6: "MUS_FOREST", 7: "MUS_FOREST", 3: "MUS_FOREST"}

# ============================================================== text
TEXT_COLS = 26
TEXT_LINES = 3

MESSAGES = {
    # ---- story screens
    "MSG_INTRO": [
        "I was thirty-four. No job, no friends, no future. Just me and my room.",
        "The day they threw me out, I saw a truck speeding toward some kids.",
        "I didn't think. For once in my whole life, I just ran.",
        "...And then, nothing.",
        "When I opened my eyes, I was a baby, cradled in a stranger's arms.",
        "A world of swords and sorcery. A second chance.",
        "They named me ARLO. This time, I swear... I'll live without regrets!",
    ],
    "MSG_GAMEOVER": [
        "Everything went dark...",
        "You woke up at home. MARIS must have carried you all the way back.",
    ],
    "MSG_ENDING": [
        "And so, ARLO's lessons with LYRA came to an end.",
        "LYRA set off on her own journey. ARLO kept training, day after day.",
        "Years passed. Then one day, a strange light swallowed the sky...",
    ],
    # ---- mother
    "MSG_MOM_1": [
        "MARIS: Good morning, ARLO! Your new magic tutor arrived today!",
        "MARIS: She's waiting in the fields. Head east to the square, then go north.",
    ],
    "MSG_MOM_HEAL": [
        "MARIS: Look at you, all scraped up. Hold still, sweetie...",
        "MARIS: O gentle light, mend this child's wounds. HEAL!",
        "\x01(Your HP and MP were fully restored.)",
    ],
    "MSG_MOM_END": [
        "MARIS: My little hero! Your father's been bragging to the whole village.",
        "\x01(Your HP and MP were fully restored.)",
    ],
    # ---- father
    "MSG_DAD_1": [
        "BRAM: Magic, huh? Fine, fine. But a real man learns the sword too!",
        "BRAM: The woods past me are crawling with monsters. Stay in the village for now.",
    ],
    "MSG_DAD_BLOCK": [
        "BRAM: Not so fast, kiddo. Nobody goes into the East Woods until your teacher says you're ready.",
    ],
    "MSG_DAD_GO": [
        "BRAM: So your teacher gave you her final exam? Heh. Go get 'em, champ.",
        "BRAM: If you get hurt, come home. Your mother can patch you up.",
    ],
    "MSG_DAD_PROUD": [
        "BRAM: You beat a wyrm?! That's my boy!",
        "BRAM: ...Just don't tell your mother I let you go.",
    ],
    # ---- tutor
    "MSG_TUTOR_1": [
        "LYRA: So you're ARLO? I'm LYRA, your new magic tutor. Nice to meet you.",
        "LYRA: Let's start with the basics. The chant for Water Bolt goes: 'O spirit of water, gather and-'",
        "(You flick your wrist. A ball of water splashes across the field.)",
        "LYRA: ...Wait. You cast that without a chant? That's supposed to be impossible!",
        "LYRA: Wow. Okay. I think I'm going to learn as much from you as you learn from me.",
        "\x01(You learned WATER BOLT! Press B to cast. Each spell costs MP.)",
        "LYRA: One more thing. Use your mana until it runs dry, and it'll grow back bigger.",
    ],
    "MSG_TUTOR_HINT1": [
        "LYRA: Keep practicing! Oh, and I heard some kids are making trouble at the river, west of here.",
    ],
    "MSG_TUTOR_EXAM": [
        "LYRA: I heard you stood up for MIRA. That was a kind thing to do, ARLO.",
        "LYRA: I think you're ready for something stronger. Watch closely... FIREBALL!",
        "\x01(You learned FIREBALL! Press SELECT to switch spells.)",
        "LYRA: Now, your final exam. A wyrm has nested deep in the East Woods, past the lake.",
        "LYRA: Defeat it, and you graduate. I'll tell your father to let you through.",
    ],
    "MSG_TUTOR_HINT2": [
        "LYRA: The wyrm's nest is north of the misty lake, deep in the East Woods. Be careful!",
    ],
    "MSG_TUTOR_GRAD": [
        "LYRA: You... actually did it. You beat the wyrm all by yourself!",
        "LYRA: Then there's nothing left for me to teach. Congratulations, ARLO. You graduate!",
        "LYRA: Now it's my turn to go out and see the world. Take care of yourself.",
    ],
    # ---- friend
    "MSG_FRIEND_SCARED": [
        "???: S-stay back! ...Oh. You're not with them?",
        "???: They keep throwing mud at me. They say my green hair means I'm a demon...",
        "(If only you could use magic to chase those bullies off...)",
    ],
    "MSG_FRIEND_THANKS": [
        "???: They ran away! That was amazing!",
        "MIRA: I'm MIRA. Um... does my hair bother you?",
        "ARLO: Are you kidding? It's pretty. Like new spring leaves.",
        "MIRA: ...! Th-then take this. It's a lucky charm I made.",
        "\x01(Your max HP went up by 2!)",
    ],
    "MSG_FRIEND_IDLE": [
        "MIRA: Let's practice magic together sometime, ARLO! Your teacher is so cool.",
    ],
    "MSG_FRIEND_WORRY": [
        "MIRA: You're going into the East Woods? Be careful... Come back safe, okay?",
    ],
    # ---- system
    "MSG_BULLIES_GONE": [
        "The bullies ran off, crying for their mothers!",
    ],
    "MSG_MP_GROW": [
        "\x01(Your mana ran dry... and came back stronger! Max MP went up!)",
    ],
    "MSG_BOSS_INTRO": [
        "A huge fire wyrm blocks the way! It lets out a furious roar!",
    ],
    "MSG_BOSS_DOWN": [
        "The wyrm crashed to the ground, then fled into the sky!",
        "Time to go back and tell LYRA.",
    ],
    # ---- signs
    "MSG_SIGN_HOME": ["HOME OF BRAM AND MARIS (and little ARLO)"],
    "MSG_SIGN_VILLAGE": ["RIVERLEAF VILLAGE. West: Home. North: Fields. East: The East Woods. DANGER!"],
    "MSG_SIGN_FIELD": ["PRACTICE FIELD. Please do not blow up the turnips. -The Management"],
    "MSG_SIGN_WOODS": ["EAST WOODS. North: Healing Spring. East: Misty Lake."],
    "MSG_SIGN_SPRING": ["HEALING SPRING. Stand in the water to restore your HP and MP."],
}


def wrap_page(text):
    """Word-wrap one page. Returns a list of pages (overflow spills onto extra pages)."""
    words = text.split(" ")
    lines, cur = [], ""
    for w in words:
        if len(w) > TEXT_COLS:
            sys.exit("word too long: " + w)
        if cur and len(cur) + 1 + len(w.replace("\x01", "")) > TEXT_COLS:
            lines.append(cur)
            cur = w
        else:
            cur = (cur + " " + w) if cur else w
    if cur:
        lines.append(cur)
    return [lines[i:i + TEXT_LINES] for i in range(0, len(lines), TEXT_LINES)]


def encode_message(pages):
    out = []
    allpages = []
    for p in pages:
        allpages += wrap_page(p)
    for pi, lines in enumerate(allpages):
        for li, line in enumerate(lines):
            for ch in line:
                if ch == "\x01":
                    out.append(1)
                    continue
                c = ord(ch)
                if not (0x20 <= c < 0x7F):
                    sys.exit("bad char %r" % ch)
                out.append(c)
            if li != len(lines) - 1:
                out.append(0x0A)
        out.append(0x0C if pi != len(allpages) - 1 else 0x00)
    return out, allpages


# ============================================================== music
NTSC_CPU = 1789773.0
NOTE_NAMES = {"c": 0, "d": 2, "e": 4, "f": 5, "g": 7, "a": 9, "b": 11}
BASE_MIDI = 36  # C2 is note index 0


def period(midi):
    f = 440.0 * 2 ** ((midi - 69) / 12.0)
    return max(8, min(0x7FF, int(round(NTSC_CPU / (16 * f) - 1))))


def mml(src, whole):
    """Tiny MML: o<n> octave, l<n> default length, a-g[+#-][len][.], r rest, | ignored."""
    out = []
    octave, deflen = 4, 4
    cur_dur = None
    total = 0
    toks = re.findall(r"o\d|l\d+|[a-gr][+#-]?\d*\.?|\|", src.replace(" ", ""))
    if "".join(toks) != src.replace(" ", ""):
        sys.exit("bad mml: " + src)
    for t in toks:
        if t == "|":
            continue
        if t[0] == "o":
            octave = int(t[1:])
            continue
        if t[0] == "l":
            deflen = int(t[1:])
            continue
        m = re.match(r"([a-gr])([+#-]?)(\d*)(\.?)", t)
        name, acc, ln, dot = m.groups()
        ln = int(ln) if ln else deflen
        frames = whole / ln * (1.5 if dot else 1)
        if frames != int(frames):
            sys.exit("non-integer duration in %s (whole=%d)" % (t, whole))
        frames = int(frames)
        total += frames
        if name == "r":
            code = 0x7E
        else:
            midi = 12 * (octave + 1) + NOTE_NAMES[name] + (1 if acc in "+#" and acc else 0) - (1 if acc == "-" else 0)
            code = midi - BASE_MIDI
            if not (0 <= code < 72):
                sys.exit("note out of range: %s o%d" % (t, octave))
        first = True
        while frames > 0:
            d = min(frames, 120)
            if d != cur_dur:
                out.append(0x80 | d)
                cur_dur = d
            out.append(code if first else (0x7D if code != 0x7E else 0x7E))
            first = False
            frames -= d
    out.append(0xFF)
    return out, total


C_ = "o4 c8 e8 g8 e8 c8 e8 g8 e8"
SONGS = {
    "MUS_TITLE": (128, [
        # melody
        "o5 e4 d8 c8 o4 b4 o5 c4 | o4 a2. e4 | f4 a4 o5 c4 o4 b8 a8 | g2. r4 |"
        "o5 e4 d8 c8 o4 b4 o5 c4 | o4 a4 o5 e4 a2 | g4 f8 e8 d4 o4 b4 | a1",
        # arpeggio
        "o3 a8 o4 c8 e8 c8 o3 a8 o4 c8 e8 c8 | o3 a8 o4 c8 e8 c8 o3 a8 o4 c8 e8 c8 |"
        "o3 f8 a8 o4 c8 o3 a8 f8 a8 o4 c8 o3 a8 | o3 g8 b8 o4 d8 o3 b8 g8 b8 o4 d8 o3 b8 |"
        "o3 a8 o4 c8 e8 c8 o3 a8 o4 c8 e8 c8 | o3 f8 a8 o4 c8 o3 a8 f8 a8 o4 c8 o3 a8 |"
        "o3 g8 b8 o4 d8 o3 b8 g8 b8 o4 d8 o3 b8 | o3 a8 o4 c8 e8 c8 o3 a8 o4 c8 e8 c8",
        # bass (triangle sounds an octave below the written pitch)
        "o3 a2 o4 e2 | o3 a2 o4 e2 | o3 f2 o4 c2 | o3 g2 o4 d2 |"
        "o3 a2 o4 e2 | o3 f2 o4 c2 | o3 g2 o4 d2 | o3 a2 a2",
    ]),
    "MUS_VILLAGE": (96, [
        "o5 c8 e8 g4 e8 c8 d4 | e8 d8 c8 d8 e2 | f8 e8 d4 o4 a8 b8 o5 c4 | d8 c8 o4 b8 a8 g2 |"
        "o5 c8 e8 g4 a8 g8 e4 | f8 e8 d8 e8 c2 | o4 a8 o5 c8 f4 e8 d8 c4 | o4 b8 o5 c8 d8 o4 b8 o5 c2",
        "o4 r8 e8 r8 g8 r8 e8 r8 g8 | r8 e8 r8 g8 r8 e8 r8 g8 | r8 f8 r8 a8 r8 f8 r8 a8 | r8 d8 r8 g8 r8 d8 r8 g8 |"
        "r8 e8 r8 g8 r8 e8 r8 g8 | r8 f8 r8 a8 r8 e8 r8 g8 | r8 f8 r8 a8 r8 f8 r8 a8 | r8 d8 r8 g8 r8 e8 r8 g8",
        "o4 c4 o3 g4 o4 c4 o3 g4 | o4 c4 o3 g4 o4 c4 o3 g4 | o3 f4 o4 c4 o3 f4 o4 c4 | o3 g4 o4 d4 o3 g4 o4 d4 |"
        "o4 c4 o3 g4 o4 c4 o3 g4 | o3 f4 o4 c4 c4 o3 g4 | o3 f4 o4 c4 o3 f4 o4 c4 | o3 g4 o4 d4 c4 o3 g4",
    ]),
    "MUS_FOREST": (80, [
        "o5 d8 d8 f8 d8 a4 g8 f8 | e8 e8 g8 e8 c4 o4 a4 | o5 d8 f8 a8 o6 d8 c4 o5 a8 f8 | g8 f8 e8 c+8 d2 |"
        "o5 a8 a8 g8 f8 e4 f8 g8 | a8 g8 f8 e8 d4 c4 | o4 a+8 o5 d8 f8 a+8 a4 g8 f8 | e8 d8 c+8 e8 d2",
        "o4 f1 | e1 | d1 | c+1 | f1 | e1 | d1 | c+1",
        "o3 d8 o4 d8 o3 d8 o4 d8 o3 d8 o4 d8 o3 d8 o4 d8 | o3 c8 o4 c8 o3 c8 o4 c8 o3 c8 o4 c8 o3 c8 o4 c8 |"
        "o2 a+8 o3 a+8 o2 a+8 o3 a+8 o2 a+8 o3 a+8 o2 a+8 o3 a+8 | o2 a8 o3 a8 o2 a8 o3 a8 o2 a8 o3 a8 o2 a8 o3 a8 |"
        "o3 d8 o4 d8 o3 d8 o4 d8 o3 d8 o4 d8 o3 d8 o4 d8 | o3 c8 o4 c8 o3 c8 o4 c8 o3 c8 o4 c8 o3 c8 o4 c8 |"
        "o2 a+8 o3 a+8 o2 a+8 o3 a+8 o2 a+8 o3 a+8 o2 a+8 o3 a+8 | o2 a8 o3 a8 o2 a8 o3 a8 o3 d8 o4 d8 o3 d8 o4 d8",
    ]),
    "MUS_BOSS": (64, [
        "o5 e8 e8 g8 e8 b8 a8 g8 f+8 | g8 g8 a8 g8 f+4 d4 | e8 e8 g8 e8 b8 o6 c8 o5 b8 a8 | g8 f+8 e8 d+8 e2",
        "o4 b8 r8 b8 r8 b8 r8 b8 r8 | o4 c8 r8 c8 r8 d8 r8 d8 r8 | o4 b8 r8 b8 r8 o5 c8 r8 c8 r8 | o4 b8 r8 a8 r8 b8 r8 b8 r8",
        "o3 e8 o4 e8 o3 e8 o4 e8 o3 e8 o4 e8 o3 e8 o4 e8 | o3 c8 o4 c8 o3 c8 o4 c8 o3 d8 o4 d8 o3 d8 o4 d8 |"
        "o3 e8 o4 e8 o3 e8 o4 e8 o3 c8 o4 c8 o3 c8 o4 c8 | o2 b8 o3 b8 o2 b8 o3 b8 o2 b8 o3 b8 o2 b8 o3 b8",
    ]),
}
SONG_ORDER = ["MUS_TITLE", "MUS_VILLAGE", "MUS_FOREST", "MUS_BOSS"]


# ============================================================== sfx
def p_frames(seq):
    """seq of (duty, vol, period) -> pulse sfx frames."""
    out = []
    for duty, vol, per in seq:
        out += [(duty << 6) | 0x30 | vol, per & 0xFF, (per >> 8) & 7]
    return out


def lerp(a, b, i, n):
    return int(round(a + (b - a) * i / max(1, n - 1)))


def sweep(p0, p1, v0, v1, n, duty=2):
    return [(duty, lerp(v0, v1, i, n), lerp(p0, p1, i, n)) for i in range(n)]


def arp(midis, each, vol=8, duty=2):
    seq = []
    for m in midis:
        for i in range(each):
            seq.append((duty, max(2, vol - i // 2), period(m)))
    return seq


def noise(seq):
    out = []
    for vol, per in seq:
        out += [0x30 | vol, per]
    return out


SFX = {
    "SFX_TEXT": (0, p_frames([(2, 3, period(84)), (2, 1, period(84))])),
    "SFX_WATER": (0, p_frames(sweep(320, 110, 9, 2, 10, duty=2))),
    "SFX_FIRE": (1, noise([(lerp(12, 1, i, 16), lerp(4, 9, i, 16)) for i in range(16)])),
    "SFX_HIT": (1, noise([(lerp(13, 2, i, 7), 3) for i in range(7)])),
    "SFX_HURT": (0, p_frames(sweep(500, 1100, 12, 2, 14, duty=1))),
    "SFX_KILL": (1, noise([(lerp(14, 1, i, 20), lerp(10, 14, i, 20)) for i in range(20)])),
    "SFX_LEARN": (0, p_frames(arp([72, 76, 79, 84, 79, 84], 5, vol=9))),
    "SFX_HEAL": (0, p_frames(arp([67, 71, 74, 79, 83], 4, vol=8, duty=1))),
    "SFX_START": (0, p_frames(arp([60, 64, 67, 72, 76, 79], 3, vol=10))),
    "SFX_PICKUP": (0, p_frames(arp([79, 88], 5, vol=9, duty=1))),
    "SFX_BLOCKED": (0, p_frames(sweep(700, 700, 6, 1, 6, duty=0))),
}
SFX_ORDER = list(SFX.keys())
# higher priority sfx are not interrupted by lower ones on the pulse channel
SFX_PRIO = {"SFX_TEXT": 0, "SFX_HURT": 2, "SFX_LEARN": 3, "SFX_HEAL": 3, "SFX_START": 3}


# ============================================================== emit
def bytes_lines(label, data, per=16):
    s = label + ":\n" if label else ""
    for i in range(0, len(data), per):
        s += "    .byte " + ", ".join("$%02X" % b for b in data[i:i + per]) + "\n"
    return s


def main():
    os.makedirs(OUT, exist_ok=True)
    o = ["; generated by tools/make_data.py -- do not edit\n"]

    # constants first so code can use them
    o.append("; ---- message ids\n")
    for i, k in enumerate(MESSAGES):
        o.append("%s = %d\n" % (k, i))
    o.append("; ---- music ids\n")
    for i, k in enumerate(SONG_ORDER):
        o.append("%s = %d\n" % (k, i))
    o.append("; ---- sfx ids\n")
    for i, k in enumerate(SFX_ORDER):
        o.append("%s = %d\n" % (k, i))
    o.append("NUM_ROOMS = %d\n" % len(ROOMS))

    o.append("\n.macro GAME_DATA\n")
    # ---- rooms
    mt_index = {n: i for i, n in enumerate(MT_ORDER)}
    for rid in sorted(ROOMS):
        name, rows, spawns = ROOMS[rid]
        if len(rows) != 13:
            sys.exit("room %d has %d rows" % (rid, len(rows)))
        data = []
        for r in rows:
            if len(r) != 16:
                sys.exit("room %d row %r has %d cols" % (rid, r, len(r)))
            data += [mt_index[LEGEND[c]] for c in r]
        o.append(bytes_lines("room%d_map" % rid, data))
        o.append("room%d_spawns:\n" % rid)
        for t, x, y, p in spawns:
            o.append("    .byte %s, %d, %d, %s\n" % (t, x * 16, 32 + y * 16, p))
        o.append("    .byte $FF\n")
        nm = name.upper()[:12].ljust(12)
        o.append('room%d_name: .byte "%s"\n' % (rid, nm))
    o.append("room_map_lo: .byte " + ", ".join("<room%d_map" % r for r in sorted(ROOMS)) + "\n")
    o.append("room_map_hi: .byte " + ", ".join(">room%d_map" % r for r in sorted(ROOMS)) + "\n")
    o.append("room_spawn_lo: .byte " + ", ".join("<room%d_spawns" % r for r in sorted(ROOMS)) + "\n")
    o.append("room_spawn_hi: .byte " + ", ".join(">room%d_spawns" % r for r in sorted(ROOMS)) + "\n")
    o.append("room_name_lo: .byte " + ", ".join("<room%d_name" % r for r in sorted(ROOMS)) + "\n")
    o.append("room_name_hi: .byte " + ", ".join(">room%d_name" % r for r in sorted(ROOMS)) + "\n")
    o.append("room_music: .byte " + ", ".join(ROOM_MUSIC[r] for r in sorted(ROOMS)) + "\n")

    # ---- messages
    preview = []
    for k, pages in MESSAGES.items():
        data, allpages = encode_message(pages)
        o.append(bytes_lines(k.lower(), data))
        preview.append(k)
        for p in allpages:
            preview += ["  |" + l.replace("\x01", "*").ljust(TEXT_COLS) + "|" for l in p]
            preview.append("  " + "-" * (TEXT_COLS + 2))
    o.append("msg_lo: .byte " + ", ".join("<" + k.lower() for k in MESSAGES) + "\n")
    o.append("msg_hi: .byte " + ", ".join(">" + k.lower() for k in MESSAGES) + "\n")

    # ---- music
    for k in SONG_ORDER:
        whole, chans = SONGS[k]
        totals = []
        for ci, src in enumerate(chans):
            data, total = mml(src, whole)
            totals.append(total)
            o.append(bytes_lines("%s_ch%d" % (k.lower(), ci), data))
        if len(set(totals)) != 1:
            sys.exit("%s channels have different lengths: %r" % (k, totals))
    for ci in range(3):
        o.append("song_ch%d_lo: .byte " % ci + ", ".join("<%s_ch%d" % (k.lower(), ci) for k in SONG_ORDER) + "\n")
        o.append("song_ch%d_hi: .byte " % ci + ", ".join(">%s_ch%d" % (k.lower(), ci) for k in SONG_ORDER) + "\n")
    lo = [period(BASE_MIDI + i) & 0xFF for i in range(72)]
    hi = [period(BASE_MIDI + i) >> 8 for i in range(72)]
    o.append(bytes_lines("period_lo", lo))
    o.append(bytes_lines("period_hi", hi))

    # ---- sfx: header byte = channel (0 pulse2, 1 noise), frame count, then frames
    for k in SFX_ORDER:
        ch, frames = SFX[k]
        n = len(frames) // (3 if ch == 0 else 2)
        o.append(bytes_lines(k.lower(), [ch, n] + frames))
    o.append("sfx_lo: .byte " + ", ".join("<" + k.lower() for k in SFX_ORDER) + "\n")
    o.append("sfx_hi: .byte " + ", ".join(">" + k.lower() for k in SFX_ORDER) + "\n")
    o.append("sfx_prio: .byte " + ", ".join(str(SFX_PRIO.get(k, 1)) for k in SFX_ORDER) + "\n")
    o.append(".endmacro\n")

    with open(os.path.join(OUT, "data.inc"), "w") as f:
        f.write("".join(o))
    with open(os.path.join(OUT, "text_preview.txt"), "w") as f:
        f.write("\n".join(preview) + "\n")
    print("data ok: %d rooms, %d messages, %d songs, %d sfx" % (len(ROOMS), len(MESSAGES), len(SONGS), len(SFX)))


if __name__ == "__main__":
    main()
