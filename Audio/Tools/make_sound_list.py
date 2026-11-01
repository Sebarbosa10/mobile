"""Genera Audio/LISTA_SONIDOS.md: qué hace cada sonido, qué buscar y qué suena ahora."""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from picks import PICKS  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
report = json.load(open(os.path.join(ROOT, "Tools", "build_report.json"), encoding="utf-8"))
meta = json.load(open(os.path.join(ROOT, "Source", "_raw", "freesound", "meta.json"), encoding="utf-8"))

CATS = [("Music", "Música"), ("Stingers", "Stingers"), ("Ambience", "Ambientes"), ("Ball", "Pelota"),
        ("Score", "Encestada"), ("Mechanics", "Mecánicas"), ("UI", "Interfaz")]

# nombre: (cuándo suena, qué buscar, duración, formato)
INFO = {
    "mus_menu": ("Menú principal, en loop.", "Hip hop instrumental callejero, relajado pero con groove, sin piano.", "20–60 s, loop", "estéreo"),
    "mus_endless_base": ("Modo infinito, siempre. Capa 1 de 3.", "Batería boom bap o hip hop.", "21,33 s (8 compases a 90 BPM)", "estéreo"),
    "mus_endless_perc": ("Modo infinito, entra entre 4 y 10 puntos. Capa 2 de 3.", "Shaker, hi-hats o percusión sin bombo.", "21,33 s (8 compases a 90 BPM)", "estéreo"),
    "mus_endless_lead": ("Modo infinito, entra entre 14 y 25 puntos. Capa 3 de 3.", "Bajo o melodía simple que encaje sobre la batería.", "21,33 s (8 compases a 90 BPM)", "estéreo"),
    "mus_desafio": ("Desafíos del carrusel y del aro esquivo, en loop.", "Beat electrónico o hip hop con tensión, enérgico.", "30–90 s, loop", "estéreo"),
    "mus_contrarreloj": ("Desafío contrarreloj, en loop.", "Tema rápido y urgente, más intenso que el de desafío.", "30–90 s, loop", "estéreo"),
    "stg_nuevo_record": ("Fin de partida con récord nuevo.", "Fanfarria corta de triunfo, arcade pero no infantil.", "1,5–3 s", "estéreo"),
    "stg_victoria": ("Desafío ganado.", "Jingle corto de victoria.", "1,5–3 s", "estéreo"),
    "stg_derrota": ("Desafío perdido.", "Jingle corto de derrota, sin burla.", "1,5–3 s", "estéreo"),
    "stg_game_over": ("Fin de partida sin récord.", "Jingle corto de game over.", "1,5–3 s", "estéreo"),
    "amb_plaza": ("Fondo Plaza, en loop.", "Plaza al atardecer: pájaros, tráfico lejano, chicos a lo lejos.", "30–60 s, loop", "estéreo"),
    "amb_azotea": ("Fondo Rooftop, en loop.", "Ciudad de noche desde lo alto, zumbido de neón, viento.", "30–60 s, loop", "estéreo"),
    "amb_playa": ("Fondo Beach, en loop.", "Olas y gaviotas.", "30–60 s, loop", "estéreo"),
    "amb_nevado": ("Fondo Snowy, en loop.", "Viento frío, sin tormenta exagerada.", "30–60 s, loop", "estéreo"),
    "amb_desierto": ("Fondo Desert, en loop.", "Viento seco con insectos.", "30–60 s, loop", "estéreo"),
    "amb_espacio": ("Fondo Space, en loop.", "Drone grave y estable.", "30–60 s, loop", "estéreo"),
    "ball_grab": ("Al agarrar la pelota con el dedo.", "Manos atrapando una pelota de básquet.", "0,1–0,3 s", "mono"),
    "ball_throw": ("Al lanzar. Más fuerte y agudo con un swipe fuerte.", "Whoosh corto de lanzamiento.", "0,2–0,6 s", "mono"),
    "ball_air": ("Mientras la pelota vuela, en loop.", "Aire suave y parejo, sin ataque ni final.", "0,5–2 s, loop", "mono"),
    "ball_bounce_floor": ("Cuando la pelota toca el piso.", "Pique de pelota de básquet en cemento o cancha.", "0,2–0,5 s", "mono"),
    "ball_bounce_soft": ("Cuando toca algo que no es piso, aro, tablero ni poste.", "Pique apagado sobre superficie blanda.", "0,2–0,5 s", "mono"),
    "ball_hit_rim": ("Cuando pega en el aro.", "Pelota contra aro metálico, con resonancia corta.", "0,3–1,2 s", "mono"),
    "ball_hit_backboard": ("Cuando pega en el tablero.", "Pelota contra tablero de madera gastada.", "0,3–0,6 s", "mono"),
    "ball_hit_pole": ("Cuando pega en el poste.", "Pelota contra poste de metal, golpe sordo.", "0,2–0,6 s", "mono"),
    "ball_respawn": ("Cuando la pelota vuelve a su lugar.", "Pop suave de aparición.", "0,2–0,5 s", "mono"),
    "ball_shake_color": ("Al sacudir el celular para cambiar el color.", "Destello o brillo corto.", "0,5–1,2 s", "mono"),
    "net_swish_chain": ("En cada encestada.", "Red de CADENA metálica recibiendo la pelota, sin voces de fondo.", "0,5–1,2 s", "mono"),
    "score_clean": ("Encestada limpia, sin tocar aro ni tablero.", "Ding brillante, el premio más alto.", "0,2–0,5 s", "mono"),
    "score_rim": ("Encestada que tocó el aro antes.", "Campana metálica corta.", "0,2–0,5 s", "mono"),
    "score_backboard": ("Encestada que tocó el tablero antes.", "Golpe de madera o pluck positivo.", "0,1–0,4 s", "mono"),
    "score_plus_one": ("En cada encestada. Sube un semitono por racha, hasta 10.", "Blip corto y afinado que aguante subir de tono.", "0,1–0,3 s", "mono"),
    "score_flash": ("En cada encestada, junto al destello visual.", "Barrido ascendente o brillo corto.", "0,2–0,5 s", "mono"),
    "miss_out": ("Cuando la pelota cae sin encestar.", "Sonido suave de fallo, sin castigar.", "0,1–0,4 s", "mono"),
    "timer_tick": ("Cada segundo del timer, hasta los últimos 3.", "Tic seco de reloj.", "0,05–0,2 s", "mono"),
    "timer_urgent": ("Cada segundo de los últimos 3.", "Beep o tic urgente.", "0,1–0,3 s", "mono"),
    "timer_reset": ("Cuando el timer se recarga al encestar.", "Subida corta y positiva.", "0,2–0,5 s", "mono"),
    "timer_out": ("Cuando se acaba el tiempo.", "Buzzer real de cancha de básquet.", "1–2 s", "mono"),
    "hoop_shrink": ("Cuando el aro se achica.", "Bajada de tono corta.", "0,5–1,5 s", "mono"),
    "hoop_move_loop": ("Mientras el aro se mueve, desde 2 puntos, en loop.", "Zumbido suave de máquina, parejo y sin agudos.", "2–5 s, loop", "mono"),
    "carousel_loop": ("Mientras gira el carrusel, en loop.", "Mecanismo rotando, suave y regular.", "1–3 s, loop", "mono"),
    "carousel_hoop_done": ("Al encestar en un aro nuevo del carrusel. Sube de tono con cada aro.", "Ding corto y afinado.", "0,2–0,6 s", "mono"),
    "evasive_dash": ("Cuando el aro esquivo cambia de dirección.", "Whoosh rápido y liviano.", "0,1–0,4 s", "mono"),
    "challenge_start": ("Al empezar un desafío.", "Silbato de árbitro.", "0,3–1 s", "mono"),
    "ui_tap": ("Botones secundarios.", "Click corto y neutro.", "0,03–0,1 s", "mono"),
    "ui_tap_primary": ("Botones principales.", "Click con más cuerpo o confirmación.", "0,1–0,3 s", "mono"),
    "ui_back": ("Botones VOLVER y MENÚ.", "Click descendente.", "0,05–0,2 s", "mono"),
    "ui_disabled": ("Botones deshabilitados y tarjetas bloqueadas.", "Golpe apagado de error, suave.", "0,1–0,2 s", "mono"),
    "ui_tab": ("Cambio de pestaña en Personalizar.", "Click liviano.", "0,05–0,1 s", "mono"),
    "ui_select": ("Elegir una tarjeta de skin, fondo o desafío.", "Tic de selección.", "0,05–0,2 s", "mono"),
    "ui_equip": ("Al equipar una skin o un fondo.", "Clic satisfactorio de colocar algo.", "0,1–0,3 s", "mono"),
    "ui_pause_in": ("Al abrir la pausa.", "Barrido corto descendente.", "0,1–0,3 s", "mono"),
    "ui_pause_out": ("Al cerrar la pausa.", "Barrido corto ascendente.", "0,1–0,3 s", "mono"),
    "ui_panel_in": ("Cuando aparece el panel de resultados.", "Whoosh suave de entrada.", "0,2–0,4 s", "mono"),
    "ui_count_up": ("Cada paso del contador de puntaje, hasta 20 veces seguidas.", "Tic muy corto que no canse al repetirse.", "0,03–0,08 s", "mono"),
    "ui_progress_fill": ("Al abrir Desafíos con encestadas acumuladas.", "Barra llenándose, subida corta.", "0,3–0,6 s", "mono"),
    "ui_unlock": ("Cuando se desbloquea una skin o un fondo.", "Destello de logro, más especial que el resto.", "0,8–1,5 s", "mono"),
    "ui_confetti": ("Al salir el confeti de récord nuevo.", "Lanzapapeles.", "0,4–0,8 s", "mono"),
    "ui_onboarding": ("Cuando aparece la ayuda de cómo lanzar.", "Aviso suave.", "0,1–0,4 s", "mono"),
}

NOTES = {
    "mus_menu": "🔴 El piano suena mal.",
    "amb_plaza": "🟠 Se pisaba con el piano.",
    "net_swish_chain": "🟠 Una variación puede traer voces; otra dura 0,14 s.",
    "ball_throw": "🟡 Una variación dura solo 0,18 s.",
    "hoop_shrink": "⚪ Hoy no suena: todos los niveles del aro tienen la misma escala.",
    "amb_desierto": "🟡 No tiene insectos.",
    "stg_nuevo_record": "🟡 Dura 1,44 s, un poco menos de lo pedido.",
    "hoop_move_loop": "🟡 Recién reemplazado, falta confirmarlo.",
}


def source_name(src):
    typ, ref = src
    if typ == "fs":
        m = meta[str(ref)]
        return f"[{m['name']}]({m['url']})"
    return f"Kenney `{ref.split('/')[-1]}`"


def current(files):
    seen = []
    for f in files:
        name = source_name(f["source"])
        if name not in seen:
            seen.append(name)
    return "<br>".join(seen)


missing = [p["name"] for p in PICKS if p["name"] not in INFO]
if missing:
    raise SystemExit(f"Faltan descripciones: {missing}")

lines = [
    "# Lista de sonidos",
    "",
    f"Son {len(PICKS)} sonidos. Marcá ☑ cuando encuentres el reemplazo.",
    "",
    "## Por dónde empezar",
    "",
    "🔴 cambiar primero · 🟠 revisar pronto · 🟡 mejorable · ⚪ puede esperar",
    "",
    "Los que más se escuchan en cada tiro son `ball_throw`, `ball_bounce_floor`, `ball_hit_rim`, "
    "`net_swish_chain`, `score_plus_one` y `timer_tick`.",
    "",
    "## Qué conviene buscar",
    "",
    "- **Licencia:** CC0, o CC-BY si anotás el crédito.",
    "- **Calidad:** WAV o FLAC de 44,1 o 48 kHz, si puede ser. Yo lo convierto a 48 kHz y 16 bit.",
    "- **Sonido:** lo más limpio posible, sin voces ni música de fondo, salvo que se pida.",
    "- **Variaciones:** la columna *Var.* indica cuántas versiones usa el juego. Sirven varias tomas de una misma grabación.",
    "- **Loops:** tienen que sonar parejos. Si la grabación es más larga, yo le armo el empalme.",
    "- **Capas de `mus_endless`:** las tres tienen que ser del mismo tempo y duración, y la melodía tiene que combinar con el resto.",
    "",
    "## Cómo pasármelos",
    "",
    "Mandame el link de Freesound o dejá el archivo en cualquier carpeta y decime a qué sonido corresponde. "
    "Yo lo recorto, lo normalizo, lo importo a Wwise y regenero los bancos.",
    "",
]

for cat, title in CATS:
    lines += [f"## {title}", "",
              "| ☐ | Sonido | Var. | Cuándo suena | Qué buscar | Duración | Canales | Suena ahora | Nota |",
              "|---|---|---|---|---|---|---|---|---|"]
    for p in PICKS:
        if p["cat"] != cat:
            continue
        name = p["name"]
        files = report[name]["main"]["files"]
        when, want, dur, ch = INFO[name]
        lines.append(f"| ☐ | `{name}` | {len(files)} | {when} | {want} | {dur} | {ch} | {current(files)} | {NOTES.get(name, '')} |")
    lines.append("")

path = os.path.join(ROOT, "LISTA_SONIDOS.md")
open(path, "w", encoding="utf-8").write("\n".join(lines))
print("Escrito", path, "|", len(PICKS), "sonidos,", sum(len(report[p["name"]]["main"]["files"]) for p in PICKS), "archivos")
