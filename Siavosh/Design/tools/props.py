"""Props, ground tiles, effects and UI icons for the game, in the same miniature style.

Each prop is an SVG fragment in its own coordinates; the exporter trims it to its bounds and
scales it to the size it has in the game world."""

import math
import random

from palette import *  # noqa: F401,F403
from scenes import P, cypress, blossom_tree, reeds, rock, star, flames, logs

S = 'stroke="%s" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"' % INK


def dummy():
    out = P("M92 330 L108 330 L108 120 L92 120 Z", EARTH_DARK)
    out += P("M40 150 L160 150 L160 164 L40 164 Z", EARTH_DARK)
    out += P("M62 170 Q50 240 70 300 Q100 316 130 300 Q150 240 138 170 Q100 158 62 170 Z", SAFFRON)
    for y in (196, 236, 276):
        out += f'<path d="M58 {y} Q100 {y + 10} 142 {y}" fill="none" stroke="{EARTH_DARK}" stroke-width="5"/>'
    out += P("M70 80 Q66 40 100 36 Q134 40 130 80 Q132 118 100 122 Q68 118 70 80 Z", PAPER_DARK)
    out += f'<path d="M86 74 L96 84 M96 74 L86 84 M106 74 L116 84 M116 74 L106 84" stroke="{CRIMSON}" stroke-width="4"/>'
    out += P("M50 330 L150 330 L156 346 L44 346 Z", EARTH)
    return out


def target():
    out = f'<path d="M100 160 L60 330 M100 160 L140 330 M100 160 L100 340" stroke="{EARTH_DARK}" stroke-width="9" stroke-linecap="round"/>'
    for r, c in ((96, SAFFRON), (78, WHITE), (60, VERMILION), (42, WHITE), (24, LAPIS), (10, GOLD)):
        out += f'<circle cx="100" cy="130" r="{r}" fill="{c}" stroke="{INK}" stroke-width="2"/>'
    return out


def gate():
    """An iwan: tiled frame around a pointed arch, the stage exit."""
    out = P("M0 500 L0 40 L300 40 L300 500 Z", LAPIS)
    for y in range(52, 500, 30):
        for x in range(12, 300, 30):
            out += f'<path d="M{x + 9} {y} l9 9 l-9 9 l-9 -9 Z" fill="{TURQUOISE}"/>'
    out += P("M40 500 L40 230 Q40 110 150 80 Q260 110 260 230 L260 500 Z", GOLD)
    out += P("M56 500 L56 236 Q56 128 150 100 Q244 128 244 236 L244 500 Z", "#2A1A12")
    out += f'<path d="M56 500 L56 236 Q56 128 150 100 Q244 128 244 236 L244 500" fill="none" stroke="{GOLD_LIGHT}" stroke-width="3" stroke-dasharray="6 6"/>'
    out += P("M-10 40 L310 40 L310 10 L-10 10 Z", GOLD)
    out += "".join(star(x, 25, 8) for x in range(20, 300, 40))
    out += P("M120 500 L120 420 Q150 380 180 420 L180 500 Z", FIRE_CORE, INK, 2)
    return out


def log():
    out = P("M20 40 L280 30 Q300 60 280 90 L20 100 Q0 70 20 40 Z", EARTH_DARK)
    out += f'<ellipse cx="282" cy="60" rx="18" ry="30" fill="{EARTH}" stroke="{INK}" stroke-width="2"/>'
    out += f'<ellipse cx="282" cy="60" rx="9" ry="16" fill="none" stroke="{EARTH_DARK}" stroke-width="2"/>'
    out += f'<path d="M60 52 Q140 46 220 44 M50 78 Q150 74 240 70" stroke="{EARTH}" stroke-width="3" fill="none"/>'
    return out


def branch():
    out = P("M0 20 Q160 40 300 90 L300 104 Q160 60 0 40 Z", EARTH_DARK)
    rnd = random.Random(4)
    for _ in range(26):
        x, y = rnd.uniform(40, 290), rnd.uniform(40, 120)
        out += f'<ellipse cx="{x:.0f}" cy="{y:.0f}" rx="22" ry="11" fill="{LEAF}" stroke="{INK}" stroke-width="1.5" transform="rotate({rnd.uniform(-40, 40):.0f} {x:.0f} {y:.0f})"/>'
    out += "".join(f'<circle cx="{rnd.uniform(40, 290):.0f}" cy="{rnd.uniform(40, 120):.0f}" r="5" fill="{ROSE}"/>' for _ in range(12))
    return out


def chest():
    out = P("M10 60 L190 60 L190 150 L10 150 Z", EARTH_DARK)
    out += P("M10 60 Q100 0 190 60 Z", EARTH)
    out += f'<path d="M40 34 L40 150 M160 34 L160 150 M10 100 L190 100" stroke="{GOLD}" stroke-width="8"/>'
    out += P("M88 90 L112 90 L112 118 L88 118 Z", GOLD)
    return out


def coin():
    out = f'<circle cx="50" cy="50" r="46" fill="{GOLD_LIGHT}" stroke="{GOLD_DARK}" stroke-width="5"/>'
    out += f'<circle cx="50" cy="50" r="32" fill="none" stroke="{GOLD_DARK}" stroke-width="2.5" stroke-dasharray="4 4"/>'
    out += f'<path d="M34 46 Q50 30 66 46 M36 58 Q50 70 64 58" fill="none" stroke="{GOLD_DARK}" stroke-width="4" stroke-linecap="round"/>'
    return out


def leaf_page():
    out = P("M10 10 L110 4 L116 150 Q60 160 14 150 Z", PAPER)
    out += f'<path d="M20 18 L104 13 L108 142 Q60 150 22 142 Z" fill="none" stroke="{GOLD}" stroke-width="4"/>'
    for i, y in enumerate(range(36, 136, 16)):
        out += f'<path d="M30 {y} q12 -5 22 0 t22 0 t22 0" fill="none" stroke="{INK}" stroke-width="2.4"/>'
    out += f'<rect x="44" y="64" width="36" height="22" fill="{LAPIS}" stroke="{GOLD}" stroke-width="2"/>'
    return out


def pomegranate():
    out = f'<circle cx="60" cy="70" r="46" fill="{VERMILION}" stroke="{INK}" stroke-width="2.5"/>'
    out += P("M46 30 L50 12 L58 24 L64 8 L70 24 L78 12 L76 32 Z", CRIMSON)
    out += f'<path d="M36 60 Q46 46 60 44" fill="none" stroke="{ROSE}" stroke-width="6" stroke-linecap="round"/>'
    return out


def lamb():
    out = ""
    for x in (60, 80, 130, 150):
        out += f'<rect x="{x}" y="110" width="9" height="44" fill="{INK}"/>'
    rnd = random.Random(2)
    out += P("M40 80 Q40 50 80 50 Q120 40 160 56 Q186 70 176 104 Q170 124 130 124 Q90 130 56 120 Q36 110 40 80 Z", WHITE)
    for _ in range(20):
        out += f'<circle cx="{rnd.uniform(56, 166):.0f}" cy="{rnd.uniform(60, 116):.0f}" r="{rnd.uniform(6, 11):.0f}" fill="{WHITE}" stroke="{PAPER_DARK}" stroke-width="1.5"/>'
    out += P("M168 60 Q188 44 206 56 Q214 72 204 86 Q190 96 176 86 Z", PAPER)
    out += f'<circle cx="196" cy="64" r="3" fill="{INK}"/>'
    out += P("M176 56 Q166 44 160 56 Q168 64 176 62 Z", ROSE)
    return out


def hoopoe(wings_up=True, hurt=False):
    """The hoopoe (hodhod), the wise bird of Persian poetry."""
    out = ""
    if hurt:
        out += P("M30 90 Q60 60 110 70 Q140 80 136 100 Q100 112 60 108 Q36 104 30 90 Z", SAFFRON)
        out += P("M60 92 Q90 104 120 120 Q90 120 64 106 Z", INK)
        out += f'<path d="M64 98 L120 118 M70 96 L118 112" stroke="{WHITE}" stroke-width="3"/>'
        out += f'<circle cx="122" cy="74" r="14" fill="{SAFFRON}" stroke="{INK}" stroke-width="2"/>'
        out += f'<path d="M134 76 L166 88" stroke="{INK}" stroke-width="3"/>'
        out += f'<circle cx="126" cy="70" r="2.5" fill="{INK}"/>'
        out += P("M112 62 L104 40 L116 54 L118 34 L124 54 L132 40 L128 62 Z", VERMILION)
        return out
    out += P("M20 80 Q60 66 110 70 Q130 74 132 86 Q100 98 60 96 Q30 94 20 80 Z", SAFFRON)
    wing = "M60 80 Q70 20 120 10 Q110 50 90 82 Z" if wings_up else "M60 84 Q80 130 130 140 Q110 100 92 86 Z"
    out += P(wing, INK)
    out += f'<path d="{wing}" fill="none" stroke="{WHITE}" stroke-width="3" stroke-dasharray="8 8"/>'
    out += f'<circle cx="132" cy="74" r="13" fill="{SAFFRON}" stroke="{INK}" stroke-width="2"/>'
    out += f'<path d="M144 76 L176 84" stroke="{INK}" stroke-width="3"/>'
    out += f'<circle cx="136" cy="70" r="2.5" fill="{INK}"/>'
    out += P("M122 64 L112 40 L126 54 L128 32 L134 54 L144 42 L138 64 Z", VERMILION)
    out += P("M20 80 L0 70 L4 86 Z", INK)
    return out


def arrow():
    out = f'<path d="M10 20 L230 20" stroke="{EARTH}" stroke-width="5" stroke-linecap="round"/>'
    out += P("M230 10 L262 20 L230 30 Z", STEEL)
    out += P("M10 20 L36 6 L48 6 L24 20 Z", VERMILION)
    out += P("M10 20 L36 34 L48 34 L24 20 Z", VERMILION)
    return out


def fence():
    out = ""
    for x in (10, 90, 170):
        out += P(f"M{x} 160 L{x} 30 L{x + 10} 20 L{x + 20} 30 L{x + 20} 160 Z", EARTH)
    out += P("M0 60 L200 54 L200 70 L0 76 Z", EARTH_DARK) + P("M0 110 L200 104 L200 120 L0 126 Z", EARTH_DARK)
    return out


def banner():
    out = f'<path d="M30 400 L30 10" stroke="{EARTH_DARK}" stroke-width="9" stroke-linecap="round"/>'
    out += f'<circle cx="30" cy="10" r="10" fill="{GOLD}" stroke="{INK}" stroke-width="2"/>'
    out += P("M34 30 Q120 20 200 40 Q170 80 200 120 Q120 104 34 120 Z", VERMILION)
    out += P("M60 60 Q100 40 140 70 Q110 76 96 96 Q80 74 60 80 Z", GOLD)
    out += "".join(f'<path d="M{x} 118 l0 18" stroke="{GOLD}" stroke-width="3"/>' for x in range(40, 200, 14))
    return out


def ledge(width):
    """A pastel rock slab the player can stand on; its top edge is flat."""
    rnd = random.Random(width)
    d = f"M0 20 L{width} 20 Q{width + 6} 60 {width - 20} 90 "
    x = width - 20
    while x > 30:
        nx = x - rnd.uniform(40, 80)
        d += f"Q{(x + nx) / 2:.0f} {120 + rnd.uniform(0, 30):.0f} {max(nx, 20):.0f} {90 + rnd.uniform(-10, 10):.0f} "
        x = nx
    d += "Q-6 60 0 20 Z"
    out = P(d, LILAC)
    for k in range(3):
        y = 40 + k * 18
        out += f'<path d="M{20 + k * 12} {y} Q{width / 2:.0f} {y + 12} {width - 20 - k * 12} {y}" fill="none" stroke="{PLUM}" stroke-width="3"/>'
    out += P(f"M0 20 L{width} 20 L{width} 4 Q{width / 2:.0f} -6 0 4 Z", MEADOW)
    out += "".join(f'<circle cx="{rnd.uniform(10, width - 10):.0f}" cy="{rnd.uniform(4, 14):.0f}" r="3" fill="{rnd.choice([WHITE, VERMILION, GOLD_LIGHT])}"/>' for _ in range(width // 30))
    return out


def ground_tile(kind="meadow", w=512, h=320):
    """Seamless along x: everything repeats with period w."""
    rnd = random.Random(7 if kind == "meadow" else 8)
    earth = EARTH if kind == "meadow" else "#8A5A34"
    top = MEADOW if kind == "meadow" else "#B08A55"
    out = f'<rect x="0" y="30" width="{w}" height="{h - 30}" fill="{earth}"/>'
    for k, y in enumerate((90, 150, 220, 280)):
        amp = 8 + k * 2
        out += f'<path d="M0 {y} ' + " ".join(f"Q{x + 32} {y + (amp if (x // 64) % 2 else -amp)} {x + 64} {y}" for x in range(0, w, 64)) + f'" fill="none" stroke="{EARTH_DARK}" stroke-width="3" opacity=".55"/>'
    for _ in range(26):
        x, y = rnd.uniform(0, w), rnd.uniform(60, h - 10)
        out += f'<ellipse cx="{x:.0f}" cy="{y:.0f}" rx="{rnd.uniform(4, 9):.0f}" ry="{rnd.uniform(3, 6):.0f}" fill="{rnd.choice([PAPER_DARK, EARTH_DARK, ROSE])}"/>'
    out += f'<path d="M0 0 L{w} 0 L{w} 34 ' + " ".join(f"Q{x - 16} {44 if (x // 32) % 2 else 30} {x - 32} 36" for x in range(w, 0, -32)) + ' Z" fill="' + top + '"/>'
    out += f'<path d="M0 0 L{w} 0" stroke="{LEAF_DARK}" stroke-width="4"/>'
    if kind == "meadow":
        for _ in range(18):
            x = rnd.uniform(6, w - 6)
            out += f'<path d="M{x:.0f} 10 l0 -10 M{x:.0f} 4 l-5 -6 M{x:.0f} 4 l5 -6" stroke="{LEAF_DARK}" stroke-width="2" fill="none"/>'
            out += f'<circle cx="{x:.0f}" cy="10" r="3.4" fill="{rnd.choice([WHITE, VERMILION, GOLD_LIGHT, ROSE])}"/>'
    return out


def slash():
    out = P("M10 150 Q60 20 220 10 Q110 50 40 160 Z", WHITE, None)
    out += f'<path d="M10 150 Q60 20 220 10" fill="none" stroke="{GOLD_LIGHT}" stroke-width="6" stroke-linecap="round"/>'
    return out


def spark():
    pts = []
    for i in range(16):
        a = math.pi * 2 * i / 16
        r = 60 if i % 2 == 0 else 22
        pts.append(f"{60 + r * math.cos(a):.1f} {60 + r * math.sin(a):.1f}")
    return f'<path d="M{" L".join(pts)} Z" fill="{FIRE_CORE}" stroke="{GOLD}" stroke-width="3"/>'


def dust():
    return "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{PAPER_DARK}" opacity=".85"/>' for x, y, r in ((40, 60, 30), (80, 50, 36), (120, 64, 28), (64, 80, 24), (100, 84, 26)))


# ---------------------------------------------------------------- icons (white strokes on 24x24)

ICON_PATHS = {
    "sword": "M5 19 L17 7 M14 4 L20 10 M7 14 L10 17 M4 20 L6 18",
    "shield": "M12 3 L19 6 V12 C19 17 12 21 12 21 C12 21 5 17 5 12 V6 Z",
    "bow": "M7 3 Q21 12 7 21 M7 3 L7 21 M7 12 L19 12 M16 9 L19 12 L16 15",
    "jump": "M12 20 V5 M6 11 L12 5 L18 11",
    "duck": "M12 4 V19 M6 13 L12 19 L18 13",
    "dodge": "M4 16 A8 8 0 1 1 18 18 M18 13 V18 H13",
    "farr": "M12 3 V9 M7 5 L9 10 M17 5 L15 10 M5 11 C5 18 19 18 19 11 Z",
    "pause": "M8 5 V19 M16 5 V19",
    "back": "M15 4 L7 12 L15 20",
    "lock": "M7 11 V8 A5 5 0 0 1 17 8 V11 M5 11 H19 V20 H5 Z",
    "check": "M5 12 L10 17 L19 7",
    "heart": "M12 20 C6 15 3 12 3 8.5 C3 6 5 4 7.5 4 C9.5 4 11 5.5 12 7 C13 5.5 14.5 4 16.5 4 C19 4 21 6 21 8.5 C21 12 18 15 12 20 Z",
    "hand": "M8 13 V6 A1.5 1.5 0 0 1 11 6 V12 M11 11 V4 A1.5 1.5 0 0 1 14 4 V11 M14 11 V5 A1.5 1.5 0 0 1 17 5 V13 C17 18 14 21 11 21 C8 21 6 19 5 16 L4 13 A1.5 1.5 0 0 1 7 12 L8 13",
    "close": "M6 6 L18 18 M18 6 L6 18",
    "gear": "M12 8 A4 4 0 1 0 12.01 8 M12 2 V5 M12 19 V22 M2 12 H5 M19 12 H22 M5 5 L7 7 M17 17 L19 19 M5 19 L7 17 M17 7 L19 5",
    "book": "M4 5 Q8 3 12 6 Q16 3 20 5 V19 Q16 17 12 20 Q8 17 4 19 Z M12 6 V20",
    "tent": "M3 20 L12 4 L21 20 Z M12 4 V20 M9 20 L12 14 L15 20",
    "map": "M3 6 L9 4 L15 6 L21 4 V18 L15 20 L9 18 L3 20 Z M9 4 V18 M15 6 V20",
    "play": "M8 5 L19 12 L8 19 Z",
    "sound": "M4 9 H8 L13 5 V19 L8 15 H4 Z M16 9 Q18 12 16 15 M18 7 Q22 12 18 17",
    "music": "M9 18 V5 L19 3 V16 M9 18 A3 3 0 1 1 6 15 A3 3 0 0 1 9 18 M19 16 A3 3 0 1 1 16 13 A3 3 0 0 1 19 16",
    "palm": "M12 3 V9 M7 5 L9 10 M17 5 L15 10 M5 11 C5 18 19 18 19 11 Z",
    "flame": "M12 3 C16 8 18 11 18 14 A6 6 0 0 1 6 14 C6 11 8 9 10 7 C10 10 11 11 12 11 C13 9 13 6 12 3 Z",
    "oath": "M12 2 L14.5 8.5 L21 9 L16 13.5 L17.5 20 L12 16.5 L6.5 20 L8 13.5 L3 9 L9.5 8.5 Z",
    "sun": "M12 7 A5 5 0 1 0 12.01 7 M12 1 V4 M12 20 V23 M1 12 H4 M20 12 H23 M4.2 4.2 L6.3 6.3 M17.7 17.7 L19.8 19.8 M4.2 19.8 L6.3 17.7 M17.7 6.3 L19.8 4.2",
    "heavy": "M4 20 L14 10 M12 6 L18 12 M14 4 L20 10 L18 12 L12 6 Z",
    "twin": "M3 9 L18 9 M15 6 L18 9 L15 12 M3 15 L18 15 M15 12 L18 15 L15 18",
    "eye": "M2 12 C6 6 18 6 22 12 C18 18 6 18 2 12 Z M12 9 A3 3 0 1 0 12.01 9",
    "mounted": "M4 18 L10 12 L14 14 L20 6 M16 6 L20 6 L20 10",
    "shoe": "M7 4 V12 A5 5 0 0 0 17 12 V4",
    "wind": "M3 8 H15 A3 3 0 1 0 12 5 M3 12 H19 A3 3 0 1 1 16 15 M3 16 H11",
    "leap": "M4 20 Q12 2 20 20 M10 20 H14",
    "call": "M12 3 V13 M8 9 L12 13 L16 9 M5 17 H19 M7 21 H17",
    "armor": "M6 4 L10 3 Q12 5 14 3 L18 4 L20 9 L17 10 L17 20 L7 20 L7 10 L4 9 Z",
    "skip": "M5 5 L13 12 L5 19 Z M13 5 L21 12 L13 19 Z",
}


def icon(name):
    return f'<path d="{ICON_PATHS[name]}" fill="none" stroke="#FFFFFF" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>'


# name -> (svg fragment builder, world height in units). Bottom-centre is the prop's origin.
PROPS = {
    "cypress": (lambda: cypress(100, 600, 560), 5.0),
    "tree_blossom": (lambda: blossom_tree(random.Random(3), 200, 420, 400, WHITE), 4.4),
    "tree_rose": (lambda: blossom_tree(random.Random(9), 200, 420, 400, ROSE), 4.0),
    "reeds": (lambda: reeds(random.Random(5), 0, 220, 200, 14), 1.7),
    "rock_lilac": (lambda: rock(random.Random(1), 0, 300, 380, 260, LILAC, PLUM), 2.6),
    "rock_rose": (lambda: rock(random.Random(2), 0, 300, 300, 200, ROSE, VERMILION), 2.0),
    "rock_turq": (lambda: rock(random.Random(3), 0, 300, 360, 280, TURQUOISE_LIGHT, TURQUOISE), 2.8),
    "dummy": (dummy, 2.3),
    "target": (target, 2.0),
    "gate": (gate, 4.6),
    "log": (log, 0.62),
    "stone": (lambda: rock(random.Random(6), 0, 120, 140, 100, PAPER_DARK, EARTH), 0.8),
    "branch": (branch, 1.2),
    "chest": (chest, 0.8),
    "coin": (coin, 0.36),
    "leaf": (leaf_page, 0.6),
    "pomegranate": (pomegranate, 0.5),
    "lamb": (lamb, 0.9),
    "bird_up": (lambda: hoopoe(True), 0.55),
    "bird_down": (lambda: hoopoe(False), 0.55),
    "bird_hurt": (lambda: hoopoe(hurt=True), 0.45),
    "arrow": (arrow, 0.16),
    "fence": (fence, 1.0),
    "banner": (banner, 3.4),
    "ledge_s": (lambda: ledge(240), 0.75),
    "ledge_l": (lambda: ledge(480), 0.8),
    "slash": (slash, 1.5),
    "spark": (spark, 0.7),
    "dust": (dust, 0.6),
}
