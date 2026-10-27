"""Genera Audio/REVIEW.md a partir de picks.py, build_report.json y los metadatos de Freesound."""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from picks import PICKS, TOP10  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
report = json.load(open(os.path.join(ROOT, "Tools", "build_report.json"), encoding="utf-8"))
meta = json.load(open(os.path.join(ROOT, "Source", "_raw", "freesound", "meta.json"), encoding="utf-8"))

CATS = [("Music", "Música"), ("Stingers", "Stingers"), ("Ambience", "Ambientes"), ("Ball", "Pelota"),
        ("Score", "Encestada"), ("Mechanics", "Mecánicas"), ("UI", "UI")]


def origin(src):
    typ, ref = src
    if typ == "fs":
        m = meta[str(ref)]
        return f"[{m['name']}]({m['url']}) de {m['username']}"
    return f"Kenney `{ref.split('/')[-1]}`"


def file_links(files):
    return "<br>".join(f"[`{os.path.basename(f['file'])}`]({f['file']}) · {f['dur']:.2f} s" for f in files)


def origins(files):
    seen = []
    for f in files:
        o = origin(f["source"])
        if o not in seen:
            seen.append(o)
    return "<br>".join(seen)


lines = [
    "# Revisión de audio",
    "",
    "Elegí cada sonido por descripción, tags, duración y calificación en Freesound: **no los escuché**.",
    "Marcá cada fila con ✅ (aprobado), 🔁 (usar la alternativa) o ❌ (buscar otro) y anotá lo que quieras en *Comentario*.",
    "",
    "- Archivos procesados: `Audio/Source/<Categoría>/`. Alternativas: `Audio/Source/_alt/<sonido>/`.",
    "- Formato: WAV 48 kHz 16 bit; mono para SFX y UI, estéreo para música y ambientes.",
    "- Niveles: SFX y UI a −1 dBTP de pico; música, stingers y ambientes a −16 LUFS (las capas de `mus_endless` a −20 LUFS, el shaker por pico). "
    "Como los SFX están normalizados por pico, el volumen percibido varía bastante entre ellos: el balance final se hace en Wwise.",
    "- Todo es CC0 salvo que se indique lo contrario (ver [CREDITS.md](CREDITS.md)).",
    "- Para regenerar después de cambiar una elección: editar `Audio/Tools/picks.py` y correr `python Audio/Tools/build_audio.py <sonido>`.",
    "",
    "## Los 10 más importantes (con alternativa)",
    "",
    "| Estado | Sonido | Principal | Alternativa | Comentario |",
    "|---|---|---|---|---|",
]
for t in TOP10:
    names = [p["name"] for p in PICKS if p["name"] == t or p["name"].startswith(t + "_")]
    for n in names:
        r = report[n]
        if not r.get("alt") or not r["alt"]["files"]:
            continue
        lines.append(f"| ⬜ | **{n}** | {file_links(r['main']['files'])}<br>{origins(r['main']['files'])} | "
                     f"{file_links(r['alt']['files'])}<br>{origins(r['alt']['files'])} | |")
lines += ["", "Notas sobre las alternativas:", ""]
for t in TOP10:
    for p in PICKS:
        if (p["name"] == t or p["name"].startswith(t + "_")) and p.get("alt"):
            lines.append(f"- **{p['name']}**: {p['note']}")

lines += ["", "## Lista completa", ""]
for cat, title in CATS:
    lines += [f"### {title}", "", "| Estado | Sonido | Archivos | Origen | Por qué / notas | Comentario |", "|---|---|---|---|---|---|"]
    for p in PICKS:
        if p["cat"] != cat:
            continue
        r = report[p["name"]]
        warns = r["main"].get("warnings", [])
        note = p.get("note", "") + ("".join(f"<br>⚠ {w}" for w in warns))
        lines.append(f"| ⬜ | **{p['name']}** | {file_links(r['main']['files'])} | {origins(r['main']['files'])} | {note} | |")
    lines.append("")

lines += [
    "## Pendientes y decisiones",
    "",
    "- **mus_endless**: las 3 capas son loops de autores distintos, todos de 8 compases exactos a 90 BPM, así que van a estar en tempo. "
    "La batería y el shaker no tienen tonalidad, pero el lead está en Mi mayor (la alternativa en Re menor). "
    "Escuchalas juntas en [`_preview/mus_endless_mix.wav`](Source/_preview/mus_endless_mix.wav) "
    "(y con el lead alternativo en [`mus_endless_mix_alt.wav`](Source/_preview/mus_endless_mix_alt.wav)); son 2 vueltas para oír el empalme. "
    "Si no pegan, dejo el lead como placeholder hasta encontrar otro.",
    "- **mus_endless_lead**: el loop original no empalma perfecto, así que le puse un micro-fade de 2 ms. Escuchá el cierre del loop.",
    "- **mus_desafio / mus_contrarreloj**: no traían BPM; los estimé (120 y ~88,8) y corté los loops en compases exactos. Escuchá el empalme.",
    "- **stg_nuevo_record**: dura 1,44 s, un poco menos que los 1,5 s pedidos (la alternativa dura 2,07 s).",
    "- **amb_desierto**: viento seco sin insectos; no encontré grillos o cigarras que pegaran. Si querés insectos, busco una capa más.",
    "- **amb_plaza**: tiene parque y chicos, pero el tráfico se oye poco.",
    "- **ball_air, hoop_move_loop, carousel_loop**: son loops cortos armados con crossfade; escuchalos en repetición.",
    "- Las fuentes son previews HQ de Freesound (OGG ~192 kbps); en el juego se recomprimen igual a Vorbis/ADPCM.",
    "",
]
open(os.path.join(ROOT, "REVIEW.md"), "w", encoding="utf-8").write("\n".join(lines))
print("REVIEW.md escrito")
