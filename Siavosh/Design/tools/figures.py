"""Standing character figures in a Persian-miniature style.

Every figure faces right in three-quarter view inside a 240x420 box with the
feet on y=410. A figure is described by a small dictionary (colours, headgear,
beard, held item) so the whole cast shares one drawing style.
"""

from palette import *  # noqa: F401,F403

W, H = 240, 420
STROKE = f'stroke="{INK}" stroke-width="1.6" stroke-linejoin="round" stroke-linecap="round"'


def svg(body, w=W, h=H, defs="", top=0, left=0):
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{left} {top} {w} {h - top}" width="{w}" height="{h - top}">'
        f"<defs>{defs}</defs>{body}</svg>\n"
    )


def path(d, fill, extra=""):
    return f'<path d="{d}" fill="{fill}" {STROKE} {extra}/>'


def dots(points, r, fill):
    return "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"/>' for x, y in points)


def scale_x(s, cx=120):
    """Transform that widens a figure around its centre line (for Rostam)."""
    return f'transform="translate({cx * (1 - s):.1f},0) scale({s},1)"'


# ---------------------------------------------------------------- body parts


def boots(c):
    color = c.get("boots", EARTH_DARK)
    back = path("M98 340 L100 400 L120 401 Q130 400 133 394 Q136 404 128 409 L96 409 Q92 404 94 398 L92 340 Z", color)
    front = path("M128 340 L130 400 L150 401 Q160 400 163 394 Q166 404 158 409 L126 409 Q122 404 124 398 L120 340 Z", color)
    trim = f'<path d="M124 392 L134 392 M96 392 L106 392" stroke="{GOLD}" stroke-width="2"/>'
    return back + front + trim


def back_arm(c):
    sleeve = c["robe"]
    return path("M94 108 Q80 150 82 196 Q84 214 94 218 Q100 206 100 192 Q100 150 106 120 Z", sleeve) + \
        f'<circle cx="90" cy="218" r="7" fill="{SKIN}" stroke="{INK}" stroke-width="1.4"/>'


def robe(c):
    length = c.get("hem", 350)
    flare = c.get("flare", 0)
    d = (
        f"M90 104 Q82 150 96 190 Q86 {length - 70} {76 - flare} {length} "
        f"Q122 {length + 12} {168 + flare} {length - 2} Q152 {length - 80} 146 190 "
        f"Q158 140 152 104 Q122 94 90 104 Z"
    )
    out = path(d, c["robe"])
    if c.get("pattern"):
        pts = []
        for row, y in enumerate(range(124, length - 10, 22)):
            for x in range(92 + (row % 2) * 11, 160, 22):
                pts.append((x, y))
        out += f'<g clip-path="url(#robe-clip-{c["id"]})">{dots(pts, 2.6, c["pattern"])}</g>'
    # underrobe peeking at the hem and the diagonal front opening of the qaba
    under = c.get("under", WHITE)
    out += path(f"M132 {length - 6} Q150 {length - 8} {168 + flare} {length - 2} Q150 {length + 6} 132 {length + 4} Z", under)
    out += f'<path d="M124 100 Q146 130 140 190 Q138 270 134 {length + 4}" fill="none" stroke="{c.get("trim", GOLD)}" stroke-width="4"/>'
    out += f'<path d="M124 100 Q146 130 140 190 Q138 270 134 {length + 4}" fill="none" stroke="{INK}" stroke-width="1"/>'
    # collar
    out += path("M108 100 Q122 112 136 100 Q130 94 122 96 Q114 94 108 100 Z", c.get("trim", GOLD))
    return out


def robe_clip(c):
    length = c.get("hem", 350)
    flare = c.get("flare", 0)
    d = (
        f"M90 104 Q82 150 96 190 Q86 {length - 70} {76 - flare} {length} "
        f"Q122 {length + 12} {168 + flare} {length - 2} Q152 {length - 80} 146 190 "
        f"Q158 140 152 104 Q122 94 90 104 Z"
    )
    return f'<clipPath id="robe-clip-{c["id"]}"><path d="{d}"/></clipPath>'


def sash(c):
    color = c.get("sash", VERMILION)
    out = path("M94 182 Q122 192 148 182 L148 196 Q122 206 94 196 Z", color)
    out += path("M136 196 Q140 230 132 262 L142 262 Q150 230 146 196 Z", color)
    out += f'<circle cx="140" cy="191" r="5" fill="{GOLD}" stroke="{INK}" stroke-width="1.2"/>'
    return out


def cape(c):
    if not c.get("cape"):
        return ""
    return path("M92 104 Q60 200 52 330 Q80 344 100 330 Q96 220 112 110 Z", c["cape"])


def front_arm(c):
    pose = c.get("pose", "rest")
    sleeve = c["robe"]
    if pose == "raise":  # hand lifted forward, e.g. holding a weapon up or gesturing
        arm = path("M146 106 Q168 128 176 156 Q182 172 192 168 Q196 158 190 150 Q176 120 156 104 Z", sleeve)
        hand = (196, 160)
    elif pose == "hip":  # hand on the sword hilt at the hip
        arm = path("M146 106 Q166 140 162 176 Q160 190 150 192 Q144 184 148 170 Q150 140 138 116 Z", sleeve)
        hand = (152, 194)
    else:  # forward and slightly down
        arm = path("M146 106 Q170 140 172 186 Q172 202 162 204 Q156 198 158 184 Q154 146 138 118 Z", sleeve)
        hand = (166, 206)
    cuff = f'<circle cx="{hand[0] - 2}" cy="{hand[1] - 8}" r="0" />'
    return arm + cuff, hand


def hand(pt):
    x, y = pt
    return f'<circle cx="{x}" cy="{y}" r="7.5" fill="{SKIN}" stroke="{INK}" stroke-width="1.4"/>'


# ---------------------------------------------------------------- held items


def held_item(kind, h):
    x, y = h
    if kind == "sword":  # point down, resting forward
        return (
            path(f"M{x - 3} {y + 4} L{x + 40} {y + 190} L{x + 44} {y + 186} L{x + 4} {y + 2} Z", STEEL)
            + path(f"M{x - 14} {y + 2} L{x + 14} {y - 6} L{x + 16} {y} L{x - 12} {y + 8} Z", GOLD)
            + path(f"M{x - 2} {y - 4} L{x - 6} {y - 22} L{x} {y - 24} L{x + 4} {y - 6} Z", CRIMSON)
            + f'<circle cx="{x - 4}" cy="{y - 25}" r="4" fill="{GOLD}" stroke="{INK}" stroke-width="1.2"/>'
        )
    if kind == "sword_up":  # raised for a strike
        return (
            path(f"M{x + 2} {y - 4} L{x + 70} {y - 170} L{x + 76} {y - 166} L{x + 8} {y + 2} Z", STEEL)
            + path(f"M{x - 10} {y - 10} L{x + 16} {y + 6} L{x + 12} {y + 12} L{x - 14} {y - 4} Z", GOLD)
            + path(f"M{x - 2} {y + 4} L{x - 10} {y + 22} L{x - 4} {y + 24} L{x + 4} {y + 8} Z", CRIMSON)
        )
    if kind == "wood_sword":  # training sword
        return (
            path(f"M{x - 3} {y + 4} L{x + 34} {y + 160} L{x + 40} {y + 157} L{x + 4} {y + 2} Z", EARTH)
            + path(f"M{x - 12} {y + 2} L{x + 12} {y - 4} L{x + 14} {y + 2} L{x - 10} {y + 8} Z", EARTH_DARK)
        )
    if kind == "mace":  # ox-head mace held upright
        return (
            f'<path d="M{x - 2} {y + 70} L{x + 4} {y - 120}" stroke="{EARTH_DARK}" stroke-width="7" stroke-linecap="round"/>'
            + path(f"M{x - 12} {y - 118} Q{x - 16} {y - 150} {x + 4} {y - 156} Q{x + 24} {y - 150} {x + 20} {y - 118} Q{x + 4} {y - 108} {x - 12} {y - 118} Z", STEEL)
            + path(f"M{x - 12} {y - 142} Q{x - 30} {y - 150} {x - 30} {y - 170} Q{x - 20} {y - 156} {x - 8} {y - 152} Z", BEARD_WHITE)
            + path(f"M{x + 18} {y - 142} Q{x + 36} {y - 150} {x + 36} {y - 170} Q{x + 26} {y - 156} {x + 14} {y - 152} Z", BEARD_WHITE)
            + dots([(x - 3, y - 136), (x + 11, y - 136)], 2.6, INK)
            + f'<path d="M{x - 10} {y - 116} Q{x + 4} {y - 110} {x + 18} {y - 116}" fill="none" stroke="{GOLD}" stroke-width="3"/>'
        )
    if kind == "staff":  # the storyteller's stick
        return f'<path d="M{x + 2} {y - 70} L{x + 22} {y + 200}" stroke="{EARTH_DARK}" stroke-width="5" stroke-linecap="round"/>' + \
            f'<circle cx="{x + 2}" cy="{y - 72}" r="6" fill="{GOLD}" stroke="{INK}" stroke-width="1.2"/>'
    if kind == "bow":
        return (
            f'<path d="M{x + 6} {y - 92} Q{x + 52} {y} {x + 6} {y + 92}" fill="none" stroke="{EARTH_DARK}" stroke-width="6" stroke-linecap="round"/>'
            + f'<path d="M{x + 6} {y - 92} Q{x + 52} {y} {x + 6} {y + 92}" fill="none" stroke="{GOLD}" stroke-width="2"/>'
            + f'<path d="M{x + 6} {y - 92} L{x + 6} {y + 92}" stroke="{INK}" stroke-width="1"/>'
        )
    if kind == "cup":
        return path(f"M{x + 4} {y - 22} L{x + 26} {y - 22} Q{x + 24} {y - 6} {x + 15} {y - 4} Q{x + 6} {y - 6} {x + 4} {y - 22} Z", GOLD) + \
            f'<path d="M{x + 15} {y - 4} L{x + 15} {y + 2} M{x + 9} {y + 3} L{x + 21} {y + 3}" stroke="{INK}" stroke-width="1.6"/>'
    if kind == "flower":
        return f'<path d="M{x + 4} {y} Q{x + 14} {y - 30} {x + 10} {y - 52}" fill="none" stroke="{LEAF_DARK}" stroke-width="2"/>' + \
            dots([(x + 10, y - 58), (x + 4, y - 54), (x + 16, y - 54), (x + 10, y - 50)], 4.5, VERMILION) + \
            f'<circle cx="{x + 10}" cy="{y - 54}" r="2.5" fill="{GOLD_LIGHT}"/>'
    if kind == "spear":
        return f'<path d="M{x} {y + 200} L{x + 10} {y - 150}" stroke="{EARTH_DARK}" stroke-width="5" stroke-linecap="round"/>' + \
            path(f"M{x + 10} {y - 150} L{x + 3} {y - 170} L{x + 11} {y - 196} L{x + 18} {y - 170} Z", STEEL)
    return ""


# ---------------------------------------------------------------- head


def head(c):
    out = ""
    hair = c.get("hair", HAIR)
    # long locks falling behind the ear (miniature princes wear side curls)
    if c.get("locks", True):
        out += path("M100 66 Q92 96 100 116 Q110 118 112 104 Q106 88 110 70 Z", hair)
    if c.get("veil"):
        out += path("M104 38 Q84 96 80 170 Q76 236 88 290 Q100 296 104 280 Q96 236 100 180 Q104 110 120 46 Z", c["veil"])
    out += f'<rect x="114" y="82" width="15" height="22" fill="{SKIN_SHADE}" stroke="{INK}" stroke-width="1.4"/>'
    out += f'<ellipse cx="124" cy="64" rx="20" ry="24" fill="{SKIN}" stroke="{INK}" stroke-width="1.6"/>'
    out += f'<path d="M106 58 Q104 44 118 40 Q132 38 142 50 Q130 46 120 50 Q112 54 110 66 Z" fill="{hair}"/>'
    out += f'<ellipse cx="112" cy="66" rx="4" ry="6" fill="{SKIN_SHADE}" stroke="{INK}" stroke-width="1.2"/>'
    # face: almond eye, arched brow, nose on the profile edge, small red mouth
    out += f'<path d="M126 60 Q131 56 137 60 Q131 63 126 60 Z" fill="{WHITE}" stroke="{INK}" stroke-width="1.2"/>'
    out += f'<circle cx="132.5" cy="59.8" r="2" fill="{INK}"/>'
    out += f'<path d="M124 53 Q131 48 139 52" fill="none" stroke="{INK}" stroke-width="1.6"/>'
    out += f'<path d="M142 58 L146 70 L141 72" fill="none" stroke="{INK}" stroke-width="1.3"/>'
    out += f'<path d="M134 79 Q138 81 141 79" fill="none" stroke="{VERMILION}" stroke-width="2.2" stroke-linecap="round"/>'
    beard = c.get("beard")
    bc = c.get("beard_color", hair)
    if beard == "short":
        out += path("M108 70 Q110 92 124 94 Q138 94 144 82 Q138 86 130 84 Q124 90 116 84 Q112 78 110 70 Z", bc)
        out += path("M130 76 Q136 73 143 77 Q137 78 130 76 Z", bc)
    elif beard == "long":
        out += path("M106 68 Q104 112 126 132 Q146 118 146 82 Q138 88 130 84 Q122 92 114 84 Q110 76 108 68 Z", bc)
        out += path("M128 76 Q136 72 145 76 Q150 84 156 80 Q150 90 140 80 Q134 79 128 76 Z", bc)
    elif beard == "forked":  # Rostam's two-pointed beard
        out += path("M104 68 Q100 110 116 140 Q122 120 126 112 Q132 126 140 138 Q150 110 146 82 Q138 88 130 84 Q122 92 114 84 Q108 76 106 68 Z", bc)
        out += path("M128 76 Q138 70 148 74 Q156 82 162 76 Q156 90 142 80 Q134 79 128 76 Z", bc)
    elif beard == "pointed":
        out += path("M108 70 Q112 96 132 116 Q140 100 144 82 Q138 86 130 84 Q122 92 114 84 Q110 76 110 70 Z", bc)
        out += path("M128 76 Q138 72 148 72 Q140 80 128 76 Z", bc)
    out += headgear(c)
    return out


def headgear(c):
    kind = c.get("headgear")
    if kind == "kiani":  # white turban with a small golden Kayanian crown
        return (
            path("M100 52 Q98 30 122 26 Q146 28 146 50 Q124 42 100 52 Z", WHITE)
            + f'<path d="M104 42 Q122 34 144 40" fill="none" stroke="{PAPER_DARK}" stroke-width="1.4"/>'
            + path("M104 34 L106 14 L114 26 L122 6 L130 26 L138 14 L140 34 Q122 28 104 34 Z", GOLD)
            + dots([(122, 18), (110, 28), (134, 28)], 2.6, VERMILION)
            + f'<path d="M140 30 Q156 22 162 34 Q154 30 146 36" fill="none" stroke="{VERMILION}" stroke-width="3"/>'
        )
    if kind == "royal":  # tall crown of the Shah with a crescent
        return (
            path("M98 50 Q96 34 122 30 Q148 32 148 50 Q124 42 98 50 Z", WHITE)
            + path("M100 40 L100 2 L110 18 L116 -6 L122 14 L128 -6 L134 18 L144 2 L144 40 Q122 32 100 40 Z", GOLD)
            + path("M112 -10 Q122 -22 132 -10 Q122 -16 112 -10 Z", GOLD)
            + dots([(122, 26), (108, 30), (136, 30), (122, 10)], 3, VERMILION)
            + dots([(114, 22), (130, 22)], 2.4, TURQUOISE)
        )
    if kind == "leopard":  # Rostam's leopard-head helmet
        return (
            path("M98 56 Q94 22 122 18 Q152 20 150 56 Q140 44 122 42 Q106 44 98 56 Z", SAFFRON)
            + path("M150 56 Q160 46 164 34 Q150 30 146 40 Z", SAFFRON)
            + dots([(110, 30), (122, 26), (134, 30), (142, 40), (114, 42), (128, 36)], 2.4, INK)
            + path("M104 22 L102 8 L114 18 Z", SAFFRON) + path("M136 18 L146 8 L144 22 Z", SAFFRON)
            + f'<path d="M150 46 L158 44 M148 52 L160 52" stroke="{WHITE}" stroke-width="2"/>'
        )
    if kind == "turan":  # tall fur-trimmed Turanian hat
        return (
            path("M104 44 Q106 6 128 -6 Q140 10 142 44 Z", c.get("hat", PLUM))
            + path("M98 52 Q96 38 122 36 Q148 38 148 52 Q122 46 98 52 Z", c.get("fur", EARTH))
            + f'<path d="M104 46 Q122 42 144 46" fill="none" stroke="{EARTH_DARK}" stroke-width="1.4" stroke-dasharray="2 3"/>'
            + f'<circle cx="128" cy="-6" r="4" fill="{GOLD}" stroke="{INK}" stroke-width="1.2"/>'
        )
    if kind == "helmet":  # pointed soldier helmet with mail
        return (
            path("M100 54 Q100 22 124 8 Q148 22 146 54 Q124 46 100 54 Z", STEEL)
            + path("M100 54 Q98 84 108 96 L114 84 Q108 70 110 56 Z", STEEL_DARK)
            + f'<path d="M124 8 L124 -4" stroke="{INK}" stroke-width="2"/>'
            + f'<path d="M104 50 Q124 42 144 50" fill="none" stroke="{GOLD}" stroke-width="2.4"/>'
        )
    if kind == "queen":  # small crown over a flowing veil
        return (
            path("M102 44 Q102 30 122 28 Q142 30 144 44 Q122 38 102 44 Z", c.get("veil", WHITE))
            + path("M106 36 L108 20 L116 30 L122 16 L128 30 L136 20 L138 36 Q122 30 106 36 Z", GOLD)
            + dots([(122, 26)], 3, c.get("jewel", VERMILION))
            + f'<path d="M108 34 Q100 50 104 70" fill="none" stroke="{GOLD}" stroke-width="1.5" stroke-dasharray="1 3"/>'
        )
    if kind == "turban":
        return path("M100 54 Q94 28 122 22 Q150 26 148 52 Q124 42 100 54 Z", c.get("hat", WHITE)) + \
            f'<path d="M102 46 Q122 30 146 42 M104 38 Q124 26 142 32" fill="none" stroke="{PAPER_DARK}" stroke-width="1.6"/>'
    if kind == "cap":  # storyteller's felt cap
        return path("M102 50 Q100 26 124 24 Q148 26 146 50 Q124 44 102 50 Z", c.get("hat", EARTH_DARK)) + \
            f'<path d="M102 48 Q124 42 146 48" fill="none" stroke="{GOLD}" stroke-width="2"/>'
    return ""


# ---------------------------------------------------------------- whole figure


def figure(c):
    item = c.get("item")
    arm, hnd = front_arm(c)
    body = ""
    body += cape(c)
    if item == "spear":
        body += held_item(item, hnd)
    body += back_arm(c)
    body += boots(c)
    body += robe(c)
    if c.get("armor"):
        body += path("M96 120 Q122 132 148 120 L146 184 Q122 194 98 184 Z", c["armor"])
        body += f'<path d="M100 140 L144 140 M100 156 L144 156 M100 172 L144 172" stroke="{GOLD_DARK}" stroke-width="1.3"/>'
    if c.get("stripes"):  # tiger-skin coat, babr-e bayan
        body += f'<g clip-path="url(#robe-clip-{c["id"]})">' + "".join(
            f'<path d="M{x} {y} q10 4 6 12 q-4 8 4 14" fill="none" stroke="{INK}" stroke-width="3.2" stroke-linecap="round"/>'
            for x, y in [(96, 130), (120, 150), (140, 128), (100, 220), (126, 236), (150, 214), (90, 290), (116, 300), (146, 290), (108, 260), (134, 180)]
        ) + "</g>"
    body += sash(c)
    body += head(c)
    if item not in ("spear",):
        body += held_item(item, hnd) if item in ("mace", "bow") else ""
    body += arm
    body += hand(hnd)
    if item == "sword_up":
        body = body.replace(arm + hand(hnd), "")
        body += held_item(item, hnd) + arm + hand(hnd)
    elif item and item not in ("mace", "bow", "spear"):
        body += held_item(item, hnd)
    defs = robe_clip(c)
    s = c.get("scale_x", 1.0)
    g = f'<g {scale_x(s)}>{body}</g>' if s != 1.0 else body
    return svg(g, w=320, defs=defs, top=-30, left=-40)


def figure_parts(c):
    """The figure split into puppet parts that share one coordinate frame:
    body (everything but the front arm and boots), arm (front arm, hand and held item),
    and the two boots. Returns {part: svg}."""
    item = c.get("item")
    arm, hnd = front_arm(c)
    body = cape(c) + back_arm(c) + robe(c)
    if c.get("armor"):
        body += path("M96 120 Q122 132 148 120 L146 184 Q122 194 98 184 Z", c["armor"])
        body += f'<path d="M100 140 L144 140 M100 156 L144 156 M100 172 L144 172" stroke="{GOLD_DARK}" stroke-width="1.3"/>'
    if c.get("stripes"):
        body += f'<g clip-path="url(#robe-clip-{c["id"]})">' + "".join(
            f'<path d="M{x} {y} q10 4 6 12 q-4 8 4 14" fill="none" stroke="{INK}" stroke-width="3.2" stroke-linecap="round"/>'
            for x, y in [(96, 130), (120, 150), (140, 128), (100, 220), (126, 236), (150, 214), (90, 290), (116, 300), (146, 290), (108, 260), (134, 180)]
        ) + "</g>"
    body += sash(c) + head(c)
    if item in ("mace", "bow", "spear"):
        arm_svg = held_item(item, hnd) + arm + hand(hnd)
    else:
        arm_svg = arm + hand(hnd) + (held_item(item, hnd) if item else "")
    b = boots(c)
    cut = b.index("<path", 1)
    trim = b.index("<path", cut + 1)
    parts = {"body": body, "arm": arm_svg, "bootB": b[:cut], "bootF": b[cut:trim]}
    defs = robe_clip(c)
    s = c.get("scale_x", 1.0)
    out = {}
    for name, inner in parts.items():
        g = f'<g {scale_x(s)}>{inner}</g>' if s != 1.0 else inner
        out[name] = svg(g, w=320, defs=defs, top=-30, left=-40)
    return out


CAST = {
    "siavosh": dict(id="siavosh", robe=LAPIS, pattern=GOLD_LIGHT, under=WHITE, sash=VERMILION,
                    headgear="kiani", beard=None, item="sword", pose="rest", cape=CRIMSON, boots=EARTH_DARK),
    "siavosh_white": dict(id="siavosh_white", robe=WHITE, pattern=PAPER_DARK, under=PAPER, sash=GOLD, trim=GOLD,
                          headgear="kiani", item=None, pose="hip"),
    "siavosh_strike": dict(id="siavosh_strike", robe=LAPIS, pattern=GOLD_LIGHT, under=WHITE, sash=VERMILION,
                           headgear="kiani", item="sword_up", pose="raise", cape=CRIMSON, boots=EARTH_DARK),
    "turan_archer": dict(id="turan_archer", robe=PLUM, pattern=None, under=STEEL, sash=SAFFRON,
                         headgear="turan", hat=INK, fur=EARTH, beard="pointed", item="bow", pose="raise", locks=False),
    "rostam": dict(id="rostam", robe=SAFFRON, stripes=True, under=LEAF, sash=LEAF_DARK, headgear="leopard",
                   beard="forked", item="mace", pose="rest", scale_x=1.22, boots=EARTH_DARK),
    "kavus": dict(id="kavus", robe=CRIMSON, pattern=GOLD, under=GOLD_LIGHT, sash=GOLD, headgear="royal",
                  beard="long", beard_color="#3A2A22", item="cup", pose="rest", cape=LAPIS_DARK),
    "sudabeh": dict(id="sudabeh", robe=VERMILION, pattern=GOLD_LIGHT, under=ROSE, sash=PLUM, headgear="queen",
                    veil=ROSE, jewel=TURQUOISE, hem=402, flare=10, item="flower", pose="raise", boots=CRIMSON),
    "farangis": dict(id="farangis", robe=TURQUOISE, pattern=WHITE, under=LAPIS, sash=GOLD, headgear="queen",
                     veil=WHITE, jewel=LAPIS, hem=402, flare=10, item=None, pose="hip", boots=LAPIS_DARK),
    "afrasiab": dict(id="afrasiab", robe=PLUM, pattern=GOLD_DARK, under=STEEL, sash=GOLD, headgear="turan",
                     hat=CRIMSON, fur=EARTH, beard="pointed", item="sword", pose="raise", armor=STEEL_DARK, cape=INK),
    "garsivaz": dict(id="garsivaz", robe=LEAF_DARK, pattern=SAFFRON, under=PLUM, sash=PLUM, headgear="turan",
                     hat=LEAF, fur=INK, beard="pointed", item=None, pose="hip"),
    "piran": dict(id="piran", robe=LILAC, pattern=WHITE, under=PAPER, sash=TURQUOISE, headgear="turban",
                  beard="long", beard_color=BEARD_WHITE, hair=BEARD_WHITE, item="staff", pose="rest"),
    "naqqal": dict(id="naqqal", robe=EARTH, pattern=None, under=PAPER, sash=INK, headgear="cap", hat=INK,
                   beard="short", beard_color="#3A2A22", item="staff", pose="raise", locks=False, cape=LAPIS_DARK),
    "trainee": dict(id="trainee", robe=LEAF, pattern=None, under=PAPER, sash=SAFFRON, headgear="turban",
                    beard="short", item="wood_sword", pose="rest", locks=False, boots=EARTH_DARK),
    "mother": dict(id="mother", robe=ROSE, pattern=WHITE, under=PLUM, sash=GOLD, headgear="queen", veil=WHITE,
                   jewel=VERMILION, hem=402, flare=10, item=None, pose="hip", boots=PLUM),
    "tus": dict(id="tus", robe=LAPIS_DARK, pattern=None, under=STEEL, sash=GOLD, headgear="helmet",
                beard="long", item="spear", pose="rest", armor=STEEL, cape=VERMILION),
    "giv": dict(id="giv", robe=CRIMSON, pattern=None, under=STEEL, sash=SAFFRON, headgear="helmet",
                beard="pointed", item="bow", pose="raise", armor=STEEL_DARK, cape=LEAF_DARK),
    "turan_soldier": dict(id="turan_soldier", robe=EARTH_DARK, pattern=None, under=STEEL, sash=CRIMSON,
                          headgear="helmet", beard="short", item="spear", pose="rest", armor=STEEL_DARK, locks=False),
}


if __name__ == "__main__":
    import os
    import sys

    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    for name, c in CAST.items():
        with open(os.path.join(out, f"{name}.svg"), "w") as f:
            f.write(figure(c))
    print("wrote", len(CAST), "figures")
