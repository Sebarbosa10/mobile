# Audio del juego

## Flujo

1. **Sonidos** (`Audio/Tools/picks.py` → `build_audio.py`): baja de Freesound/Kenney, procesa y deja los WAV en `Audio/Source/`.
   Créditos en `CREDITS.md`, revisión en `REVIEW.md` (`make_review.py`).
2. **Wwise** (`Audio/Tools/wwise_build.py`, con Wwise 2023.1.19 abierto y WAAPI activo):
   arma buses, states, RTPCs, contenedores, música interactiva, eventos y SoundBanks.
   Es idempotente: se puede volver a correr después de cambiar algo.
   ```bash
   python Audio/Tools/wwise_build.py --generate
   ```
   Genera los bancos para Android, iOS y Windows en `Assets/WwiseData/Bank/` y exporta
   `Audio/Tools/wwise_objects.json` (nombre → GUID).
3. **Unity**: `Tools > Audio > Vincular objetos de Wwise` completa `Assets/Audio/Resources/AudioConfig.asset`
   (corre solo al abrir el editor si el json cambió).

## Código

- `Assets/Scripts/Audio/AudioManager.cs`: única entrada al audio (`PlayThrow`, `PlayScore`, `SetTheme`, `PlayUI`...).
  Se crea solo antes de la primera escena; si la escena no tiene WwiseGlobal crea el AkInitializer.
- `SessionAudio.cs`: música por modo, estados (Playing/Paused/Results), timer, stingers y carrusel.
- `BallSurfaces.cs` / `AudioSurface.cs`: qué sonido hace cada collider que toca la pelota.
- Volúmenes: `AudioManager.MusicVolume` / `SfxVolume` (PlayerPrefs → RTPC MusicVolume / SFXVolume).

## Estructura en Wwise

- Buses: Master › Music, Stingers, Ambience, SFX › (Ball, Game, UI). Máximo 24 voces; rebotes, 4.
- Música: `Music` (Music Switch por State GameMode) con un playlist en loop por modo; `mus_endless`
  tiene 3 pistas cuyo volumen sigue al RTPC Score (percusión desde 4–10, lead desde 14–25).
- Stingers: Music Stingers al próximo beat; el evento baja el bus Music 4 dB mientras suena.
- Ambientes: `amb_switch` por State Theme, crossfade de 1 s; un banco por fondo.
- Pausa (State GameState = Paused): low-pass en Music/Stingers/Ambience y −10 dB en SFX.
- Conversión: Vorbis calidad 4 (streaming) para música y ambientes, ADPCM para SFX y UI.
