"""Shabrang, Siavosh's black horse, in the miniature 'flying gallop', with an
optional rider. Drawn in a 420x260 box and scaled up on output."""

from palette import *  # noqa: F401,F403
from figures import path, dots, svg

BLACK = "#17110F"
BLACK_SHINE = "#3B302B"


def leg(points, width_top, width_bottom, color):
    """A tapered leg along a polyline; the hoof sits at the last point."""
    import math

    left, right = [], []
    n = len(points)
    for i, (x, y) in enumerate(points):
        if i == 0:
            dx, dy = points[1][0] - x, points[1][1] - y
        elif i == n - 1:
            dx, dy = x - points[i - 1][0], y - points[i - 1][1]
        else:
            dx, dy = points[i + 1][0] - points[i - 1][0], points[i + 1][1] - points[i - 1][1]
        ln = math.hypot(dx, dy) or 1
        nx, ny = -dy / ln, dx / ln
        w = (width_top + (width_bottom - width_top) * i / (n - 1)) / 2
        left.append((x + nx * w, y + ny * w))
        right.append((x - nx * w, y - ny * w))
    pts = left + right[::-1]
    d = "M" + " L".join(f"{x:.1f} {y:.1f}" for x, y in pts) + " Z"
    hx, hy = points[-1]
    px, py = points[-2]
    import math as m
    ang = m.degrees(m.atan2(hy - py, hx - px))
    hoof = f'<g transform="translate({hx:.1f},{hy:.1f}) rotate({ang:.1f})"><path d="M-3 -5 L7 -6 L9 6 L-3 5 Z" fill="{INK}"/></g>'
    return f'<path d="{d}" fill="{color}" stroke="{INK}" stroke-width="1.2" stroke-linejoin="round"/>' + hoof


def horse(caparison=CRIMSON, trim=GOLD, body=BLACK, shine=BLACK_SHINE, tack=True, parts=False):
    """The horse in the flying gallop. With parts=True returns {part: svg-fragment} with the four
    legs separate (pivot = first point of each leg) so they can swing."""
    P = {}
    # far legs first (a shade lighter so they read behind)
    P["legFF"] = leg(LEG_FF, 16, 7, shine)
    P["legHF"] = leg(LEG_HF, 18, 7, shine)
    b = ""
    # tail, knotted with a ribbon as in the manuscripts
    b += path("M106 134 Q86 124 74 136 Q64 148 50 146 Q30 142 22 158 Q40 152 52 160 Q36 170 30 186 Q50 172 66 166 Q82 158 92 150 Z", body)
    if tack:
        b += f'<path d="M78 134 L84 150" stroke="{caparison}" stroke-width="5"/>'
    b += path(
        "M108 128 Q118 112 150 116 Q196 126 244 114 Q262 112 272 122 Q296 140 292 170 "
        "Q288 192 268 196 Q220 204 160 198 Q128 196 116 182 Q98 162 108 128 Z",
        body,
    )
    b += path(
        "M244 118 Q262 70 312 50 Q322 46 330 52 Q346 60 362 84 Q376 98 378 106 Q372 114 360 112 "
        "Q348 108 338 100 Q324 96 318 112 Q302 136 292 160 Q276 126 244 118 Z",
        body,
    )
    b += path("M316 50 L310 28 L326 44 Z", body)
    b += f'<circle cx="340" cy="66" r="3.2" fill="{WHITE}"/><circle cx="341" cy="66" r="1.6" fill="{INK}"/>'
    b += f'<path d="M370 98 Q374 102 372 106" fill="none" stroke="{shine}" stroke-width="1.4"/>'
    mane = BLACK if body != BLACK else body
    for x, y in [(254, 104), (266, 88), (280, 74), (294, 62), (308, 54)]:
        b += path(f"M{x} {y + 10} Q{x - 16} {y - 2} {x - 22} {y + 14} Q{x - 10} {y + 8} {x} {y + 18} Z", mane)
    b += f'<path d="M150 124 Q196 132 240 122" fill="none" stroke="{shine}" stroke-width="3" stroke-linecap="round"/>'
    b += f'<path d="M116 150 Q118 172 134 184" fill="none" stroke="{shine}" stroke-width="3" stroke-linecap="round"/>'
    if tack:
        b += f'<path d="M322 54 Q346 76 366 104 M336 96 Q350 82 352 66 M318 60 L346 66" fill="none" stroke="{trim}" stroke-width="2.6"/>'
        b += f'<path d="M262 120 Q294 150 290 178" fill="none" stroke="{trim}" stroke-width="4"/>'
        b += dots([(272, 132), (282, 146), (288, 160), (289, 172)], 3, caparison)
        b += path("M168 120 Q200 126 240 118 L244 176 Q206 186 166 180 Z", caparison)
        b += f'<path d="M172 128 Q204 132 236 126 L238 170 Q206 178 172 174 Z" fill="none" stroke="{trim}" stroke-width="2.4"/>'
        b += dots([(180, 150), (204, 152), (226, 148), (192, 166), (216, 166)], 3, trim)
        for i, x in enumerate(range(170, 244, 9)):
            y = 181 + (2 if i % 2 else 0) - (x - 170) * 0.03
            b += f'<path d="M{x} {y - 2} L{x} {y + 8}" stroke="{trim}" stroke-width="2"/>'
        b += path("M188 118 Q200 104 214 116 Q224 106 234 112 L232 124 Q210 128 190 126 Z", EARTH_DARK)
    else:  # wild coat: pale belly and a dark stripe along the back
        b += path("M150 186 Q206 196 262 186 Q240 200 200 200 Q170 200 150 186 Z", PAPER)
        b += f'<path d="M150 120 Q200 130 246 118" fill="none" stroke="{EARTH_DARK}" stroke-width="5" stroke-linecap="round"/>'
    P["body"] = b
    P["legFN"] = leg(LEG_FN, 20, 7, body)
    P["legHN"] = leg(LEG_HN, 24, 7, body)
    if parts:
        return P
    return P["legFF"] + P["legHF"] + P["body"] + P["legFN"] + P["legHN"]


LEG_FF = [(262, 186), (288, 212), (314, 228), (328, 232)]
LEG_HF = [(132, 184), (108, 212), (80, 228), (66, 232)]
LEG_FN = [(276, 182), (306, 204), (334, 220), (350, 222)]
LEG_HN = [(120, 168), (100, 198), (70, 214), (54, 218)]
ONAGER = "#C99B63"
ONAGER_SHINE = "#A87B48"


def rider(c, reins_to=(352, 96)):
    """Siavosh seated, leaning into the gallop. Coordinates match horse()."""
    out = ""
    cape = c.get("cape")
    if cape:  # flying behind in the wind
        out += path("M200 66 Q160 60 120 74 Q96 82 80 76 Q100 92 128 90 Q100 104 86 104 Q126 112 170 96 Q196 90 206 82 Z", cape)
    # leg: thigh along the horse's side, boot in the stirrup
    out += path("M204 112 Q226 118 236 132 Q240 150 232 166 L222 162 Q228 146 222 134 Q212 126 200 124 Z", c["robe"])
    out += path("M222 160 L236 166 Q246 168 250 162 Q252 172 242 176 L220 172 Z", c.get("boots", EARTH_DARK))
    out += f'<path d="M230 140 L232 170" stroke="{GOLD}" stroke-width="1.6"/>'
    # torso and skirt of the robe
    out += path("M196 120 Q192 96 200 72 Q214 62 226 70 Q234 92 228 118 Q212 126 196 120 Z", c["robe"])
    if c.get("pattern"):
        out += dots([(206, 82), (218, 84), (204, 100), (220, 102), (212, 112)], 1.6, c["pattern"])
    out += path("M198 106 Q214 112 230 106 L230 114 Q214 120 198 114 Z", c.get("sash", VERMILION))
    out += f'<path d="M216 70 Q224 90 222 120" fill="none" stroke="{GOLD}" stroke-width="2"/>'
    # head
    out += path("M204 44 Q200 58 206 66 Q210 62 210 52 Z", HAIR)
    out += f'<rect x="210" y="56" width="8" height="10" fill="{SKIN_SHADE}" stroke="{INK}" stroke-width="1.2"/>'
    out += f'<ellipse cx="216" cy="46" rx="10" ry="12" fill="{SKIN}" stroke="{INK}" stroke-width="1.3"/>'
    out += f'<path d="M206 44 Q206 34 216 33 Q224 34 226 40 Q216 38 210 46 Z" fill="{HAIR}"/>'
    out += f'<path d="M218 44 Q221 42 224 44 Q221 45 218 44 Z" fill="{WHITE}" stroke="{INK}" stroke-width="0.9"/>'
    out += f'<circle cx="221.6" cy="43.9" r="1.1" fill="{INK}"/>'
    out += f'<path d="M217 40 Q221 37 225 39" fill="none" stroke="{INK}" stroke-width="1.1"/>'
    out += f'<path d="M226 43 L228 49 L225 50" fill="none" stroke="{INK}" stroke-width="1"/>'
    out += f'<path d="M221 53 Q223 54 225 53" fill="none" stroke="{VERMILION}" stroke-width="1.5"/>'
    # turban and small crown
    out += path("M204 38 Q204 26 216 24 Q228 26 228 36 Q216 32 204 38 Z", WHITE)
    out += path("M207 30 L208 20 L212 26 L216 16 L220 26 L224 20 L225 30 Q216 27 207 30 Z", GOLD)
    out += f'<circle cx="216" cy="22" r="1.6" fill="{VERMILION}"/>'
    out += f'<path d="M226 28 Q236 24 240 32" fill="none" stroke="{VERMILION}" stroke-width="2"/>'
    # arm holding the reins
    out += path("M218 74 Q236 84 248 92 Q254 96 252 100 Q246 102 240 98 Q228 92 212 84 Z", c["robe"])
    out += f'<circle cx="252" cy="96" r="4" fill="{SKIN}" stroke="{INK}" stroke-width="1.2"/>'
    out += f'<path d="M254 96 Q300 92 {reins_to[0]} {reins_to[1]}" fill="none" stroke="{GOLD_DARK}" stroke-width="1.6"/>'
    return out


def shabrang(with_rider=None, scale=1.4):
    w, h = int(420 * scale), int(250 * scale)
    body = horse()
    if with_rider:  # riders are drawn large, as in the manuscripts
        k, cx, cy = 1.3, 214, 120
        reins = (cx + (352 - cx) / k, cy + (96 - cy) / k)
        body += f'<g transform="translate({cx} {cy}) scale({k}) translate({-cx} {-cy})">{rider(with_rider, reins)}</g>'
    pad = 50  # head room for the rider's crown
    return svg(f'<g transform="scale({scale}) translate(0 {pad})">{body}</g>', w, h + int(pad * scale))


if __name__ == "__main__":
    import os
    import sys
    from figures import CAST

    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    open(os.path.join(out, "shabrang.svg"), "w").write(shabrang())
    open(os.path.join(out, "siavosh_riding.svg"), "w").write(shabrang(CAST["siavosh"]))
    open(os.path.join(out, "siavosh_riding_white.svg"), "w").write(shabrang(dict(CAST["siavosh_white"], cape=None)))
    print("wrote horses")
