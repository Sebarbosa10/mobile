"""Arma toda la estructura de audio en el proyecto de Wwise por WAAPI (HTTP, puerto 8090).

Uso:  python Audio/Tools/wwise_build.py [--generate]

Es idempotente: se puede volver a correr después de cambiar algo (los objetos se fusionan por nombre,
los eventos y las listas se reemplazan). Con --generate también genera los SoundBanks.
Al final exporta Audio/Tools/wwise_objects.json (nombre -> GUID) para vincular los objetos en Unity.
"""
import json
import os
import sys

import requests
import soundfile as sf

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "Source")
URL = "http://127.0.0.1:8090/waapi"
PLATFORMS = ["Android", "iOS", "Windows"]

AMH = r"\Actor-Mixer Hierarchy\Default Work Unit"
IMH = r"\Interactive Music Hierarchy\Default Work Unit"
MASTER = r"\Master-Mixer Hierarchy\Default Work Unit\Master Audio Bus"
EVENTS = r"\Events\Default Work Unit"
STATES = r"\States\Default Work Unit"
GAMEPARAMS = r"\Game Parameters\Default Work Unit"
TRIGGERS = r"\Triggers\Default Work Unit"
BANKS = r"\SoundBanks\Default Work Unit"
VORBIS = r"\Conversion Settings\Factory Conversion Settings\Vorbis\Vorbis Quality High"  # calidad 4
ADPCM = r"\Conversion Settings\Factory Conversion Settings\ADPCM\ADPCM As Input"

WARNINGS = []


class WaapiError(Exception):
    pass


def call(uri, args=None, options=None):
    body = {"uri": uri, "args": args or {}, "options": options or {}}
    r = requests.post(URL, data=json.dumps(body), timeout=600)
    text = r.content.decode("utf-8", errors="replace")
    if r.status_code != 200:
        raise WaapiError(f"{uri}: {text[:600]}")
    return json.loads(text)


def get(waql_or_args, ret=("id", "name", "path", "type")):
    args = {"waql": waql_or_args} if isinstance(waql_or_args, str) else waql_or_args
    return call("ak.wwise.core.object.get", args, {"return": list(ret)})["return"]


def setobj(objects, conflict="merge", list_mode="replaceAll"):
    return call("ak.wwise.core.object.set", {"objects": objects, "onNameConflict": conflict, "listMode": list_mode})


def prop(obj, name, value):
    call("ak.wwise.core.object.setProperty", {"object": obj, "property": name, "value": value})


def ref(obj, name, value):
    call("ak.wwise.core.object.setReference", {"object": obj, "reference": name, "value": value})


def try_(label, fn, *a, **k):
    try:
        return fn(*a, **k)
    except WaapiError as e:
        WARNINGS.append(f"{label}: {e}")
        print("  ⚠", label, str(e)[:200])


def dur_ms(path):
    info = sf.info(path)
    return info.frames / info.samplerate * 1000.0


def wavs(cat, name):
    """Archivos procesados de un sonido: name.wav o name_01.wav, name_02.wav..."""
    folder = os.path.join(SRC, cat)
    single = os.path.join(folder, name + ".wav")
    if os.path.exists(single):
        return [single]
    out = sorted(os.path.join(folder, f) for f in os.listdir(folder)
                 if f.startswith(name + "_") and f[len(name) + 1:-4].isdigit() and f.endswith(".wav"))
    if not out:
        raise SystemExit(f"Falta el audio de {name} en {folder}")
    return out


def curve(points):
    return {"type": "Curve", "points": [{"x": x, "y": y, "shape": "Linear"} for x, y in points]}


def rtpc(param, prop_name, points):
    return {"type": "RTPC", "name": "", "@PropertyName": prop_name, "@ControlInput": f"{GAMEPARAMS}\\{param}",
            "@Curve": curve(points)}


# ------------------------------------------------------------------ definición

BUSES = {  # nombre: padre
    "Music": MASTER, "Stingers": MASTER, "Ambience": MASTER, "SFX": MASTER,
    "Ball": MASTER + r"\SFX", "Game": MASTER + r"\SFX", "UI": MASTER + r"\SFX",
}

GAME_PARAMETERS = {  # nombre: (min, max, inicial)
    "ThrowForce": (0, 1, 0.5), "ImpactVelocity": (0, 1, 1), "ScoreStreak": (0, 10, 0),
    "TimerRemaining": (0, 1, 1), "Score": (0, 50, 0), "CarouselHoopIndex": (1, 5, 1),
    "MusicVolume": (0, 100, 100), "SFXVolume": (0, 100, 100),
}

STATE_GROUPS = {
    "GameMode": ["Menu", "Endless", "Challenge", "TimeAttack"],
    "GameState": ["Playing", "Paused", "Results"],
    "Theme": ["Plaza", "Azotea", "Playa", "Nevado", "Desierto", "Espacio"],
}

THEMES = {"Plaza": "amb_plaza", "Azotea": "amb_azotea", "Playa": "amb_playa",
          "Nevado": "amb_nevado", "Desierto": "amb_desierto", "Espacio": "amb_espacio"}

# Actor-Mixer: (categoría de archivos, bus, conversión, [(nombre, tipo, opciones)])
#   tipo: "sound" | "random" ; opciones: loop, rand (pitch/volumen aleatorio), rtpc
RAND = dict(rand=True)
ACTOR_MIXERS = {
    "Ball": ("Ball", "Ball", ADPCM, [
        ("ball_grab", "sound", RAND),
        ("ball_throw", "random", dict(rand=True, rtpc=[("ThrowForce", "Pitch", [(0, -200), (1, 200)]),
                                                         ("ThrowForce", "Volume", [(0, -8), (1, 0)])])),
        ("ball_air", "sound", dict(loop=True)),
        ("ball_respawn", "sound", RAND),
        ("ball_shake_color", "sound", RAND),
    ]),
    "Ball\\Impacts": ("Ball", None, None, [
        ("ball_hit_rim", "random", RAND),
        ("ball_hit_backboard", "random", RAND),
        ("ball_hit_pole", "random", RAND),
    ]),
    "Ball\\Impacts\\Bounces": ("Ball", None, None, [
        ("ball_bounce_floor", "random", RAND),
        ("ball_bounce_soft", "random", RAND),
    ]),
    "Score": ("Score", "Game", ADPCM, [
        ("net_swish_chain", "random", RAND),
        ("score_clean", "sound", {}),
        ("score_rim", "sound", {}),
        ("score_backboard", "sound", {}),
        ("score_plus_one", "sound", dict(rtpc=[("ScoreStreak", "Pitch", [(0, 0), (10, 1000)])])),
        ("score_flash", "sound", {}),
        ("miss_out", "sound", {}),
    ]),
    "Mechanics": ("Mechanics", "Game", ADPCM, [
        ("timer_tick", "sound", dict(rtpc=[("TimerRemaining", "Pitch", [(0, 200), (1, 0)])])),
        ("timer_urgent", "sound", {}),
        ("timer_reset", "sound", {}),
        ("timer_out", "sound", {}),
        ("hoop_shrink", "sound", {}),
        ("hoop_move_loop", "sound", dict(loop=True, volume=-14)),
        ("carousel_loop", "sound", dict(loop=True)),
        ("carousel_hoop_done", "sound", dict(rtpc=[("CarouselHoopIndex", "Pitch", [(1, 0), (5, 800)])])),
        ("evasive_dash", "random", {}),
        ("challenge_start", "sound", {}),
    ]),
    "UI": ("UI", "UI", ADPCM, [(n, "sound", {}) for n in (
        "ui_tap", "ui_tap_primary", "ui_back", "ui_disabled", "ui_tab", "ui_select", "ui_equip", "ui_pause_in",
        "ui_pause_out", "ui_panel_in", "ui_count_up", "ui_progress_fill", "ui_unlock", "ui_confetti", "ui_onboarding")]),
}

# Música: estado de GameMode -> (playlist, segmento, tempo, [(pista, archivo, volumen dB, rtpc Score)])
MUSIC = {
    "Menu": ("mus_menu", 85, [("mus_menu", "mus_menu", 0, None)]),
    "Endless": ("mus_endless", 90, [
        ("base", "mus_endless_base", 0, None),
        ("perc", "mus_endless_perc", 4, [(0, -96), (4, -96), (10, 0), (50, 0)]),
        ("lead", "mus_endless_lead", 0, [(0, -96), (14, -96), (25, 0), (50, 0)]),
    ]),
    "Challenge": ("mus_desafio", 120, [("mus_desafio", "mus_desafio", 0, None)]),
    "TimeAttack": ("mus_contrarreloj", 88.84, [("mus_contrarreloj", "mus_contrarreloj", 0, None)]),
}

STINGERS = {"stg_nuevo_record": "Stg_NuevoRecord", "stg_victoria": "Stg_Victoria",
            "stg_derrota": "Stg_Derrota", "stg_game_over": "Stg_GameOver"}

MUSIC_DUCK_DB = -4


# ------------------------------------------------------------------ pasos

def build_buses():
    print("Buses")
    for name, parent in BUSES.items():
        setobj([{"object": parent, "children": [{"type": "Bus", "name": name}]}])
    # Límite de voces global
    setobj([{"object": MASTER, "@UseMaxSoundPerInstance": True, "@MaxSoundPerInstance": 24,
             "@OverLimitBehavior": 1}])


def build_game_parameters():
    print("Game Parameters, States y Triggers")
    setobj([{"object": GAMEPARAMS, "children": [
        {"type": "GameParameter", "name": n, "@Min": lo, "@Max": hi, "@InitialValue": init}
        for n, (lo, hi, init) in GAME_PARAMETERS.items()]}])
    setobj([{"object": STATES, "children": [
        {"type": "StateGroup", "name": g, "@DefaultTransitionTime": 0.3 if g == "GameState" else 0,
         "children": [{"type": "State", "name": s} for s in states]}
        for g, states in STATE_GROUPS.items()]}])
    setobj([{"object": TRIGGERS, "children": [{"type": "Trigger", "name": t} for t in STINGERS.values()]}])


def bus_path(name):
    parent = BUSES[name]
    return f"{parent}\\{name}"


def build_bus_controls():
    print("Volúmenes de opciones y estado de pausa en buses")
    vol_curve = [(0, -96), (10, -30), (50, -9), (100, 0)]
    for bus, param in (("Music", "MusicVolume"), ("Stingers", "MusicVolume"),
                       ("SFX", "SFXVolume"), ("Ambience", "SFXVolume")):
        setobj([{"object": bus_path(bus), "@RTPC": [rtpc(param, "BusVolume", vol_curve)]}])

    paused = {"Music": {"Lowpass": 55, "Volume": -6}, "Stingers": {"Lowpass": 55, "Volume": -6},
              "Ambience": {"Lowpass": 55, "Volume": -6}, "SFX": {"Volume": -10}}
    for bus, values in paused.items():
        path = bus_path(bus)
        call("ak.wwise.core.object.setStateGroups", {"object": path, "stateGroups": [f"{STATES}\\GameState"]})
        call("ak.wwise.core.object.setStateProperties", {"object": path, "stateProperties": sorted(values)})
        owner = get({"from": {"path": [path]}}, ("id",))[0]["id"]
        customs = get({"from": {"ofType": ["CustomState"]}}, ("id", "path", "owner"))
        target = [c for c in customs if c["owner"]["id"] == owner and c["path"] == "[Paused (Custom)]"]
        if not target:
            WARNINGS.append(f"No encontré el CustomState Paused de {bus}")
            continue
        for k, v in values.items():
            prop(target[0]["id"], k, v)


def container_obj(name, kind, opts):
    o = {"name": name}
    if kind == "random":
        o.update({"type": "RandomSequenceContainer", "@RandomOrSequence": 1, "@NormalOrShuffle": 1,
                  "@RandomAvoidRepeating": True, "@RandomAvoidRepeatingCount": 2})
    else:
        o["type"] = "Sound"
    if opts.get("loop"):
        o.update({"@IsLoopingEnabled": True, "@IsLoopingInfinite": True})
    if "volume" in opts:
        o["@Volume"] = opts["volume"]
    if opts.get("rtpc"):
        o["@RTPC"] = [rtpc(*r) for r in opts["rtpc"]]
    return o


def import_audio(imports):
    if imports:
        call("ak.wwise.core.audio.import", {"importOperation": "useExisting",
                                            "default": {"importLanguage": "SFX"}, "imports": imports},
             {"return": ["id", "name"]})


def build_actor_mixers():
    print("Actor-Mixer Hierarchy")
    for mixer, (cat, bus, conversion, items) in ACTOR_MIXERS.items():
        parts = mixer.split("\\")
        parent = AMH + ("\\" + "\\".join(parts[:-1]) if len(parts) > 1 else "")
        am = {"type": "ActorMixer", "name": parts[-1]}
        if bus:
            am.update({"@OverrideOutput": True, "@OutputBus": bus_path(bus)})
        if conversion:
            am.update({"@OverrideConversion": True, "@Conversion": conversion})
        setobj([{"object": parent, "children": [am]}])
        path = f"{parent}\\{parts[-1]}"
        if mixer == "Ball\\Impacts":
            setobj([{"object": path, "@RTPC": [rtpc("ImpactVelocity", "Volume", [(0, -24), (0.5, -6), (1, 0)])]}])
        if mixer == "Ball\\Impacts\\Bounces":
            setobj([{"object": path, "@UseMaxSoundPerInstance": True, "@MaxSoundPerInstance": 4, "@IsGlobalLimit": 1,
                     "@MaxReachedBehavior": 0}])

        setobj([{"object": path, "children": [container_obj(n, k, o) for n, k, o in items]}])
        imports = []
        for name, kind, opts in items:
            files = wavs(cat, name)
            if kind == "random":
                for f in files:
                    sname = os.path.splitext(os.path.basename(f))[0]
                    imports.append({"audioFile": f, "objectPath": f"{path}\\{name}\\<Sound SFX>{sname}",
                                    "originalsSubFolder": cat})
            else:
                imports.append({"audioFile": files[0], "objectPath": f"{path}\\<Sound SFX>{name}",
                                "originalsSubFolder": cat})
            if opts.get("rand"):
                obj = f"{path}\\{name}"
                call("ak.wwise.core.object.setRandomizer", {"object": obj, "property": "Pitch", "enabled": True, "min": -50, "max": 50})
                call("ak.wwise.core.object.setRandomizer", {"object": obj, "property": "Volume", "enabled": True, "min": -1.5, "max": 1.5})
        import_audio(imports)


def build_ambience():
    print("Ambientes")
    setobj([{"object": AMH, "children": [{"type": "ActorMixer", "name": "Ambience", "@OverrideOutput": True,
                                          "@OutputBus": bus_path("Ambience"), "@OverrideConversion": True,
                                          "@Conversion": VORBIS}]}])
    am = AMH + r"\Ambience"
    setobj([{"object": am, "children": [{
        "type": "SwitchContainer", "name": "amb_switch", "@SwitchBehavior": 1,
        "@SwitchGroupOrStateGroup": f"{STATES}\\Theme", "@DefaultSwitchOrState": f"{STATES}\\Theme\\Plaza",
        "children": [{"type": "Sound", "name": s, "@IsLoopingEnabled": True, "@IsLoopingInfinite": True,
                      "@IsStreamingEnabled": True} for s in THEMES.values()]}]}])
    sw = am + r"\amb_switch"
    import_audio([{"audioFile": wavs("Ambience", s)[0], "objectPath": f"{sw}\\<Sound SFX>{s}",
                   "originalsSubFolder": "Ambience"} for s in THEMES.values()])
    existing = call("ak.wwise.core.switchContainer.getAssignments", {"id": get({"from": {"path": [sw]}}, ("id",))[0]["id"]})["return"]
    have = {(a["child"], a["stateOrSwitch"]) for a in existing}
    for state, sound in THEMES.items():
        child = get({"from": {"path": [f"{sw}\\{sound}"]}}, ("id",))[0]["id"]
        st = get({"from": {"path": [f"{STATES}\\Theme\\{state}"]}}, ("id",))[0]["id"]
        if (child, st) not in have:
            call("ak.wwise.core.switchContainer.addAssignment", {"child": child, "stateOrSwitch": st})
    # Crossfade de 1 s entre fondos: fade in/out de cada hijo del switch
    try:
        ctx = [c for c in get("$ from project select switchContainerChildContext", ("id", "owner"))
               if c["owner"]["name"] == "amb_switch"]
        for c in ctx:
            for k, v in (("FadeInTime", 1.0), ("FadeOutTime", 1.0)):
                prop(c["id"], k, v)
        if not ctx:
            raise WaapiError("sin child context")
    except WaapiError as e:
        WARNINGS.append("Crossfade de ambientes: configurar a mano Fade-in/Fade-out de 1 s en amb_switch "
                        f"(Assigned Objects > behavior). Detalle: {str(e)[:120]}")


def build_music():
    print("Interactive Music Hierarchy")
    setobj([{"object": IMH, "children": [{
        "type": "MusicSwitchContainer", "name": "Music", "@OverrideOutput": True, "@OutputBus": bus_path("Music"),
        "@OverrideConversion": True, "@Conversion": VORBIS}]}])
    msc = IMH + r"\Music"
    imports = []
    for state, (seg, bpm, tracks) in MUSIC.items():
        seg_len = max(dur_ms(wavs("Music", f)[0]) for _, f, _, _ in tracks)
        setobj([{"object": msc, "children": [{
            "type": "MusicPlaylistContainer", "name": state,
            "children": [{"type": "MusicSegment", "name": seg, "@OverrideClockSettings": True, "@Tempo": bpm,
                          "@TimeSignatureUpper": 4, "@TimeSignatureLower": 4, "@EndPosition": seg_len,
                          "children": [{"type": "MusicTrack", "name": t, "@IsStreamingEnabled": True, "@Volume": vol,
                                        **({"@RTPC": [rtpc("Score", "Volume", curve_pts)]} if curve_pts else {})}
                                       for t, _, vol, curve_pts in tracks]}]}]}])
        seg_path = f"{msc}\\{state}\\{seg}"
        for t, f, _, _ in tracks:
            imports.append({"audioFile": wavs("Music", f)[0], "objectPath": f"{seg_path}\\{t}", "originalsSubFolder": "Music"})
        try_(f"playlist {state}", setobj, [{"object": f"{msc}\\{state}", "@PlaylistRoot": {
            "type": "MusicPlaylistItem", "name": "", "@PlaylistItemType": 0, "@LoopCount": 0, "@PlayMode": 0,
            "children": [{"type": "MusicPlaylistItem", "name": "", "@PlaylistItemType": 1, "@LoopCount": 1,
                          "@Segment": seg_path}]}}])

    # Stingers: segmentos en una carpeta aparte, salen por el bus Stingers
    setobj([{"object": IMH, "children": [{"type": "Folder", "name": "Stingers"}]}])
    folder = IMH + r"\Stingers"
    for stg in STINGERS:
        f = wavs("Stingers", stg)[0]
        setobj([{"object": folder, "children": [{
            "type": "MusicSegment", "name": stg, "@OverrideOutput": True, "@OutputBus": bus_path("Stingers"),
            "@OverrideConversion": True, "@Conversion": VORBIS, "@EndPosition": dur_ms(f),
            "children": [{"type": "MusicTrack", "name": stg}]}]}])
        imports.append({"audioFile": f, "objectPath": f"{folder}\\{stg}\\{stg}", "originalsSubFolder": "Stingers"})
    import_audio(imports)

    # Música según el State GameMode
    setobj([{"object": msc, "@Arguments": [f"{STATES}\\GameMode"],
             "@Entries": [{"type": "MultiSwitchEntry", "name": "", "@AudioNode": f"{msc}\\{state}",
                           "@EntryPath": [f"{STATES}\\GameMode\\{state}"]} for state in MUSIC]}])
    # Stingers al próximo beat
    setobj([{"object": msc, "@Stingers": [
        {"type": "MusicStinger", "name": "", "@Trigger": f"{TRIGGERS}\\{trig}", "@Segment": f"{folder}\\{stg}",
         "@PlaySegmentAt": 3, "@DontPlayAgainTime": 0.5} for stg, trig in STINGERS.items()]}])
    # Cambio de modo: salir en el próximo compás (por defecto espera al Exit Cue)
    root = get({"from": {"path": [msc]}}, ("@TransitionRoot",))[0]["@TransitionRoot"]["id"]
    for t in get({"from": {"id": [root]}, "transform": [{"select": ["descendants"]}]}, ("id", "type")):
        if t["type"] == "MusicTransition":
            prop(t["id"], "ExitSourceAt", 2)


def action(kind, target=None, **props):
    a = {"type": "Action", "name": "", "@ActionType": kind}
    if target:
        a["@Target"] = target
    a.update({f"@{k}": v for k, v in props.items()})
    return a


PLAY, STOP, TRIGGER, SET_BUS_VOL, RESET_BUS_VOL = 1, 2, 35, 11, 14


def build_events():
    print("Eventos")
    events = []
    for mixer, (cat, _, _, items) in ACTOR_MIXERS.items():
        path = AMH + "\\" + mixer
        for name, _, opts in items:
            target = f"{path}\\{name}"
            events.append(("Play_" + name, [action(PLAY, target)]))
            if opts.get("loop"):
                events.append(("Stop_" + name, [action(STOP, target, FadeTime=0.15)]))
    score = AMH + r"\Score"
    for kind, extra in (("Clean", "score_clean"), ("Rim", "score_rim"), ("Backboard", "score_backboard")):
        events.append((f"Play_Score_{kind}", [action(PLAY, f"{score}\\{s}") for s in
                                              ("net_swish_chain", extra, "score_plus_one", "score_flash")]))
    events.append(("Play_Ambience", [action(PLAY, AMH + r"\Ambience\amb_switch", FadeTime=1.0)]))
    events.append(("Stop_Ambience", [action(STOP, AMH + r"\Ambience\amb_switch", FadeTime=1.0)]))
    events.append(("Play_Music", [action(PLAY, IMH + r"\Music", FadeTime=0.5)]))
    events.append(("Stop_Music", [action(STOP, IMH + r"\Music", FadeTime=1.0)]))
    music_bus = bus_path("Music")
    for stg, trig in STINGERS.items():
        length = dur_ms(wavs("Stingers", stg)[0]) / 1000.0
        events.append(("Play_" + stg, [
            action(TRIGGER, f"{TRIGGERS}\\{trig}"),
            action(SET_BUS_VOL, music_bus, Volume=MUSIC_DUCK_DB, FadeTime=0.15),
            action(RESET_BUS_VOL, music_bus, Delay=round(length + 0.3, 2), FadeTime=0.6),
        ]))
    for _, acts in events:
        for i, a in enumerate(acts):
            a["name"] = f"Action{i + 1}"
    # merge (no replace): así los GUID de los eventos no cambian entre corridas y Unity no pierde las referencias
    setobj([{"object": EVENTS, "children": [{"type": "Event", "name": n, "children": acts} for n, acts in events]}])
    return [n for n, _ in events]


def build_banks(event_names):
    print("SoundBanks")
    banks = ["Main", "Music"] + [f"Amb_{t}" for t in THEMES]
    setobj([{"object": BANKS, "children": [{"type": "SoundBank", "name": b} for b in banks]}])
    # Limpia inclusiones que apunten a objetos borrados (quedan colgadas y rompen la generación)
    for b in banks:
        current = call("ak.wwise.core.soundbank.getInclusions", {"soundbank": f"{BANKS}\\{b}"})["inclusions"]
        dangling = 0
        for inc in current:
            try:
                get({"from": {"id": [inc["object"]]}}, ("id",))
            except WaapiError:
                dangling += 1
        if dangling:
            call("ak.wwise.core.object.delete", {"object": f"{BANKS}\\{b}"})
            setobj([{"object": BANKS, "children": [{"type": "SoundBank", "name": b}]}])
    ev = lambda n: f"{EVENTS}\\{n}"
    music_events = {"Play_Music", "Stop_Music"} | {"Play_" + s for s in STINGERS}
    main = [{"object": ev(n), "filter": ["events", "structures", "media"]} for n in event_names
            if n not in music_events and n not in ("Play_Ambience", "Stop_Ambience")]
    # Los eventos de ambiente y la estructura del switch van en Main; el audio de cada fondo, en su banco
    main += [{"object": ev("Play_Ambience"), "filter": ["events", "structures"]},
             {"object": ev("Stop_Ambience"), "filter": ["events", "structures"]}]
    music = [{"object": ev(n), "filter": ["events", "structures", "media"]} for n in sorted(music_events)]
    music.append({"object": IMH + r"\Stingers", "filter": ["structures", "media"]})
    inclusions = {"Main": main, "Music": music}
    for theme, sound in THEMES.items():
        inclusions[f"Amb_{theme}"] = [{"object": f"{AMH}\\Ambience\\amb_switch\\{sound}", "filter": ["media"]}]
    for bank, inc in inclusions.items():
        call("ak.wwise.core.soundbank.setInclusions", {"soundbank": f"{BANKS}\\{bank}", "operation": "replace",
                                                       "inclusions": inc})
    return banks


def generate(banks):
    print("Generando SoundBanks para", ", ".join(PLATFORMS))
    r = call("ak.wwise.core.soundbank.generate", {"soundbanks": [{"name": b} for b in banks], "platforms": PLATFORMS,
                                                  "writeToDisk": True, "rebuildInitBank": True,
                                                  "clearAudioFileCache": False})
    errors = [m for m in r.get("logs", []) if m.get("severity") in ("Error", "Fatal Error")]
    for m in r.get("logs", []):
        if m.get("severity") in ("Error", "Fatal Error", "Warning"):
            print(f"  [{m.get('severity')}] {m.get('message', '')[:200]}")
    if errors:
        raise SystemExit(f"{len(errors)} errores al generar los bancos")


def export_objects():
    """Nombre -> GUID de los objetos que usa Unity (eventos, bancos, states, game parameters)."""
    out = {"Event": {}, "Soundbank": {}, "State": {}, "StateGroup": {}, "GameParameter": {}}
    for e in get({"from": {"path": [EVENTS]}, "transform": [{"select": ["children"]}]}, ("name", "id")):
        out["Event"][e["name"]] = e["id"]
    for b in get({"from": {"path": [BANKS]}, "transform": [{"select": ["children"]}]}, ("name", "id")):
        out["Soundbank"][b["name"]] = b["id"]
    for g in STATE_GROUPS:
        gid = get({"from": {"path": [f"{STATES}\\{g}"]}}, ("id",))[0]["id"]
        out["StateGroup"][g] = gid
        for s in get({"from": {"path": [f"{STATES}\\{g}"]}, "transform": [{"select": ["children"]}]}, ("name", "id")):
            out["State"][f"{g}/{s['name']}"] = s["id"]
    for p in get({"from": {"path": [GAMEPARAMS]}, "transform": [{"select": ["children"]}]}, ("name", "id")):
        out["GameParameter"][p["name"]] = p["id"]
    # Formato de listas ({"Event": [{"name", "guid"}]}) para que Unity lo lea con JsonUtility
    out = {kind: [{"name": n, "guid": g} for n, g in items.items()] for kind, items in out.items()}
    path = os.path.join(ROOT, "Tools", "wwise_objects.json")
    json.dump(out, open(path, "w", encoding="utf-8"), indent=1)
    print("Exportado", os.path.relpath(path, os.path.dirname(ROOT)))


def main():
    info = call("ak.wwise.core.getInfo")
    print("Conectado a", info["version"]["displayName"])
    call("ak.wwise.core.undo.beginGroup")
    try:
        build_buses()
        build_game_parameters()
        build_bus_controls()
        build_actor_mixers()
        build_ambience()
        build_music()
        names = build_events()
        banks = build_banks(names)
    finally:
        call("ak.wwise.core.undo.endGroup", {"displayName": "Audio: estructura del juego"})
    call("ak.wwise.core.project.save")
    export_objects()
    if "--generate" in sys.argv:
        generate(banks)
    if WARNINGS:
        print("\nAvisos:")
        for w in WARNINGS:
            print(" -", w)


if __name__ == "__main__":
    main()
