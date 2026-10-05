# Art generator

The game's sprites are drawn as SVG in code and rendered to PNG with headless Chromium.

```
node tools/art/render.mjs            # render all sprites into Assets/_Project/Resources/Art
node tools/art/render.mjs --preview  # contact sheets in tools/art/preview (not committed)
```

| File | Contents |
|---|---|
| `characters.mjs` | Every character as body parts (legs, torso, detail, head, hat, cape, back, arm, weapon); helmets and armour items |
| `backdrops.mjs` | Seven biomes, three tileable layers each (far, mid, ground) |
| `props.mjs` | Projectiles, battle props (shields, platforms, covers, horse), cutscene scenery, effects |
| `ui.mjs` | 9-slice frame, panel and buttons, bars, icons, girih pattern |

Parts drawn in greys (torso, cape, arm, Arash's cap) are tinted in the game.
`Assets/_Project/Art/art-manifest.json` lists each sprite's pivot, pixels per unit and 9-slice
border; the editor applies it to the importers. Hand-drawn art can replace any PNG as long as it
keeps the same name, size ratio and pivot.
