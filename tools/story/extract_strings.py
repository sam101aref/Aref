#!/usr/bin/env python3
"""Builds Resources/Localization/Story.json from Editor/Content/StoryScript.json.

The story script holds every title, dialogue line and cutscene caption in English and Persian.
The game looks text up by key; the keys follow the same scheme as StoryContent.cs:

  chapter.{n}.title                     chapter titles
  level.{id}.title / .intro / .hint     level banner texts
  dlg.{id}.{i}  dlg.{id}o.{i}           dialogue before / after a level
  dlg.{id}w{w}.{i}                      dialogue before wave w
  cut.{cutscene}.title  cut.{cutscene}.{i}
      cutscene ids: prologue, ch{n}, ending, lv{id} (before a level), lv{id}o (after a level)

Run from the repository root:  python3 tools/story/extract_strings.py
"""
import json
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SCRIPT = os.path.join(ROOT, 'Assets/_Project/Editor/Content/StoryScript.json')
OUT = os.path.join(ROOT, 'Assets/_Project/Resources/Localization/Story.json')


def main():
    with open(SCRIPT, encoding='utf-8') as f:
        script = json.load(f)
    entries = []
    seen = set()

    def add(key, text):
        if not text or not (text.get('en') or text.get('fa')):
            return
        if key in seen:
            sys.exit('Duplicate key: ' + key)
        seen.add(key)
        entries.append({'key': key, 'en': text.get('en', ''), 'fa': text.get('fa', '')})

    def lines(prefix, items):
        for i, line in enumerate(items or [], 1):
            add('%s.%d' % (prefix, i), line)

    def cutscene(cid, data):
        if not data or not data.get('panels'):
            return
        add('cut.%s.title' % cid, data.get('title'))
        for i, panel in enumerate(data['panels'], 1):
            add('cut.%s.%d' % (cid, i), panel.get('text'))

    for chapter in script['chapters']:
        n = chapter['number']
        add('chapter.%d.title' % n, chapter.get('title'))
        cutscene('prologue' if n == 0 else 'ch%d' % n, chapter.get('intro'))
        for level in chapter.get('levels', []):
            lid = level['id']
            add('level.%s.title' % lid, level.get('title'))
            add('level.%s.intro' % lid, level.get('intro'))
            add('level.%s.hint' % lid, level.get('hint'))
            lines('dlg.%s' % lid, level.get('introDialogue'))
            lines('dlg.%so' % lid, level.get('outroDialogue'))
            for w, wave in enumerate(level.get('waves', []), 1):
                lines('dlg.%sw%d' % (lid, w), wave.get('dialogue'))
            cutscene('lv%s' % lid, level.get('cutscene'))
            cutscene('lv%so' % lid, level.get('outro'))
    cutscene('ending', script.get('ending'))

    with open(OUT, 'w', encoding='utf-8') as f:
        f.write('{\n  "entries": [\n')
        f.write(',\n'.join('    ' + json.dumps(e, ensure_ascii=False) for e in entries))
        f.write('\n  ]\n}\n')
    print('Wrote %d entries to %s' % (len(entries), os.path.relpath(OUT, ROOT)))


if __name__ == '__main__':
    main()
