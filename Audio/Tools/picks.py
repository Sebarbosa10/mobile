"""Selección de sonidos: qué fuente va a cada archivo final.

Fuentes:
  FS(id, **opts)      sonido de Freesound (se baja la preview HQ en OGG)
  KN(ruta, **opts)    archivo de un pack de Kenney en Audio/Source/_raw/kenney

Opciones de una fuente:
  slice=N        separar N golpes de una grabación con varios (se quedan los N más fuertes)
  offset=s       empezar a leer desde s segundos
  maxlen=s       cortar el one-shot a s segundos (con fade out)
  bpm=, bars=    loop musical de duración exacta (bars compases de 4/4)
  tile=N         repetir el loop N veces (para igualar la duración de otras capas)
  loop_len=s, xfade=s   loop no musical con crossfade de cierre
  layers=[(id, dB)]     capas extra mezcladas (ambientes)

Cada entrada: name, cat, kind (sfx | ui | music | stinger | amb), main, alt (opcional), note.
"""


def FS(sid, **o):
    return ("fs", sid, o)


def KN(path, **o):
    return ("kn", path, o)


IS = "kenney_interface-sounds/Audio/"
DA = "kenney_digital-audio/Audio/"
IM = "kenney_impact-sounds/Audio/"
UA = "kenney_ui-audio/Audio/"

PICKS = [
    # --- Música (loops) ---
    dict(name="mus_menu", cat="Music", kind="music",
         main=[FS(842606, bpm=85, bars=8)],
         note="Batería + piano, 85 BPM, tranquilo para el menú."),
    dict(name="mus_endless_base", cat="Music", kind="music", lufs=-20,
         main=[FS(629139, bpm=90, bars=8)],
         note="Capa 1 (base): batería boom bap 90 BPM, 8 compases."),
    dict(name="mus_endless_perc", cat="Music", kind="music", norm="peak",
         main=[FS(337239, bpm=90, bars=8)],
         note="Capa 2 (percusión): shaker 90 BPM, 8 compases."),
    dict(name="mus_endless_lead", cat="Music", kind="music", lufs=-20,
         main=[FS(412901, bpm=90, bars=8)],
         alt=[FS(265152, bpm=90, bars=2, tile=4)],
         note="Capa 3 (lead): loop de synth 90 BPM (Mi mayor). Alternativa: bajo reese en Re menor, más oscuro."),
    dict(name="mus_desafio", cat="Music", kind="music",
         main=[FS(722311, bpm=120, bars=48)],
         note="Electro beat de acción para los desafíos (120 BPM, loop de 48 compases)."),
    dict(name="mus_contrarreloj", cat="Music", kind="music",
         main=[FS(676998, bpm=88.84, bars=36)],
         note="Synth tenso para la contrarreloj (~88,8 BPM estimado, loop de 36 compases)."),

    # --- Stingers ---
    dict(name="stg_nuevo_record", cat="Stingers", kind="stinger",
         main=[FS(607207)], alt=[FS(521646)],
         note="Jingle arcade corto de felicitación. Alternativa: fanfarria con guitarra muteada."),
    dict(name="stg_victoria", cat="Stingers", kind="stinger",
         main=[FS(615099)], note="Jingle de victoria."),
    dict(name="stg_derrota", cat="Stingers", kind="stinger",
         main=[FS(135831)], note="Jingle de derrota."),
    dict(name="stg_game_over", cat="Stingers", kind="stinger",
         main=[FS(133283)], note="Jingle de game over."),

    # --- Ambientes ---
    dict(name="amb_plaza", cat="Ambience", kind="amb",
         main=[FS(268903, loop_len=48, xfade=2, layers=[(469613, -10)])],
         note="Parque urbano + chicos jugando a lo lejos (grabado en Argentina). Sin tráfico marcado."),
    dict(name="amb_azotea", cat="Ambience", kind="amb",
         main=[FS(427841, loop_len=60, xfade=2, layers=[(118340, -20)])],
         note="Ciudad de noche + zumbido de neón bajo."),
    dict(name="amb_playa", cat="Ambience", kind="amb",
         main=[FS(518637, loop_len=60, xfade=2)],
         note="Olas suaves, pájaros y gaviota."),
    dict(name="amb_nevado", cat="Ambience", kind="amb",
         main=[FS(117136, loop_len=50, xfade=2)],
         note="Viento ártico frío."),
    dict(name="amb_desierto", cat="Ambience", kind="amb",
         main=[FS(146436, loop_len=60, xfade=2)],
         note="Pasto seco con viento (Atacama). No tiene insectos."),
    dict(name="amb_espacio", cat="Ambience", kind="amb",
         main=[FS(686237, loop_len=60, xfade=2)],
         note="Drone grave de estación espacial."),

    # --- Pelota ---
    dict(name="ball_grab", cat="Ball", kind="sfx",
         main=[FS(514989)], note="Atrapada de pelota de goma con la mano."),
    dict(name="ball_throw", cat="Ball", kind="sfx",
         main=[FS(789840, maxlen=0.6), FS(394873, maxlen=0.6), FS(427823, maxlen=0.6)],
         alt=[FS(419341, maxlen=0.6), FS(719637, maxlen=0.6), FS(60030, maxlen=0.6)],
         note="Whooshes cortos. Alternativa: whooshes de bambú más agudos."),
    dict(name="ball_air", cat="Ball", kind="sfx",
         main=[FS(449990, offset=1.0, loop_len=0.8, xfade=0.2)],
         note="Loop corto sacado del medio de un whoosh largo y grave."),
    dict(name="ball_bounce_floor", cat="Ball", kind="sfx",
         main=[FS(319785, maxlen=0.5), FS(319762, maxlen=0.5), FS(319764, maxlen=0.5), FS(319783, maxlen=0.5)],
         note="4 piques de pelota de básquet reales, de la misma serie."),
    dict(name="ball_bounce_soft", cat="Ball", kind="sfx",
         main=[FS(451642, slice=3, maxlen=0.5)],
         note="3 piques sobre alfombra, más apagados."),
    dict(name="ball_hit_rim", cat="Ball", kind="sfx",
         main=[FS(406489, maxlen=1.2), FS(406490, maxlen=1.2), FS(244348, slice=2, maxlen=1.2)],
         alt=[FS(95275, slice=2, maxlen=1.2), FS(405665, maxlen=1.0), FS(815762, slice=1, maxlen=1.0)],
         note="2 golpes en el caño de un aro real + 2 golpes de un tiro errado al aro. Alternativa: anillos y piezas de metal sueltas, más agudos."),
    dict(name="ball_hit_backboard", cat="Ball", kind="sfx",
         main=[FS(324247, maxlen=0.6), KN(IM + "impactPlank_medium_000.ogg"), KN(IM + "impactWood_heavy_001.ogg")],
         note="Tablero real + 2 golpes de madera de Kenney para variar."),
    dict(name="ball_hit_pole", cat="Ball", kind="sfx",
         main=[FS(336000), KN(IM + "impactMetal_heavy_000.ogg")],
         note="Golpe en poste metálico + metal pesado de Kenney."),
    dict(name="ball_respawn", cat="Ball", kind="sfx",
         main=[FS(824189)], note="Pop de aparición."),
    dict(name="ball_shake_color", cat="Ball", kind="sfx",
         main=[FS(511485)], note="Destello mágico corto."),

    # --- Encestada ---
    dict(name="net_swish_chain", cat="Score", kind="sfx",
         main=[FS(788259, maxlen=1.2), FS(536737, maxlen=1.0), FS(235960, slice=1, maxlen=1.0)],
         alt=[FS(387240, slice=3, maxlen=1.0)],
         note="Red de cadena: tiro que entra y sacude el aro + cadenas cortas. Alternativa: 3 sacudidas de una misma cadena."),
    dict(name="score_clean", cat="Score", kind="sfx",
         main=[FS(328120)], note="Ding brillante (power up)."),
    dict(name="score_rim", cat="Score", kind="sfx",
         main=[FS(237105)], note="Campanita metálica corta."),
    dict(name="score_backboard", cat="Score", kind="sfx",
         main=[KN(IS + "pluck_001.ogg")], note="Pluck suave de Kenney."),
    dict(name="score_plus_one", cat="Score", kind="sfx",
         main=[FS(396193)], alt=[FS(350876)],
         note="Blip con sexta mayor, sube de tono con la racha. Alternativa: moneda 8-bit."),
    dict(name="score_flash", cat="Score", kind="sfx",
         main=[KN(DA + "phaserUp3.ogg")], note="Barrido ascendente de Kenney."),
    dict(name="miss_out", cat="Score", kind="sfx",
         main=[FS(405548)], note="Campana suave de error, sin castigar."),

    # --- Mecánicas ---
    dict(name="timer_tick", cat="Mechanics", kind="sfx",
         main=[FS(810938)], alt=[FS(509714)],
         note="Tic de reloj real. Alternativa: tic más seco."),
    dict(name="timer_urgent", cat="Mechanics", kind="sfx",
         main=[FS(391650)], note="Beep 8-bit corto para los últimos segundos."),
    dict(name="timer_reset", cat="Mechanics", kind="sfx",
         main=[KN(DA + "highUp.ogg")], note="Subida de tono de Kenney."),
    dict(name="timer_out", cat="Mechanics", kind="sfx",
         main=[FS(510026, maxlen=2.0)], alt=[FS(259290)],
         note="Buzzer deportivo real. Alternativa: buzzer más estridente."),
    dict(name="hoop_shrink", cat="Mechanics", kind="sfx",
         main=[FS(159399)], note="Power down."),
    dict(name="hoop_move_loop", cat="Mechanics", kind="sfx",
         main=[FS(587617, offset=2, loop_len=4, xfade=0.5)], alt=[FS(593862, offset=2, loop_len=4, xfade=0.5)],
         note="Zumbido de máquina grave y parejo (reemplaza al torno Dremel, que chillaba). Alternativa: drone eléctrico más grave."),
    dict(name="carousel_loop", cat="Mechanics", kind="sfx",
         main=[FS(340656, loop_len=1.0, xfade=0.2)], note="Loop de taladro rotando."),
    dict(name="carousel_hoop_done", cat="Mechanics", kind="sfx",
         main=[FS(515643)], note="Ding corto (sube de tono con cada aro)."),
    dict(name="evasive_dash", cat="Mechanics", kind="sfx",
         main=[FS(521996), FS(562191), FS(388004)], note="3 whooshes rápidos."),
    dict(name="challenge_start", cat="Mechanics", kind="sfx",
         main=[FS(538422)], note="Silbato de árbitro."),

    # --- UI ---
    dict(name="ui_tap", cat="UI", kind="ui", main=[KN(IS + "click_001.ogg")], alt=[KN(UA + "click2.ogg")],
         note="Click seco. Alternativa: click más blando."),
    dict(name="ui_tap_primary", cat="UI", kind="ui", main=[KN(IS + "confirmation_001.ogg")], note="Confirmación."),
    dict(name="ui_back", cat="UI", kind="ui", main=[KN(IS + "back_001.ogg")], note="Volver."),
    dict(name="ui_disabled", cat="UI", kind="ui", main=[KN(IS + "error_004.ogg")], note="Botón deshabilitado o tarjeta bloqueada."),
    dict(name="ui_tab", cat="UI", kind="ui", main=[KN(UA + "click1.ogg")], note="Cambio de pestaña."),
    dict(name="ui_select", cat="UI", kind="ui", main=[KN(IS + "select_002.ogg")], note="Seleccionar tarjeta."),
    dict(name="ui_equip", cat="UI", kind="ui", main=[KN(IS + "drop_002.ogg")], note="Equipar skin o fondo."),
    dict(name="ui_pause_in", cat="UI", kind="ui", main=[KN(IS + "minimize_003.ogg")], note="Abrir pausa."),
    dict(name="ui_pause_out", cat="UI", kind="ui", main=[KN(IS + "maximize_003.ogg")], note="Cerrar pausa."),
    dict(name="ui_panel_in", cat="UI", kind="ui", main=[KN(IS + "open_002.ogg")], note="Entrada de panel."),
    dict(name="ui_count_up", cat="UI", kind="ui", main=[KN(IS + "tick_004.ogg")], note="Tic del contador de puntaje."),
    dict(name="ui_progress_fill", cat="UI", kind="ui", main=[KN(DA + "phaserUp1.ogg")], note="Barra de progreso llenándose."),
    dict(name="ui_unlock", cat="UI", kind="ui", main=[FS(715067)], alt=[KN(DA + "powerUp7.ogg")],
         note="Destello de logro. Alternativa: power up digital de Kenney."),
    dict(name="ui_confetti", cat="UI", kind="ui", main=[FS(26349)], note="Lanzapapeles."),
    dict(name="ui_onboarding", cat="UI", kind="ui", main=[KN(IS + "bong_001.ogg")], note="Aviso suave del tutorial."),
]

# Los 10 más importantes (llevan alternativa en _alt/)
TOP10 = ["net_swish_chain", "ball_hit_rim", "ball_throw", "score_plus_one", "timer_tick",
         "timer_out", "ui_tap", "ui_unlock", "stg_nuevo_record", "mus_endless"]
