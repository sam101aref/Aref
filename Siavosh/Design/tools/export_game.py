"""Renders all game art from the SVG sources into PNG sprites for Unity, plus a manifest.

    python3 tools/export_game.py            (from Siavosh/Design; needs node + playwright)

Output goes to Siavosh/Assets/_Project/Resources/Art. Sprites are trimmed to their drawn area;
the manifest records, for every sprite, its size and where its centre sits relative to its rig's
origin (feet for people, the ground under the belly for horses, bottom-centre for props), all in
world units (1 unit = 200 px). Rig joints (shoulders, hips) are listed as anchors.
"""

import json
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(__file__))

import figures  # noqa: E402
import horse  # noqa: E402
import props  # noqa: E402
import scenes  # noqa: E402
from palette import *  # noqa: E402,F401,F403

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Art")
ICON_OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Icon.png")
PPU = 200.0

# World units per SVG unit for each rig.
FIGURE_SCALE = 0.0062
HORSE_SCALE = 0.0115
FIGURE_ORIGIN = (120.0, 410.0)
HORSE_ORIGIN = (200.0, 232.0)
SHOULDER = (146.0, 112.0)

jobs = []
anchors = {}


def inner(svg_text):
    """Defs + body of a full <svg> document."""
    start = svg_text.index(">", svg_text.index("<svg")) + 1
    return svg_text[start:svg_text.rindex("</svg>")]


def job(name, content, scale, origin=None, world=None, trim=True, size=None, kind="art"):
    jobs.append(dict(name=name, content=content, scale=scale, origin=origin, world=world, trim=trim, size=size, kind=kind))


# ---------------------------------------------------------------- people

FULL = ["siavosh", "siavosh_white", "rostam", "kavus", "sudabeh", "farangis", "afrasiab", "garsivaz", "piran",
        "naqqal", "turan_soldier", "turan_archer", "trainee", "mother", "tus", "giv"]
for name in FULL:
    job(f"chars/{name}", inner(figures.figure(figures.CAST[name])), 2.0, origin=FIGURE_ORIGIN, world=2.0 / PPU)

RIGS = {
    # rig name -> (cast entry, {arm variant: (item, pose)})
    "siavosh": ("siavosh", {"arm": ("sword", "rest"), "armbow": ("bow", "raise")}),
    "rostam": ("rostam", {"arm": ("mace", "rest")}),
    "naqqal": ("naqqal", {"arm": ("staff", "raise")}),
    "soldier": ("turan_soldier", {"arm": ("spear", "rest")}),
    "archer": ("turan_archer", {"arm": ("bow", "raise")}),
    "trainee": ("trainee", {"arm": ("wood_sword", "rest")}),
}
for rig, (cast, arms) in RIGS.items():
    base = figures.CAST[cast]
    parts = figures.figure_parts(base)
    for part in ("body", "bootB", "bootF"):
        job(f"parts/{rig}_{part}", inner(parts[part]), FIGURE_SCALE * PPU, origin=FIGURE_ORIGIN, world=FIGURE_SCALE)
    for variant, (item, pose) in arms.items():
        arm = figures.figure_parts(dict(base, item=item, pose=pose))["arm"]
        job(f"parts/{rig}_{variant}", inner(arm), FIGURE_SCALE * PPU, origin=FIGURE_ORIGIN, world=FIGURE_SCALE)
    sx = base.get("scale_x", 1.0)
    shoulder = (FIGURE_ORIGIN[0] + (SHOULDER[0] - FIGURE_ORIGIN[0]) * sx, SHOULDER[1])
    anchors[rig] = {"shoulder": [round((shoulder[0] - FIGURE_ORIGIN[0]) * FIGURE_SCALE, 4),
                                 round((FIGURE_ORIGIN[1] - shoulder[1]) * FIGURE_SCALE, 4)]}

# ---------------------------------------------------------------- horses

def horse_rig(rig, **kw):
    hp = horse.horse(parts=True, **kw)
    for part, frag in hp.items():
        job(f"parts/{rig}_{part}", frag, HORSE_SCALE * PPU, origin=HORSE_ORIGIN, world=HORSE_SCALE)
    legs = {"legFF": horse.LEG_FF, "legHF": horse.LEG_HF, "legFN": horse.LEG_FN, "legHN": horse.LEG_HN}
    anchors[rig] = {k: [round((v[0][0] - HORSE_ORIGIN[0]) * HORSE_SCALE, 4), round((HORSE_ORIGIN[1] - v[0][1]) * HORSE_SCALE, 4)]
                    for k, v in legs.items()}


horse_rig("shabrang")
horse_rig("onager", body=horse.ONAGER, shine=horse.ONAGER_SHINE, tack=False)
for variant, cast in (("rider", "siavosh"), ("riderwhite", "siavosh_white")):
    k, cx, cy = 1.3, 214, 120
    reins = (cx + (352 - cx) / k, cy + (96 - cy) / k)
    rider_cast = dict(figures.CAST[cast], cape=figures.CAST["siavosh"]["cape"] if variant == "rider" else None)
    frag = f'<g transform="translate({cx} {cy}) scale({k}) translate({-cx} {-cy})">{horse.rider(rider_cast, reins)}</g>'
    job(f"parts/shabrang_{variant}", frag, HORSE_SCALE * PPU, origin=HORSE_ORIGIN, world=HORSE_SCALE)
anchors["shabrang"]["rider"] = [round((214 - HORSE_ORIGIN[0]) * HORSE_SCALE, 4), round((HORSE_ORIGIN[1] - 120) * HORSE_SCALE, 4)]
# horse.shabrang() draws at 1.4x with 50 units of head room; its origin is the ground under the belly.
RIDE_ORIGIN = (HORSE_ORIGIN[0] * 1.4, (HORSE_ORIGIN[1] + 50) * 1.4)
job("chars/siavosh_riding", inner(horse.shabrang(figures.CAST["siavosh"])), 1.6, origin=RIDE_ORIGIN, world=1.6 / PPU)
job("chars/siavosh_riding_white", inner(horse.shabrang(dict(figures.CAST["siavosh_white"], cape=None))), 1.6, origin=RIDE_ORIGIN, world=1.6 / PPU)
job("chars/shabrang", inner(horse.shabrang()), 1.6, origin=RIDE_ORIGIN, world=1.6 / PPU)

# ---------------------------------------------------------------- props, icons, backgrounds

for name, (builder, size) in props.PROPS.items():
    job(f"props/{name}", builder(), None, world=("height", size), kind="prop")
for name in props.ICON_PATHS:
    job(f"icons/{name}", props.icon(name), 128 / 24.0, trim=False, size=(24, 24))
for name, builder in scenes.SCENES.items():
    job(f"bg/{name[3:]}", inner(builder()), 2304 / 1920.0, trim=False, size=(1920, 864))
job("tiles/ground_meadow", props.ground_tile("meadow"), 1.0, trim=False, size=(512, 320))
job("tiles/ground_earth", props.ground_tile("earth"), 1.0, trim=False, size=(512, 320))

# ---------------------------------------------------------------- titles (rendered text)

NASTALIQ = "'Noto Nastaliq Urdu'"
CINZEL = "Cinzel"
TITLES = {
    "logo_fa": ("شاهزاده سیاوش", NASTALIQ, 150),
    "logo_en": ("SIAVOSH THE PRINCE", CINZEL, 96),
    "ch0_fa": ("پیش‌درآمد", NASTALIQ, 110), "ch0_en": ("Prologue", CINZEL, 84),
    "ch1_fa": ("شاگرد رستم", NASTALIQ, 110), "ch1_en": ("Rostam's Pupil", CINZEL, 84),
    "ch2_fa": ("بازگشت به پایتخت", NASTALIQ, 110), "ch2_en": ("Return to the Capital", CINZEL, 84),
    "ch3_fa": ("گذر از آتش", NASTALIQ, 110), "ch3_en": ("Through the Fire", CINZEL, 84),
    "ch4_fa": ("جنگ بلخ", NASTALIQ, 110), "ch4_en": ("The War at Balkh", CINZEL, 84),
    "ch5_fa": ("مهمان توران", NASTALIQ, 110), "ch5_en": ("Guest of Turan", CINZEL, 84),
    "ch6_fa": ("سیاوش‌گرد", NASTALIQ, 110), "ch6_en": ("Siavoshgerd", CINZEL, 84),
    "ch7_fa": ("نیرنگ", NASTALIQ, 110), "ch7_en": ("The Deceit", CINZEL, 84),
    "ch8_fa": ("کیخسرو", NASTALIQ, 110), "ch8_en": ("Kay Khosrow", CINZEL, 84),
    "no_fa": ("نه، جوان…", NASTALIQ, 96), "no_en": ("No, young one…", CINZEL, 72),
    "end_fa": ("پایان فصل", NASTALIQ, 110), "end_en": ("End of the Chapter", CINZEL, 84),
}
for name, (text, family, size) in TITLES.items():
    jobs.append(dict(name=f"titles/{name}", text=text, family=family, fontsize=size, kind="text"))

# ---------------------------------------------------------------- render


def app_icon():
    face = figures.figure(dict(figures.CAST["siavosh"], item=None))
    body = inner(face)
    content = (
        f'<circle cx="256" cy="256" r="256" fill="{LAPIS}"/>'
        f'<circle cx="256" cy="256" r="236" fill="none" stroke="{GOLD}" stroke-width="14"/>'
        f'<circle cx="256" cy="256" r="214" fill="none" stroke="{GOLD_LIGHT}" stroke-width="3" stroke-dasharray="6 8"/>'
        f'<g transform="translate(256 300) scale(4.2) translate(-124 -64)">{body}</g>'
    )
    return content


def font_css():
    """Google Fonts CSS with the font files inlined, so Chromium needs no network of its own."""
    import base64
    import urllib.request

    cache = os.path.join(HERE, ".fonts.css")
    if os.path.exists(cache):
        return open(cache).read()
    ua = {"User-Agent": "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/120 Safari/537.36"}
    url = "https://fonts.googleapis.com/css2?family=Noto+Nastaliq+Urdu:wght@700&family=Cinzel:wght@800&display=block"
    css = urllib.request.urlopen(urllib.request.Request(url, headers=ua)).read().decode()
    for font_url in set(re.findall(r"url\((https://[^)]+)\)", css)):
        data = urllib.request.urlopen(urllib.request.Request(font_url, headers=ua)).read()
        css = css.replace(font_url, "data:font/woff2;base64," + base64.b64encode(data).decode())
    open(cache, "w").write(css)
    return css


def main():
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(HERE, ".fonts.css.tmp"), "w") as f:
        f.write(font_css())
    jobs.append(dict(name="__icon__", content=app_icon(), scale=1.0, trim=False, size=(512, 512), kind="icon", out=ICON_OUT))
    payload = []
    for j in jobs:
        out = j.get("out") or os.path.join(OUT, j["name"] + ".png")
        os.makedirs(os.path.dirname(out), exist_ok=True)
        payload.append(dict(j, out=out))
    path = os.path.join(HERE, ".jobs.json")
    with open(path, "w") as f:
        json.dump(payload, f, ensure_ascii=False)
    env = dict(os.environ, NODE_PATH=os.environ.get("NODE_PATH", "/opt/node22/lib/node_modules"))
    result = subprocess.run(["node", os.path.join(HERE, "render.js"), path, os.path.join(HERE, ".fonts.css.tmp")],
                            env=env, capture_output=True, text=True)
    os.remove(os.path.join(HERE, ".fonts.css.tmp"))
    if result.returncode != 0:
        print(result.stdout, result.stderr)
        raise SystemExit(1)
    boxes = json.loads(result.stdout)
    os.remove(path)

    sprites = []
    for j in payload:
        if j["kind"] in ("icon",):
            continue
        b = boxes[j["name"]]
        entry = {"name": j["name"], "w": round(b["pw"] / PPU, 4), "h": round(b["ph"] / PPU, 4)}
        if j.get("origin"):
            ox, oy = j["origin"]
            ws = j["world"]
            entry["x"] = round((b["x"] + b["w"] / 2 - ox) * ws, 4)
            entry["y"] = round((oy - (b["y"] + b["h"] / 2)) * ws, 4)
        elif j["kind"] == "prop":
            entry["x"] = 0.0
            entry["y"] = round(entry["h"] / 2, 4)
        sprites.append(entry)
    manifest = {"pixelsPerUnit": PPU, "sprites": sprites,
                "rigs": [{"name": k, "anchors": [{"name": a, "x": v[0], "y": v[1]} for a, v in d.items()]} for k, d in anchors.items()]}
    with open(os.path.join(OUT, "manifest.json"), "w") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=1)
    print(f"exported {len(payload)} images, {len(sprites)} in the manifest")


if __name__ == "__main__":
    main()
