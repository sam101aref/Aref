#!/usr/bin/env python3
"""Builds an installable Android APK of Sam Nariman without the Android SDK.

The game is HTML5, so the app is a full-screen WebView that loads it from the APK's assets.
Only three small Java tools are needed, all from Maven Central: the stub android.jar (to compile
MainActivity), dx (to make classes.dex) and apksig (to sign). The binary AndroidManifest.xml and the
one-icon resources.arsc that aapt2 would normally produce are written here directly.

Usage:  python3 Sam/android/build.py            -> Sam/android/build/sam-nariman.apk
Needs:  python3, a JDK (javac, java, keytool), internet access to Maven Central and Google Fonts.

Signing key: set SAM_KEYSTORE (a .p12 file) and SAM_KEYSTORE_PASS to sign with your own key.
Otherwise a key is created once in Sam/android/.cache/ and reused. Keep that file: Android only
installs an update over the old app when both are signed with the same key.
"""
import os
import re
import shutil
import struct
import subprocess
import sys
import urllib.request
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.join(HERE, '..', 'game')
CACHE = os.path.join(HERE, '.cache')
BUILD = os.path.join(HERE, 'build')
OUT = os.path.join(BUILD, 'sam-nariman.apk')

PACKAGE = 'ir.aref.samnariman'
VERSION_CODE = 1
VERSION_NAME = '0.1'
LABEL = 'سام نریمان'
MIN_SDK, TARGET_SDK = 24, 34

MAVEN = ['https://repo1.maven.org/maven2', 'https://maven-central.storage.googleapis.com/maven2']
TOOLS = {
    'android.jar': 'com/google/android/android/4.1.1.4/android-4.1.1.4.jar',
    'dx.jar': 'com/jakewharton/android/repackaged/dalvik-dx/16.0.1/dalvik-dx-16.0.1.jar',
    'apksig.jar': 'com/android/tools/build/apksig/2.3.0/apksig-2.3.0.jar',
}
FONTS_CSS = 'https://fonts.googleapis.com/css2?family=Lalezar&family=Vazirmatn:wght@400;700&display=swap'
BROWSER_UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36'


def run(*cmd):
    subprocess.run(cmd, check=True)


def fetch(url, ua=None):
    req = urllib.request.Request(url, headers={'User-Agent': ua or 'sam-nariman-build'})
    with urllib.request.urlopen(req, timeout=60) as r:
        return r.read()


def tool(name):
    path = os.path.join(CACHE, name)
    if not os.path.exists(path):
        last = None
        for base in MAVEN:
            try:
                data = fetch(base + '/' + TOOLS[name])
                break
            except Exception as e:  # try the next mirror
                last = e
        else:
            raise SystemExit(f'could not download {name}: {last}')
        with open(path, 'wb') as f:
            f.write(data)
    return path


# ------------------------------------------------------------------ binary XML (AndroidManifest.xml)

ANDROID_NS = 'http://schemas.android.com/apk/res/android'
ATTR_IDS = {
    'theme': 0x01010000, 'label': 0x01010001, 'icon': 0x01010002, 'name': 0x01010003,
    'exported': 0x01010010, 'screenOrientation': 0x0101001e, 'configChanges': 0x0101001f,
    'minSdkVersion': 0x0101020c, 'versionCode': 0x0101021b, 'versionName': 0x0101021c,
    'targetSdkVersion': 0x01010270, 'hardwareAccelerated': 0x010102d3,
}
T_REF, T_STRING, T_INT, T_BOOL = 0x01, 0x03, 0x10, 0x12


def string_pool(strings):
    """A UTF-16 ResStringPool chunk."""
    offsets, data = [], b''
    for s in strings:
        offsets.append(len(data))
        u = s.encode('utf-16-le')
        data += struct.pack('<H', len(u) // 2) + u + b'\0\0'
    data += b'\0' * (-len(data) % 4)
    start = 28 + 4 * len(strings)
    body = b''.join(struct.pack('<I', o) for o in offsets) + data
    return struct.pack('<HHIIIIII', 0x0001, 28, 28 + len(body), len(strings), 0, 0, start, 0) + body


def manifest():
    # (tag, [(android-attr?, name, type, value)], children)
    E = lambda tag, attrs, kids=(): (tag, attrs, list(kids))
    tree = E('manifest', [(False, 'package', T_STRING, PACKAGE), (True, 'versionCode', T_INT, VERSION_CODE),
                          (True, 'versionName', T_STRING, VERSION_NAME)], [
        E('uses-sdk', [(True, 'minSdkVersion', T_INT, MIN_SDK), (True, 'targetSdkVersion', T_INT, TARGET_SDK)]),
        E('application', [(True, 'label', T_STRING, LABEL), (True, 'icon', T_REF, 0x7f010000),
                          (True, 'hardwareAccelerated', T_BOOL, True)], [
            E('activity', [(True, 'name', T_STRING, PACKAGE + '.MainActivity'), (True, 'exported', T_BOOL, True),
                           (True, 'screenOrientation', T_INT, 6),       # sensorLandscape
                           (True, 'configChanges', T_INT, 0x04a0)], [  # orientation|keyboardHidden|screenSize
                E('intent-filter', [], [
                    E('action', [(True, 'name', T_STRING, 'android.intent.action.MAIN')]),
                    E('category', [(True, 'name', T_STRING, 'android.intent.category.LAUNCHER')]),
                ]),
            ]),
        ]),
    ])

    # attribute names that carry a resource id come first, in the same order as the resource map
    ids = sorted({n for n in ATTR_IDS}, key=lambda n: ATTR_IDS[n])
    strings = list(ids)

    def idx(s):
        if s not in strings:
            strings.append(s)
        return strings.index(s)

    for s in ('android', ANDROID_NS):
        idx(s)

    def walk(node):
        tag, attrs, kids = node
        idx(tag)
        for is_android, name, typ, val in attrs:
            idx(name)
            if typ == T_STRING:
                idx(val)
        for k in kids:
            walk(k)
    walk(tree)

    body = b''
    ns, line = idx(ANDROID_NS), [1]

    def node_chunk(typ, size, extra):
        c = struct.pack('<HHIIi', typ, 16, size, line[0], -1) + extra
        line[0] += 1
        return c

    def emit(node):
        nonlocal body
        tag, attrs, kids = node
        attrs = sorted(attrs, key=lambda a: ATTR_IDS.get(a[1], 0xffffffff))
        ab = b''
        for is_android, name, typ, val in attrs:
            raw = idx(val) if typ == T_STRING else -1
            data = idx(val) if typ == T_STRING else (0xffffffff if val else 0) if typ == T_BOOL else val
            ab += struct.pack('<iiiHBBI', ns if is_android else -1, idx(name), raw, 8, 0, typ, data)
        body += node_chunk(0x0102, 36 + len(ab), struct.pack('<iiHHHHHH', -1, idx(tag), 20, 20, len(attrs), 0, 0, 0) + ab)
        for k in kids:
            emit(k)
        body += node_chunk(0x0103, 24, struct.pack('<ii', -1, idx(tag)))

    body += node_chunk(0x0100, 24, struct.pack('<ii', idx('android'), ns))
    emit(tree)
    body += node_chunk(0x0101, 24, struct.pack('<ii', idx('android'), ns))

    resmap = struct.pack('<HHI', 0x0180, 8, 8 + 4 * len(ids)) + b''.join(struct.pack('<I', ATTR_IDS[n]) for n in ids)
    content = string_pool(strings) + resmap + body
    return struct.pack('<HHI', 0x0003, 8, 8 + len(content)) + content


# ------------------------------------------------------------------ resources.arsc with one drawable

def resources(icon_path):
    """Resource table holding one entry: drawable/icon (0x7f010000) -> res/drawable/icon.png."""
    types, keys = string_pool(['drawable']), string_pool(['icon'])
    spec = struct.pack('<HHIBBHI', 0x0202, 16, 20, 1, 0, 0, 1) + struct.pack('<I', 0)
    config = struct.pack('<I', 64) + b'\0' * 60
    entry = struct.pack('<HHI', 8, 0, 0) + struct.pack('<HBBI', 8, 0, T_STRING, 0)
    header_size = 20 + len(config)
    typ = struct.pack('<HHIBBHII', 0x0201, header_size, header_size + 4 + len(entry), 1, 0, 0, 1, header_size + 4) + config
    typ += struct.pack('<I', 0) + entry
    name = PACKAGE.encode('utf-16-le').ljust(256, b'\0')
    head = 288
    pkg_body = types + keys + spec + typ
    pkg = struct.pack('<HHII', 0x0200, head, head + len(pkg_body), 0x7f) + name
    pkg += struct.pack('<IIIII', head, 1, head + len(types), 1, 0) + pkg_body
    values = string_pool([icon_path])
    content = values + pkg
    return struct.pack('<HHII', 0x0002, 12, 12 + len(content), 1) + content


# ------------------------------------------------------------------ assets

def bundle_fonts(assets):
    """Downloads the two web fonts so the app works offline; returns the <link> to put in the page."""
    try:
        css = fetch(FONTS_CSS, BROWSER_UA).decode()
        os.makedirs(os.path.join(assets, 'fonts'), exist_ok=True)
        for url in sorted(set(re.findall(r'url\((https://[^)]+)\)', css))):
            name = url.rsplit('/', 2)[-2] + '-' + url.rsplit('/', 1)[-1]
            with open(os.path.join(assets, 'fonts', name), 'wb') as f:
                f.write(fetch(url, BROWSER_UA))
            css = css.replace(url, 'fonts/' + name)
        with open(os.path.join(assets, 'fonts.css'), 'w') as f:
            f.write(css)
        return '<link href="fonts.css" rel="stylesheet">'
    except Exception as e:
        print('fonts not bundled, the app will load them online:', e)
        return None


def build_assets(assets):
    shutil.copytree(GAME, assets)
    link = bundle_fonts(assets)
    page = os.path.join(assets, 'index.html')
    html = open(page, encoding='utf-8').read()
    if link:
        html = re.sub(r'<link rel="preconnect"[^>]*>\n', '', html)
        html = re.sub(r'<link href="https://fonts.googleapis.com[^>]*>', link, html)
    open(page, 'w', encoding='utf-8').write(html)


# ------------------------------------------------------------------ build

def main():
    os.makedirs(CACHE, exist_ok=True)
    shutil.rmtree(BUILD, ignore_errors=True)
    os.makedirs(BUILD)
    android_jar, dx_jar, apksig_jar = tool('android.jar'), tool('dx.jar'), tool('apksig.jar')

    classes = os.path.join(BUILD, 'classes')
    stubs = os.path.join(BUILD, 'stubs')
    run('javac', '-nowarn', '--release', '8', '-d', stubs, os.path.join(HERE, 'stubs/android/webkit/JavascriptInterface.java'))
    run('javac', '-nowarn', '--release', '8', '-cp', android_jar + os.pathsep + stubs, '-d', classes,
        os.path.join(HERE, 'src/ir/aref/samnariman/MainActivity.java'))
    dex = os.path.join(BUILD, 'classes.dex')
    run('java', '-cp', dx_jar, 'com.android.dx.command.Main', '--dex', f'--min-sdk-version={MIN_SDK}', '--output=' + dex, classes)

    assets = os.path.join(BUILD, 'assets')
    build_assets(assets)

    unsigned = os.path.join(BUILD, 'unsigned.apk')
    with zipfile.ZipFile(unsigned, 'w') as z:
        # resources.arsc goes first and uncompressed: its data then starts 4-byte aligned, as Android 11+ requires
        z.writestr(zipfile.ZipInfo('resources.arsc', (2024, 1, 1, 0, 0, 0)), resources('res/drawable/icon.png'), zipfile.ZIP_STORED)
        z.writestr('AndroidManifest.xml', manifest(), zipfile.ZIP_DEFLATED)
        z.write(dex, 'classes.dex', zipfile.ZIP_DEFLATED)
        z.write(os.path.join(HERE, 'icon.png'), 'res/drawable/icon.png', zipfile.ZIP_STORED)
        for root, _, files in os.walk(assets):
            for f in sorted(files):
                p = os.path.join(root, f)
                z.write(p, 'assets/' + os.path.relpath(p, assets).replace(os.sep, '/'), zipfile.ZIP_DEFLATED)

    keystore = os.environ.get('SAM_KEYSTORE') or os.path.join(CACHE, 'sam-nariman.p12')
    password = os.environ.get('SAM_KEYSTORE_PASS', 'samnariman')
    if not os.path.exists(keystore):
        run('keytool', '-genkeypair', '-keystore', keystore, '-storetype', 'PKCS12', '-storepass', password,
            '-alias', 'sam', '-keyalg', 'RSA', '-keysize', '2048', '-validity', '10000',
            '-dname', 'CN=Sam Nariman, O=Aref')
    signer = os.path.join(BUILD, 'signer')
    run('javac', '-nowarn', '-cp', apksig_jar, '-d', signer, os.path.join(HERE, 'Sign.java'))
    # apksig 2.3.0 (the newest on Maven Central) touches a JDK-internal class even when v1 signing is off
    run('java', '--add-exports=java.base/sun.security.x509=ALL-UNNAMED', '--add-exports=java.base/sun.security.util=ALL-UNNAMED',
        '-cp', signer + os.pathsep + apksig_jar, 'Sign', keystore, password, unsigned, OUT)
    print(f'built {os.path.relpath(OUT)} ({os.path.getsize(OUT) // 1024} KB)')


if __name__ == '__main__':
    sys.exit(main())
