"""Descarga y procesa los sonidos de picks.py.

Uso:  python Audio/Tools/build_audio.py [nombre ...]

- Baja las previews HQ de Freesound a Audio/Source/_raw/freesound/ (con metadatos en meta.json).
- Procesa cada sonido: recorte de silencios, fades de 5 ms, loops sin click, WAV 48 kHz 16 bit,
  mono para SFX/UI y estéreo para música/ambientes.
- Normaliza: SFX/UI a -1 dBTP de pico; música, stingers y ambientes a -16 LUFS integrados
  (bajando más si el pico verdadero pasaría de -1 dBTP).
- Escribe Audio/CREDITS.md y Audio/Tools/build_report.json.

Necesita ffmpeg en el PATH (o en FFMPEG) y FREESOUND_API_KEY (variable de entorno o registro de usuario).
"""
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

import numpy as np
import requests
import soundfile as sf

sys.path.insert(0, os.path.dirname(__file__))
from picks import PICKS, TOP10  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
RAW_FS = os.path.join(ROOT, "Source", "_raw", "freesound")
RAW_KN = os.path.join(ROOT, "Source", "_raw", "kenney")
OUT = os.path.join(ROOT, "Source")
SR = 48000
FADE = 0.005
SFX_PEAK_DBTP = -1.0
MUSIC_LUFS = -16.0

KENNEY_PACKS = {
    "kenney_interface-sounds": "https://kenney.nl/assets/interface-sounds",
    "kenney_ui-audio": "https://kenney.nl/assets/ui-audio",
    "kenney_digital-audio": "https://kenney.nl/assets/digital-audio",
    "kenney_impact-sounds": "https://kenney.nl/assets/impact-sounds",
}


def ffmpeg_bin():
    found = os.environ.get("FFMPEG") or shutil.which("ffmpeg")
    if found:
        return found
    base = os.path.expandvars(r"%LOCALAPPDATA%\Microsoft\WinGet\Packages")
    for dirpath, _, files in os.walk(base):
        if "ffmpeg.exe" in files:
            return os.path.join(dirpath, "ffmpeg.exe")
    sys.exit("No encuentro ffmpeg")


FFMPEG = ffmpeg_bin()


def api_key():
    key = os.environ.get("FREESOUND_API_KEY")
    if key:
        return key
    import winreg
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as h:
        return winreg.QueryValueEx(h, "FREESOUND_API_KEY")[0]


# ---------------------------------------------------------------- Freesound

def load_meta():
    path = os.path.join(RAW_FS, "meta.json")
    return json.load(open(path, encoding="utf-8")) if os.path.exists(path) else {}


def save_meta(meta):
    json.dump(meta, open(os.path.join(RAW_FS, "meta.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)


def fetch_fs(sid, meta):
    os.makedirs(RAW_FS, exist_ok=True)
    key = str(sid)
    if key not in meta:
        r = requests.get(f"https://freesound.org/apiv2/sounds/{sid}/",
                         params={"fields": "id,name,username,license,url,previews,bpm,duration,channels",
                                 "token": api_key()}, timeout=40)
        if not r.ok:
            raise RuntimeError(f"Freesound {sid}: HTTP {r.status_code}")
        meta[key] = r.json()
        save_meta(meta)
    path = os.path.join(RAW_FS, f"{sid}.ogg")
    if not os.path.exists(path):
        r = requests.get(meta[key]["previews"]["preview-hq-ogg"], timeout=120)
        r.raise_for_status()
        open(path, "wb").write(r.content)
    return path


# ---------------------------------------------------------------- audio utils

def decode(path, channels):
    cmd = [FFMPEG, "-v", "error", "-i", path, "-ac", str(channels), "-ar", str(SR), "-f", "f32le", "-"]
    raw = subprocess.run(cmd, capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.float32).reshape(-1, channels).astype(np.float64)


def db(x):
    return 20 * np.log10(max(x, 1e-12))


def envelope(x, win=0.010, hop=0.0025):
    mono = np.abs(x).max(axis=1)
    w, h = int(win * SR), int(hop * SR)
    n = max(1, (len(mono) - w) // h + 1)
    idx = np.arange(n) * h
    env = np.array([np.sqrt(np.mean(mono[i:i + w] ** 2)) for i in idx])
    return env, idx


def fade(y, fin=FADE, fout=FADE):
    y = y.copy()
    a, b = int(fin * SR), int(fout * SR)
    if a:
        y[:a] *= np.linspace(0, 1, a)[:, None]
    if b:
        y[-b:] *= np.linspace(1, 0, b)[:, None]
    return y


def trim(x, thr_db=-50.0, tail=0.02):
    peak = np.abs(x).max()
    if peak <= 0:
        return x
    above = np.where(np.abs(x).max(axis=1) > peak * 10 ** (thr_db / 20))[0]
    start = max(0, above[0] - int(FADE * SR))
    end = min(len(x), above[-1] + int(tail * SR))
    return x[start:end]


def slice_hits(x, n, maxlen):
    # Onset = la envolvente sube más de 6 dB en 20 ms (sirve aunque el golpe anterior siga resonando)
    env, idx = envelope(x)
    on = env.max() * 10 ** (-30 / 20)
    look, gap = 8, int(0.12 / 0.0025)
    hits = [0] if env[0] > on else []
    for i in range(1, len(env)):
        prev = env[max(0, i - look):i].min()
        if env[i] > on and env[i] > 2 * prev and (not hits or i - hits[-1] >= gap):
            hits.append(i)
    segs = []
    for k, i in enumerate(hits):
        s = max(0, idx[i] - int(0.01 * SR))
        nxt = idx[hits[k + 1]] - int(0.01 * SR) if k + 1 < len(hits) else len(x)
        e = min(nxt, s + int(maxlen * SR))
        segs.append((np.abs(x[s:e]).max(), s, e))
    best = sorted(segs, reverse=True)[:n]
    return [x[s:e] for _, s, e in sorted(best, key=lambda t: t[1])]


def equal_power(n):
    t = np.linspace(0, 1, n)[:, None]
    return np.sqrt(1 - t), np.sqrt(t)


def make_loop(x, length, xfade):
    """Loop de `length` muestras: la cola posterior se mezcla sobre el inicio, así el
    final empalma con el principio sin click."""
    xf = min(xfade, max(0, len(x) - length))
    y = x[:length].copy()
    if xf >= int(0.002 * SR):
        fo, fi = equal_power(xf)
        y[:xf] = x[:xf] * fi + x[length:length + xf] * fo
        return y, False
    # Sin cola: si el loop original ya empalma (salto chico entre última y primera muestra) se deja
    # intacto; si no, micro-fades de 2 ms (pequeño valle en el empalme).
    if np.abs(y[-1] - y[0]).max() < 0.02 * np.abs(y).max():
        return y, False
    return fade(y, 0.002, 0.002), True


def first_onset(x, thr_db=-40.0):
    peak = np.abs(x).max()
    above = np.where(np.abs(x).max(axis=1) > peak * 10 ** (thr_db / 20))[0]
    return int(above[0]) if len(above) else 0


def measure(y):
    """(LUFS integrados, pico verdadero dBTP) medidos con ffmpeg ebur128."""
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as f:
        tmp = f.name
    sf.write(tmp, y, SR, subtype="FLOAT")
    try:
        # ebur128 necesita al menos 400 ms: rellenar con silencio no cambia el pico
        pad = ["-af", "apad=whole_dur=0.5,ebur128=peak=true:framelog=quiet"]
        err = subprocess.run([FFMPEG, "-nostats", "-i", tmp] + pad + ["-f", "null", "-"],
                             capture_output=True, text=True).stderr
    finally:
        os.remove(tmp)
    summary = err[err.rfind("Summary:"):]
    i = re.search(r"I:\s+(-?[\d.]+|-inf) LUFS", summary)
    p = re.search(r"True peak:\s+Peak:\s+(-?[\d.]+|-inf) dBFS", summary)
    lufs = float(i.group(1)) if i and i.group(1) != "-inf" else -70.0
    tp = float(p.group(1)) if p and p.group(1) != "-inf" else -70.0
    return lufs, tp


def limit(y, ceiling_db=-2.0, loop=False):
    """Limitador de ffmpeg. En loops se procesa con 1 s del final antepuesto, para que el
    estado del limitador en el empalme sea el mismo que en el resto."""
    pre = min(len(y), SR) if loop else 0
    src = np.concatenate([y[-pre:], y]) if pre else y
    with tempfile.TemporaryDirectory() as d:
        a, b = os.path.join(d, "a.wav"), os.path.join(d, "b.wav")
        sf.write(a, src, SR, subtype="FLOAT")
        lim = 10 ** (ceiling_db / 20)
        subprocess.run([FFMPEG, "-v", "error", "-y", "-i", a, "-af",
                        f"alimiter=limit={lim:.4f}:attack=5:release=80:level=false",
                        "-c:a", "pcm_f32le", b], check=True)
        out, _ = sf.read(b, always_2d=True)
    out = out[pre:pre + len(y)]
    return np.pad(out, ((0, len(y) - len(out)), (0, 0)))


def normalize(y, kind, loop=False, target=MUSIC_LUFS):
    lufs, tp = measure(y)
    note = None
    if kind in ("sfx", "ui"):
        gain = SFX_PEAK_DBTP - tp
        y = y * 10 ** (gain / 20)
    else:
        gain = target - lufs
        y = y * 10 ** (gain / 20)
        if tp + gain > SFX_PEAK_DBTP:
            y = limit(y, loop=loop)
            note = f"limitador aplicado ({tp + gain - SFX_PEAK_DBTP:.1f} dB de reducción en picos)"
            _, tp2 = measure(y)
            if tp2 > SFX_PEAK_DBTP:
                y = y * 10 ** ((SFX_PEAK_DBTP - tp2) / 20)
    lufs2, tp2 = measure(y)
    if kind not in ("sfx", "ui") and lufs2 < target - 1:
        note = (note + "; " if note else "") + f"queda en {lufs2:.1f} LUFS"
    return np.clip(y, -1, 1), dict(lufs=round(lufs2, 1), dbtp=round(tp2, 1), gain_db=round(gain, 1), note=note)


# ---------------------------------------------------------------- pipeline

def source_path(src, meta):
    typ, ref, _ = src
    return fetch_fs(ref, meta) if typ == "fs" else os.path.join(RAW_KN, ref)


def process_source(src, kind, meta):
    typ, ref, o = src
    ch = 1 if kind in ("sfx", "ui") else 2
    x = decode(source_path(src, meta), ch)
    warnings = []
    if o.get("offset"):
        x = x[int(o["offset"] * SR):]

    if "layers" in o:
        for lid, gain in o["layers"]:
            lay = decode(fetch_fs(lid, meta), ch) * 10 ** (gain / 20)
            reps = int(np.ceil(len(x) / len(lay)))
            lay = np.tile(lay, (reps, 1))[:len(x)]
            x = x + lay

    if "bpm" in o or (kind == "music" and meta.get(str(ref), {}).get("bpm")):
        bpm = o.get("bpm") or meta[str(ref)]["bpm"]
        start = first_onset(x)
        if start < int(0.03 * SR):
            start = 0
        x = x[start:]
        bar = 4 * 60 / bpm * SR
        bars = o.get("bars") or max(4, int((len(x) - 0.05 * SR) / bar) // 4 * 4)
        length = int(round(bars * bar))
        if length > len(x):
            if length - len(x) <= int(0.005 * SR):  # diferencia de redondeo: rellenar con silencio
                x = np.pad(x, ((0, length - len(x)), (0, 0)))
            else:
                warnings.append(f"el audio dura menos que {bars} compases a {bpm} BPM")
                length = len(x)
        y, short = make_loop(x, length, int(0.05 * SR))
        if short:
            warnings.append("loop sin cola: empalme con micro-fade, revisar el cierre")
        if o.get("tile"):
            y = np.tile(y, (o["tile"], 1))
        return [y], warnings, dict(bpm=bpm, bars=bars * o.get("tile", 1))

    if "loop_len" in o or kind in ("amb", "music"):
        x = trim(x, -60)
        xf = int(o.get("xfade", 2.0) * SR)
        length = int(o.get("loop_len", 60) * SR)
        if length + xf > len(x):
            length = len(x) - xf
            warnings.append(f"loop acortado a {length / SR:.1f} s por la duración de la fuente")
        y, short = make_loop(x, length, xf)
        if short:
            warnings.append("loop sin cola: revisar el cierre")
        return [y], warnings, {}

    maxlen = o.get("maxlen", 3.0 if kind == "stinger" else 2.0)
    if o.get("slice"):
        parts = slice_hits(x, o["slice"], maxlen)
        if len(parts) < o["slice"]:
            warnings.append(f"solo se encontraron {len(parts)} golpes de {o['slice']}")
    else:
        parts = [x]
    outs = []
    for p in parts:
        p = trim(p)
        if len(p) > maxlen * SR:
            p = p[:int(maxlen * SR)]
            p = fade(p, FADE, 0.04)
        else:
            p = fade(p)
        outs.append(p)
    return outs, warnings, {}


def build(entry, variant, meta):
    srcs = entry.get(variant)
    if not srcs:
        return None
    clips, warns, info, origins = [], [], {}, []
    for src in srcs:
        outs, w, i = process_source(src, entry["kind"], meta)
        clips += outs
        warns += w
        info.update(i)
        origins += [src] * len(outs)
    folder = os.path.join(OUT, entry["cat"]) if variant == "main" else os.path.join(OUT, "_alt", entry["name"])
    os.makedirs(folder, exist_ok=True)
    # Borra salidas anteriores de este sonido (por si cambió la cantidad de variaciones)
    for old in os.listdir(folder):
        if re.fullmatch(re.escape(entry["name"]) + r"(_\d\d)?\.wav", old):
            os.remove(os.path.join(folder, old))
    files = []
    for k, (clip, src) in enumerate(zip(clips, origins)):
        fname = entry["name"] + (f"_{k + 1:02d}" if len(clips) > 1 else "") + ".wav"
        y, stats = normalize(clip, "sfx" if entry.get("norm") == "peak" else entry["kind"], loop=entry["kind"] in ("music", "amb"),
                             target=entry.get("lufs", MUSIC_LUFS))
        if stats["note"] and "LUFS" in stats["note"]:
            warns.append(f"{fname}: {stats['note']}")
        dur = len(y) / SR
        if entry["kind"] == "stinger" and not 1.5 <= dur <= 3.0:
            warns.append(f"{fname}: dura {dur:.2f} s (pedido: 1,5–3 s)")
        sf.write(os.path.join(folder, fname), y, SR, subtype="PCM_16")
        files.append(dict(file=os.path.relpath(os.path.join(folder, fname), ROOT).replace("\\", "/"),
                          dur=round(dur, 2), channels=y.shape[1], source=list(src[:2]), **stats))
    return dict(files=files, warnings=warns, **info)


def credit_rows(report, meta):
    rows = []
    for name, r in report.items():
        for variant in ("main", "alt"):
            if not r.get(variant):
                continue
            for f in r[variant]["files"]:
                typ, ref = f["source"]
                if typ == "fs":
                    m = meta[str(ref)]
                    lic = "CC0 1.0" if "zero" in m["license"] else ("CC-BY 4.0" if "/by/4" in m["license"] else m["license"])
                    rows.append((f["file"], m["url"], m["username"], lic))
                    for lid, _ in next(s for s in PICK_BY_NAME[name][variant] if s[1] == ref)[2].get("layers", []):
                        lm = meta[str(lid)]
                        rows.append((f["file"] + " (capa)", lm["url"], lm["username"],
                                     "CC0 1.0" if "zero" in lm["license"] else lm["license"]))
                else:
                    pack = ref.split("/")[0]
                    rows.append((f["file"], f"{KENNEY_PACKS[pack]} ({ref.split('/')[-1]})", "Kenney (kenney.nl)", "CC0 1.0"))
    return rows


def write_credits(report, meta):
    rows = credit_rows(report, meta)
    by = [r for r in rows if "BY" in r[3]]
    lines = ["# Créditos de audio", "",
             "Todos los sonidos son de Freesound (previews HQ) o de los packs de Kenney.",
             "CC0 no exige atribución; se registra igual para poder rastrear el origen.", ""]
    if by:
        lines += ["## Requieren atribución (CC-BY)", ""] + [f"- `{r[0]}`: \"{r[1]}\" de {r[2]}, {r[3]}" for r in by] + [""]
    lines += ["## Registro completo", "", "| Archivo final | Origen | Autor | Licencia |", "|---|---|---|---|"]
    lines += [f"| `{r[0]}` | {r[1]} | {r[2]} | {r[3]} |" for r in rows]
    open(os.path.join(ROOT, "CREDITS.md"), "w", encoding="utf-8").write("\n".join(lines) + "\n")


PICK_BY_NAME = {p["name"]: p for p in PICKS}


def main(only):
    meta = load_meta()
    report_path = os.path.join(ROOT, "Tools", "build_report.json")
    report = json.load(open(report_path, encoding="utf-8")) if os.path.exists(report_path) else {}
    for p in PICKS:
        if only and p["name"] not in only:
            continue
        r = dict(cat=p["cat"], kind=p["kind"], note=p.get("note", ""), top10=any(p["name"].startswith(t) for t in TOP10))
        for variant in ("main", "alt"):
            try:
                r[variant] = build(p, variant, meta)
            except Exception as e:  # sigue con el resto
                r[variant] = dict(files=[], warnings=[f"ERROR: {e}"])
        report[p["name"]] = r
        n = len(r["main"]["files"]) if r.get("main") else 0
        w = (r["main"] or {}).get("warnings", []) + ((r.get("alt") or {}).get("warnings", []))
        print(f"{p['name']}: {n} archivo(s)" + (f"  ⚠ {'; '.join(w)}" if w else ""), flush=True)
    json.dump(report, open(report_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    write_credits(report, meta)


if __name__ == "__main__":
    main(set(sys.argv[1:]))
