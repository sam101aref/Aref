"""Backgrounds in the Persian-miniature style: golden or lapis skies with
ribbon clouds, pastel sponge rocks, cypresses, blossoming trees and flowered
meadows. Each scene is 1920x864 (20:9, a landscape phone)."""

import math
import random

from palette import *  # noqa: F401,F403

SW, SH = 1920, 864


def svg(body, defs="", w=SW, h=SH):
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">'
        f"<defs>{defs}</defs>{body}</svg>\n"
    )


def P(d, fill, stroke=INK, sw=2, extra=""):
    s = f' stroke="{stroke}" stroke-width="{sw}" stroke-linejoin="round" stroke-linecap="round"' if stroke else ""
    return f'<path d="{d}" fill="{fill}"{s} {extra}/>'


# ---------------------------------------------------------------- elements


def gold_sky(y1, defs_id="sky"):
    defs = (
        f'<linearGradient id="{defs_id}" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{GOLD_LIGHT}"/><stop offset="1" stop-color="{GOLD}"/></linearGradient>'
    )
    return f'<rect width="{SW}" height="{y1}" fill="url(#{defs_id})"/>', defs


def night_sky(y1, rnd, defs_id="night"):
    defs = (
        f'<linearGradient id="{defs_id}" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{LAPIS_NIGHT}"/><stop offset="1" stop-color="{LAPIS}"/></linearGradient>'
    )
    stars = "".join(
        star(rnd.uniform(20, SW - 20), rnd.uniform(10, y1 - 40), rnd.uniform(3, 7)) for _ in range(70)
    )
    return f'<rect width="{SW}" height="{y1}" fill="url(#{defs_id})"/>' + stars, defs


def star(x, y, r):
    pts = []
    for i in range(8):
        a = math.pi / 4 * i - math.pi / 2
        rr = r if i % 2 == 0 else r * 0.38
        pts.append(f"{x + rr * math.cos(a):.1f} {y + rr * math.sin(a):.1f}")
    return f'<path d="M{" L".join(pts)} Z" fill="{GOLD_LIGHT}"/>'


def cloud(x, y, s=1.0, fill=WHITE, line=LAPIS):
    """A Chinese-style ribbon cloud, as painted in Safavid skies."""
    d = (
        f"M{x} {y} q{20*s} {-34*s} {50*s} {-16*s} q{14*s} {-30*s} {44*s} {-18*s} q{26*s} {-26*s} {52*s} {-2*s} "
        f"q{30*s} {-8*s} {34*s} {18*s} q{24*s} {6*s} {10*s} {24*s} q{-30*s} {14*s} {-80*s} {4*s} "
        f"q{-40*s} {16*s} {-80*s} {4*s} q{-26*s} {6*s} {-30*s} {-14*s} Z"
    )
    curl = (
        f'<path d="M{x + 160*s} {y + 8*s} q{40*s} {6*s} {60*s} {-8*s} q{14*s} {-14*s} {-2*s} {-20*s} '
        f'q{-12*s} {2*s} {-6*s} {12*s}" fill="none" stroke="{line}" stroke-width="{2.4*s:.1f}" stroke-linecap="round"/>'
    )
    tail = (
        f'<path d="M{x} {y} q{-40*s} {8*s} {-70*s} {-4*s} q{-16*s} {-8*s} {-4*s} {-18*s}" fill="none" '
        f'stroke="{line}" stroke-width="{2.4*s:.1f}" stroke-linecap="round"/>'
    )
    return P(d, fill, line, 2.4 * s) + curl + tail


def bumpy_mound(rnd, x0, x1, base, top, lobes):
    """Outline of a miniature rock: a mound whose top is made of rounded lobes."""
    pts = []
    n = lobes
    for i in range(n + 1):
        t = i / n
        x = x0 + (x1 - x0) * t
        hump = math.sin(math.pi * t) ** 0.7
        y = base - (base - top) * hump * rnd.uniform(0.75, 1.0)
        pts.append((x, y))
    d = f"M{x0:.0f} {base:.0f} L{pts[0][0]:.0f} {pts[0][1]:.0f} "
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        cx = (ax + bx) / 2 + rnd.uniform(-8, 8)
        cy = min(ay, by) - abs(bx - ax) * rnd.uniform(0.45, 0.8)
        d += f"Q{cx:.0f} {cy:.0f} {bx:.0f} {by:.0f} "
    d += f"L{x1:.0f} {base:.0f} Z"
    return d


def rock(rnd, x, base, w, h, color, shade):
    out = P(bumpy_mound(rnd, x, x + w, base, base - h, rnd.randint(4, 7)), color, INK, 2)
    # inner contour lines give the spongy, layered look
    for k in range(1, 4):
        inset = w * 0.12 * k
        if w - 2 * inset < 40:
            break
        out += f'<path d="{bumpy_open(rnd, x + inset, x + w - inset, base - h * 0.1 * k, base - h * (1 - 0.22 * k))}" fill="none" stroke="{shade}" stroke-width="3" stroke-linecap="round"/>'
    return out


def bumpy_open(rnd, x0, x1, base, top):
    n = rnd.randint(3, 5)
    pts = []
    for i in range(n + 1):
        t = i / n
        hump = math.sin(math.pi * (0.1 + 0.8 * t)) ** 0.7
        pts.append((x0 + (x1 - x0) * t, base - (base - top) * hump))
    d = f"M{pts[0][0]:.0f} {pts[0][1]:.0f} "
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        d += f"Q{(ax + bx) / 2:.0f} {min(ay, by) - abs(bx - ax) * 0.5:.0f} {bx:.0f} {by:.0f} "
    return d


def cypress(x, base, h, w=None):
    w = w or h * 0.16
    d = (
        f"M{x} {base} Q{x - w * 0.7} {base - h * 0.35} {x - w * 0.45} {base - h * 0.7} "
        f"Q{x - w * 0.2} {base - h * 0.95} {x} {base - h} Q{x + w * 0.2} {base - h * 0.95} {x + w * 0.45} {base - h * 0.7} "
        f"Q{x + w * 0.7} {base - h * 0.35} {x} {base} Z"
    )
    lines = "".join(
        f'<path d="M{x - w * 0.3 + i * w * 0.15:.0f} {base - h * 0.15:.0f} Q{x:.0f} {base - h * (0.5 + i * 0.06):.0f} {x + w * 0.05:.0f} {base - h * (0.8 + i * 0.02):.0f}" fill="none" stroke="{LEAF}" stroke-width="2"/>'
        for i in range(4)
    )
    return P(d, LEAF_DARK, INK, 2) + lines + f'<rect x="{x - 4}" y="{base - 6}" width="8" height="10" fill="{EARTH_DARK}"/>'


def blossom_tree(rnd, x, base, h, bloom=WHITE):
    trunk = P(
        f"M{x - 10} {base} Q{x - 4} {base - h * 0.4} {x - 22} {base - h * 0.62} M{x - 4} {base - h * 0.4} "
        f"Q{x + 16} {base - h * 0.55} {x + 30} {base - h * 0.7} M{x + 10} {base} Q{x + 6} {base - h * 0.3} {x - 4} {base - h * 0.4}",
        "none", EARTH_DARK, 9,
    )
    crown = ""
    for _ in range(26):
        a = rnd.uniform(0, math.pi * 2)
        r = rnd.uniform(0, h * 0.32)
        cx, cy = x + math.cos(a) * r * 1.3, base - h * 0.7 + math.sin(a) * r * 0.8
        crown += f'<circle cx="{cx:.0f}" cy="{cy:.0f}" r="{rnd.uniform(h*0.07, h*0.12):.0f}" fill="{LEAF}" stroke="{INK}" stroke-width="1.5"/>'
    flowers = "".join(
        f'<circle cx="{x + rnd.uniform(-h*0.42, h*0.42):.0f}" cy="{base - h * 0.7 + rnd.uniform(-h*0.3, h*0.3):.0f}" r="{rnd.uniform(3, 6):.1f}" fill="{bloom}"/>'
        for _ in range(60)
    )
    return trunk + crown + flowers


def meadow_flowers(rnd, x0, x1, y0, y1, n, colors):
    out = ""
    for _ in range(n):
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        c = rnd.choice(colors)
        out += f'<path d="M{x:.0f} {y:.0f} l0 -14 M{x:.0f} {y - 6:.0f} l-6 -6 M{x:.0f} {y - 8:.0f} l6 -5" stroke="{LEAF_DARK}" stroke-width="1.6" fill="none"/>'
        out += f'<circle cx="{x:.0f}" cy="{y - 16:.0f}" r="3.6" fill="{c}"/>'
    return out


def reeds(rnd, x0, x1, base, n):
    out = ""
    for _ in range(n):
        x = rnd.uniform(x0, x1)
        h = rnd.uniform(60, 130)
        lean = rnd.uniform(-20, 20)
        out += f'<path d="M{x:.0f} {base} Q{x + lean / 2:.0f} {base - h / 2:.0f} {x + lean:.0f} {base - h:.0f}" stroke="{LEAF_DARK}" stroke-width="2.4" fill="none"/>'
        out += f'<ellipse cx="{x + lean:.0f}" cy="{base - h - 8:.0f}" rx="4" ry="12" fill="{EARTH}" transform="rotate({lean / 2:.0f} {x + lean:.0f} {base - h - 8:.0f})"/>'
    return out


def castle(x, base, s=1.0, wall=PAPER, dome=TURQUOISE):
    out = P(f"M{x} {base} L{x} {base - 120*s} L{x + 200*s} {base - 120*s} L{x + 200*s} {base} Z", wall)
    for i in range(9):
        cx = x + i * 25 * s
        out += P(f"M{cx} {base - 120*s} l0 {-12*s} l{12*s} 0 l0 {12*s} Z", wall, INK, 1.5)
    out += P(f"M{x + 60*s} {base - 120*s} Q{x + 100*s} {base - 220*s} {x + 140*s} {base - 120*s} Z", dome)
    out += P(f"M{x + 85*s} {base} L{x + 85*s} {base - 50*s} Q{x + 100*s} {base - 74*s} {x + 115*s} {base - 50*s} L{x + 115*s} {base} Z", EARTH_DARK)
    out += P(f"M{x - 30*s} {base} L{x - 30*s} {base - 170*s} L{x + 10*s} {base - 170*s} L{x + 10*s} {base} Z", wall)
    out += P(f"M{x - 34*s} {base - 170*s} L{x - 10*s} {base - 200*s} L{x + 14*s} {base - 170*s} Z", VERMILION)
    for i in range(3):
        out += f'<rect x="{x + 30*s + i*60*s:.0f}" y="{base - 95*s:.0f}" width="{10*s:.0f}" height="{22*s:.0f}" rx="{5*s:.0f}" fill="{INK}"/>'
    return out


def tile_band(x, y, w, h, c1=LAPIS, c2=TURQUOISE, c3=WHITE):
    out = f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{c1}" stroke="{INK}" stroke-width="2"/>'
    step = h
    i = 0
    while i * step < w:
        cx = x + i * step + step / 2
        cy = y + h / 2
        out += f'<path d="M{cx} {cy - h * 0.38} L{cx + h * 0.38} {cy} L{cx} {cy + h * 0.38} L{cx - h * 0.38} {cy} Z" fill="{c2}"/>'
        out += f'<circle cx="{cx}" cy="{cy}" r="{h * 0.1}" fill="{c3}"/>'
        i += 1
    return out


def arch(x, y, w, h, fill, frame=PAPER):
    d = f"M{x} {y + h} L{x} {y + w * 0.5} Q{x} {y} {x + w / 2} {y - w * 0.12} Q{x + w} {y} {x + w} {y + w * 0.5} L{x + w} {y + h} Z"
    return P(d, fill, INK, 2.5) + f'<path d="{d}" fill="none" stroke="{frame}" stroke-width="10" opacity=".9"/>' + P(d, "none", INK, 2)


def flames(rnd, x0, x1, base, h, n):
    out = ""
    for layer, (color, k) in enumerate([(FIRE_OUT, 1.0), (FIRE_MID, 0.72), (FIRE_CORE, 0.42)]):
        for _ in range(n):
            x = rnd.uniform(x0, x1)
            fh = h * k * rnd.uniform(0.55, 1.0)
            fw = fh * rnd.uniform(0.22, 0.34)
            lean = rnd.uniform(-0.25, 0.25) * fh
            d = (
                f"M{x - fw:.0f} {base} Q{x - fw * 1.1:.0f} {base - fh * 0.5:.0f} {x + lean * 0.4:.0f} {base - fh * 0.7:.0f} "
                f"Q{x - fw * 0.2 + lean:.0f} {base - fh * 0.85:.0f} {x + lean:.0f} {base - fh:.0f} "
                f"Q{x + fw * 0.8 + lean * 0.5:.0f} {base - fh * 0.6:.0f} {x + fw * 0.4:.0f} {base - fh * 0.45:.0f} "
                f"Q{x + fw * 1.1:.0f} {base - fh * 0.25:.0f} {x + fw:.0f} {base} Z"
            )
            stroke = INK if layer == 0 else "none"
            out += P(d, color, stroke if stroke != "none" else None, 1.6)
    return out


def logs(rnd, x0, x1, base, rows):
    out = ""
    for r in range(rows):
        y = base - r * 18
        x = x0 + (r % 2) * 20 + r * 30
        while x < x1 - r * 30:
            ln = rnd.uniform(80, 140)
            out += f'<rect x="{x:.0f}" y="{y - 16:.0f}" width="{ln:.0f}" height="16" rx="8" fill="{EARTH_DARK}" stroke="{INK}" stroke-width="1.5"/>'
            out += f'<circle cx="{x + ln - 8:.0f}" cy="{y - 8:.0f}" r="5" fill="{EARTH}" stroke="{INK}" stroke-width="1"/>'
            x += ln - 6
    return out


def crowd(rnd, x0, x1, base, n, colors):
    out = ""
    for _ in range(n):
        x = rnd.uniform(x0, x1)
        y = base + rnd.uniform(-10, 10)
        c = rnd.choice(colors)
        out += P(f"M{x - 14:.0f} {y:.0f} Q{x - 16:.0f} {y - 40:.0f} {x:.0f} {y - 46:.0f} Q{x + 16:.0f} {y - 40:.0f} {x + 14:.0f} {y:.0f} Z", c, INK, 1.4)
        out += f'<circle cx="{x:.0f}" cy="{y - 54:.0f}" r="9" fill="{SKIN}" stroke="{INK}" stroke-width="1.2"/>'
        out += P(f"M{x - 10:.0f} {y - 56:.0f} Q{x:.0f} {y - 74:.0f} {x + 10:.0f} {y - 56:.0f} Z", rnd.choice([WHITE, VERMILION, LAPIS]), INK, 1.2)
    return out


# ---------------------------------------------------------------- scenes


def zabul(seed=3, dusk=False, far=False):
    """Zabulistan: golden sky, Rostam's castle on a hill, pastel rocks, the reeds of the Helmand.
    far=True leaves out the river and the foreground strip, for the gameplay backdrop."""
    rnd = random.Random(seed)
    horizon = 300
    sky, defs = gold_sky(horizon + 40)
    if dusk:
        defs = (
            f'<linearGradient id="sky" x1="0" y1="0" x2="0" y2="1">'
            f'<stop offset="0" stop-color="{PLUM}"/><stop offset=".6" stop-color="{VERMILION}"/><stop offset="1" stop-color="{SAFFRON}"/></linearGradient>'
        )
    out = sky
    for cx, cy, s in [(160, 110, 1.0), (760, 70, 0.8), (1300, 130, 1.1), (1700, 60, 0.7)]:
        out += cloud(cx, cy, s)
    # distant hills and castle
    out += P(f"M0 {horizon + 30} Q300 {horizon - 80} 640 {horizon + 10} Q980 {horizon - 120} 1320 {horizon + 20} Q1640 {horizon - 60} {SW} {horizon + 10} L{SW} {SH} L0 {SH} Z", "#A9B86A")
    out += castle(1380, horizon - 30, 0.9)
    # meadow
    out += P(f"M0 {horizon + 60} Q480 {horizon + 20} 960 {horizon + 70} Q1440 {horizon + 110} {SW} {horizon + 50} L{SW} {SH} L0 {SH} Z", MEADOW)
    out += meadow_flowers(rnd, 0, SW, horizon + 100, SH - 140, 160, [WHITE, VERMILION, GOLD_LIGHT, ROSE])
    # rocks
    out += rock(rnd, -60, 560, 420, 300, LILAC, PLUM)
    out += rock(rnd, 240, 580, 300, 200, ROSE, VERMILION)
    out += rock(rnd, 1560, 600, 420, 330, TURQUOISE_LIGHT, TURQUOISE)
    out += cypress(560, 520, 300)
    out += cypress(1500, 560, 260)
    out += blossom_tree(rnd, 1080, 520, 300, WHITE)
    out += blossom_tree(rnd, 300, 700, 220, ROSE)
    if far:
        out += meadow_flowers(rnd, 0, SW, SH - 140, SH, 60, [WHITE, VERMILION, GOLD_LIGHT, ROSE])
        return svg(out, defs)
    # river with reeds in the foreground
    out += P(f"M0 640 Q500 600 980 650 Q1460 700 {SW} 640 L{SW} 700 Q1460 760 980 712 Q500 660 0 704 Z", "#BFD6DB", INK, 2)
    out += "".join(f'<path d="M{x} {650 + (x % 3) * 12} q30 -8 60 0" stroke="{WHITE}" stroke-width="3" fill="none"/>' for x in range(40, SW, 140))
    out += reeds(rnd, 0, 700, 720, 40)
    out += reeds(rnd, 1300, SW, 720, 40)
    # ground strip where the player walks
    out += P(f"M0 744 Q960 724 {SW} 744 L{SW} {SH} L0 {SH} Z", EARTH, INK, 2)
    out += P(f"M0 744 Q960 724 {SW} 744 L{SW} 760 Q960 740 0 760 Z", LEAF, None)
    return svg(out, defs)


def fire_trial(seed=7):
    """The trial by fire: two mountains of burning wood with a narrow road between."""
    rnd = random.Random(seed)
    sky, defs = night_sky(560, rnd)
    defs += (
        f'<radialGradient id="glow" cx=".5" cy=".75" r=".6"><stop offset="0" stop-color="{FIRE_MID}" stop-opacity=".85"/>'
        f'<stop offset="1" stop-color="{FIRE_OUT}" stop-opacity="0"/></radialGradient>'
    )
    out = sky + f'<rect width="{SW}" height="{SH}" fill="url(#glow)"/>'
    out += f'<circle cx="1640" cy="120" r="54" fill="{WHITE}" stroke="{GOLD}" stroke-width="3"/>'
    out += P(f"M0 520 Q480 470 960 500 Q1440 530 {SW} 480 L{SW} {SH} L0 {SH} Z", EARTH_DARK)
    # the city far away
    out += castle(840, 500, 0.6, PAPER_DARK, LAPIS)
    out += crowd(rnd, 0, 360, 600, 22, [LAPIS, VERMILION, TURQUOISE, PLUM, SAFFRON])
    out += crowd(rnd, 1560, SW, 600, 22, [LAPIS, VERMILION, TURQUOISE, PLUM, SAFFRON])
    # two burning mountains with a gap for the rider
    out += logs(rnd, 300, 900, 760, 8)
    out += logs(rnd, 1040, 1640, 760, 8)
    out += flames(rnd, 320, 880, 640, 460, 18)
    out += flames(rnd, 1060, 1620, 640, 460, 18)
    out += P(f"M0 760 L{SW} 760 L{SW} {SH} L0 {SH} Z", EARTH, INK, 2)
    return svg(out, defs)


def coffeehouse(seed=11, with_curtain=True):
    """The storyteller's coffeehouse: brick arches, tiles, samovar, benches, the painted curtain."""
    rnd = random.Random(seed)
    out = f'<rect width="{SW}" height="{SH}" fill="#7A4A2A"/>'
    # brick courses
    for y in range(0, SH, 28):
        off = 0 if (y // 28) % 2 else 40
        for x in range(-40 + off, SW, 80):
            out += f'<rect x="{x + 2}" y="{y + 2}" width="76" height="24" fill="{rnd.choice(["#8C5631", "#93603A", "#7F4C2B"])}"/>'
    out += tile_band(0, 40, SW, 46)
    out += arch(70, 200, 300, 560, "#3B2416")
    out += arch(1550, 200, 300, 560, "#3B2416")
    if with_curtain:
        out += f'<rect x="470" y="130" width="980" height="560" fill="{PAPER}" stroke="{INK}" stroke-width="3"/>'
        out += f'<rect x="470" y="122" width="980" height="16" rx="8" fill="{EARTH_DARK}"/>'
    # lanterns
    for x in (220, 1700):
        out += f'<path d="M{x} 86 L{x} 170" stroke="{INK}" stroke-width="3"/>'
        out += P(f"M{x - 26} 210 Q{x} 160 {x + 26} 210 Q{x} 250 {x - 26} 210 Z", GOLD)
        out += f'<circle cx="{x}" cy="212" r="60" fill="{FIRE_CORE}" opacity=".18"/>'
    # samovar on a shelf
    out += f'<rect x="120" y="620" width="200" height="16" fill="{EARTH_DARK}" stroke="{INK}" stroke-width="2"/>'
    out += P("M170 620 L176 540 Q220 500 264 540 L270 620 Z", STEEL)
    out += P("M200 520 L240 520 L232 500 L208 500 Z", STEEL_DARK)
    out += f'<path d="M264 560 Q298 570 300 596" stroke="{STEEL_DARK}" stroke-width="6" fill="none"/>'
    # tea glasses
    for x in (1640, 1690, 1740):
        out += P(f"M{x} 600 L{x + 26} 600 L{x + 22} 640 L{x + 4} 640 Z", "#B4532A", INK, 1.6)
    out += f'<rect x="1610" y="640" width="200" height="14" fill="{EARTH_DARK}" stroke="{INK}" stroke-width="2"/>'
    # floor and benches with kilims
    out += f'<rect x="0" y="760" width="{SW}" height="{SH - 760}" fill="#5A3620"/>'
    for x0, x1 in ((0, 600), (1320, SW)):
        out += f'<rect x="{x0}" y="740" width="{x1 - x0}" height="44" fill="{EARTH_DARK}" stroke="{INK}" stroke-width="2"/>'
        out += f'<rect x="{x0}" y="728" width="{x1 - x0}" height="24" fill="{VERMILION}" stroke="{INK}" stroke-width="2"/>'
        out += "".join(f'<path d="M{x} 730 l10 10 l-10 10 l-10 -10 Z" fill="{SAFFRON}"/>' for x in range(x0 + 20, x1, 40))
    return svg(out)


def palace(seed=5):
    """Kavus's palace: lapis tiles, a pointed arch opening onto a garden, a carpet."""
    rnd = random.Random(seed)
    out = f'<rect width="{SW}" height="{SH}" fill="{PAPER}"/>'
    out += f'<rect x="0" y="0" width="{SW}" height="120" fill="{LAPIS}"/>'
    for x in range(0, SW, 60):
        out += star(x + 30, 60, 18)
    out += tile_band(0, 120, SW, 40, TURQUOISE, LAPIS, GOLD_LIGHT)
    # garden through the arches
    for i, x in enumerate((160, 760, 1360)):
        out += arch(x, 260, 400, 470, "#B9D3A0")
        out += f'<clipPath id="ac{i}"><path d="M{x} 730 L{x} {260 + 200} Q{x} 260 {x + 200} 212 Q{x + 400} 260 {x + 400} {460} L{x + 400} 730 Z"/></clipPath>'
        out += f'<g clip-path="url(#ac{i})">' + cypress(x + 120, 730, 360) + cypress(x + 290, 730, 320) + blossom_tree(rnd, x + 205, 740, 260, ROSE) + "</g>"
    out += f'<rect x="0" y="730" width="{SW}" height="{SH - 730}" fill="{CRIMSON}" stroke="{INK}" stroke-width="2"/>'
    out += tile_band(0, 730, SW, 30, GOLD, CRIMSON, LAPIS)
    out += "".join(f'<circle cx="{x}" cy="810" r="10" fill="{GOLD}"/>' for x in range(60, SW, 120))
    return svg(out)


def pardeh_map(seed=13):
    """The painted storyteller's curtain used as the level map: a cloth painting with the
    places of Siavosh's journey joined by a winding road (stage medallions go on top in UI)."""
    rnd = random.Random(seed)
    out = f'<rect width="{SW}" height="{SH}" fill="{PAPER}"/>'
    out += f'<rect x="20" y="20" width="{SW - 40}" height="{SH - 40}" fill="none" stroke="{VERMILION}" stroke-width="10"/>'
    out += f'<rect x="38" y="38" width="{SW - 76}" height="{SH - 76}" fill="none" stroke="{INK}" stroke-width="2"/>'
    # river Jeyhun crossing the middle
    out += P("M1180 40 Q1120 300 1220 460 Q1300 620 1200 824 L1270 824 Q1370 620 1290 460 Q1200 300 1250 40 Z", "#BFD6DB", INK, 2)
    # region washes
    out += P("M44 44 L1150 44 Q1100 300 1190 460 Q1260 620 1170 820 L44 820 Z", "#E9DDB5", None)
    out += P("M1290 44 L1876 44 L1876 820 L1300 820 Q1390 620 1310 460 Q1230 300 1290 44 Z", "#D8E0C0", None)
    # vignettes: Zabulistan, the court, the fire, Balkh, Turan, Siavashgerd
    out += cypress(150, 330, 160) + cypress(200, 340, 130) + castle(240, 330, 0.55)
    out += castle(560, 270, 0.7, PAPER, LAPIS)
    out += logs(rnd, 860, 1000, 380, 4) + flames(rnd, 870, 990, 320, 150, 7)
    out += castle(900, 720, 0.6, PAPER_DARK, TURQUOISE)
    out += castle(1420, 300, 0.75, PAPER, PLUM)
    out += castle(1620, 700, 0.8, WHITE, TURQUOISE) + cypress(1590, 700, 130) + cypress(1820, 700, 130)
    out += rock(rnd, 300, 740, 240, 140, LILAC, PLUM) + rock(rnd, 1500, 520, 200, 120, ROSE, VERMILION)
    for cx, cy, s in [(400, 110, 0.6), (1500, 100, 0.6)]:
        out += cloud(cx, cy, s)
    # the road
    road = "M150 380 C300 520 420 300 560 320 C700 340 760 460 930 420 C1080 380 760 640 900 760 C1040 860 1140 560 1260 520 C1360 480 1360 360 1480 340 C1640 320 1700 520 1700 700"
    out += f'<path d="{road}" fill="none" stroke="{EARTH}" stroke-width="16" stroke-linecap="round"/>'
    out += f'<path d="{road}" fill="none" stroke="{PAPER}" stroke-width="4" stroke-dasharray="14 14" stroke-linecap="round"/>'
    return svg(out)


SCENES = {
    "bg_zabul_far": lambda: zabul(seed=3, far=True),
    "bg_dusk_far": lambda: zabul(seed=9, dusk=True, far=True),
    "bg_zabul": zabul,
    "bg_pass_dusk": lambda: zabul(seed=9, dusk=True),
    "bg_fire": fire_trial,
    "bg_coffeehouse": coffeehouse,
    "bg_palace": palace,
    "bg_pardeh_map": pardeh_map,
}

if __name__ == "__main__":
    import os
    import sys

    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    for name, fn in SCENES.items():
        open(os.path.join(out, f"{name}.svg"), "w").write(fn())
    print("wrote", len(SCENES), "scenes")
