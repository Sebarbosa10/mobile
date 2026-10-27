# Revisión de audio

Elegí cada sonido por descripción, tags, duración y calificación en Freesound: **no los escuché**.
Marcá cada fila con ✅ (aprobado), 🔁 (usar la alternativa) o ❌ (buscar otro) y anotá lo que quieras en *Comentario*.

- Archivos procesados: `Audio/Source/<Categoría>/`. Alternativas: `Audio/Source/_alt/<sonido>/`.
- Formato: WAV 48 kHz 16 bit; mono para SFX y UI, estéreo para música y ambientes.
- Niveles: SFX y UI a −1 dBTP de pico; música, stingers y ambientes a −16 LUFS (las capas de `mus_endless` a −20 LUFS, el shaker por pico). Como los SFX están normalizados por pico, el volumen percibido varía bastante entre ellos: el balance final se hace en Wwise.
- Todo es CC0 salvo que se indique lo contrario (ver [CREDITS.md](CREDITS.md)).
- Para regenerar después de cambiar una elección: editar `Audio/Tools/picks.py` y correr `python Audio/Tools/build_audio.py <sonido>`.

## Los 10 más importantes (con alternativa)

| Estado | Sonido | Principal | Alternativa | Comentario |
|---|---|---|---|---|
| ⬜ | **net_swish_chain** | [`net_swish_chain_01.wav`](Source/Score/net_swish_chain_01.wav) · 1.20 s<br>[`net_swish_chain_02.wav`](Source/Score/net_swish_chain_02.wav) · 0.95 s<br>[`net_swish_chain_03.wav`](Source/Score/net_swish_chain_03.wav) · 0.14 s<br>[Sports Basketball Hit Basket](https://freesound.org/people/amsaenz03/sounds/788259/) de amsaenz03<br>[Chain.ogg](https://freesound.org/people/egomassive/sounds/536737/) de egomassive<br>[Chains Rattling.wav](https://freesound.org/people/mffm/sounds/235960/) de mffm | [`net_swish_chain_01.wav`](Source/_alt/net_swish_chain/net_swish_chain_01.wav) · 0.17 s<br>[`net_swish_chain_02.wav`](Source/_alt/net_swish_chain/net_swish_chain_02.wav) · 0.14 s<br>[`net_swish_chain_03.wav`](Source/_alt/net_swish_chain/net_swish_chain_03.wav) · 0.28 s<br>[chain](https://freesound.org/people/nettimato/sounds/387240/) de nettimato | |
| ⬜ | **ball_hit_rim** | [`ball_hit_rim_01.wav`](Source/Ball/ball_hit_rim_01.wav) · 1.20 s<br>[`ball_hit_rim_02.wav`](Source/Ball/ball_hit_rim_02.wav) · 1.20 s<br>[`ball_hit_rim_03.wav`](Source/Ball/ball_hit_rim_03.wav) · 0.17 s<br>[`ball_hit_rim_04.wav`](Source/Ball/ball_hit_rim_04.wav) · 0.20 s<br>[hit - metallic - basketball hoop pipe 02.wav](https://freesound.org/people/Anthousai/sounds/406489/) de Anthousai<br>[hit - metallic - basketball hoop pipe 01.wav](https://freesound.org/people/Anthousai/sounds/406490/) de Anthousai<br>[basketball.wav](https://freesound.org/people/reedhos/sounds/244348/) de reedhos | [`ball_hit_rim_01.wav`](Source/_alt/ball_hit_rim/ball_hit_rim_01.wav) · 1.11 s<br>[`ball_hit_rim_02.wav`](Source/_alt/ball_hit_rim/ball_hit_rim_02.wav) · 0.95 s<br>[`ball_hit_rim_03.wav`](Source/_alt/ball_hit_rim/ball_hit_rim_03.wav) · 0.55 s<br>[`ball_hit_rim_04.wav`](Source/_alt/ball_hit_rim/ball_hit_rim_04.wav) · 0.17 s<br>[metal_rings_04.wav](https://freesound.org/people/Department64/sounds/95275/) de Department64<br>[metal bowl - hit - with wooden spoon 01.wav](https://freesound.org/people/Anthousai/sounds/405665/) de Anthousai<br>[Metal hits (high)](https://freesound.org/people/xkeril/sounds/815762/) de xkeril | |
| ⬜ | **ball_throw** | [`ball_throw_01.wav`](Source/Ball/ball_throw_01.wav) · 0.51 s<br>[`ball_throw_02.wav`](Source/Ball/ball_throw_02.wav) · 0.18 s<br>[`ball_throw_03.wav`](Source/Ball/ball_throw_03.wav) · 0.41 s<br>[Whoosh Short](https://freesound.org/people/FartCTO/sounds/789840/) de FartCTO<br>[Throw 1.mp3](https://freesound.org/people/deleted_user_6479820/sounds/394873/) de deleted_user_6479820<br>[Whoosh #1](https://freesound.org/people/Kinoton/sounds/427823/) de Kinoton | [`ball_throw_01.wav`](Source/_alt/ball_throw/ball_throw_01.wav) · 0.22 s<br>[`ball_throw_02.wav`](Source/_alt/ball_throw/ball_throw_02.wav) · 0.24 s<br>[`ball_throw_03.wav`](Source/_alt/ball_throw/ball_throw_03.wav) · 0.44 s<br>[Swoosh.ogg](https://freesound.org/people/WizardOZ/sounds/419341/) de WizardOZ<br>[Fast whoosh, bamboo swoosh through air, version 2](https://freesound.org/people/zapsplat.com/sounds/719637/) de zapsplat.com<br>[Swosh / Whoosh / Air Cut](https://freesound.org/people/qubodup/sounds/60030/) de qubodup | |
| ⬜ | **score_plus_one** | [`score_plus_one.wav`](Source/Score/score_plus_one.wav) · 0.24 s<br>[UI Cute Select Major 6th](https://freesound.org/people/plasterbrain/sounds/396193/) de plasterbrain | [`score_plus_one.wav`](Source/_alt/score_plus_one/score_plus_one.wav) · 0.20 s<br>[Coin_C_09](https://freesound.org/people/cabled_mess/sounds/350876/) de cabled_mess | |
| ⬜ | **timer_tick** | [`timer_tick.wav`](Source/Mechanics/timer_tick.wav) · 0.22 s<br>[clock_tickC2](https://freesound.org/people/Mohagged/sounds/810938/) de Mohagged | [`timer_tick.wav`](Source/_alt/timer_tick/timer_tick.wav) · 0.17 s<br>[Clock Bonmark Tick.wav](https://freesound.org/people/zmastafa102/sounds/509714/) de zmastafa102 | |
| ⬜ | **timer_out** | [`timer_out.wav`](Source/Mechanics/timer_out.wav) · 2.00 s<br>[buzzer sportif.wav](https://freesound.org/people/Na%C3%AFma/sounds/510026/) de Naïma | [`timer_out.wav`](Source/_alt/timer_out/timer_out.wav) · 1.19 s<br>[buzzer.wav](https://freesound.org/people/Philip_Daniels/sounds/259290/) de Philip_Daniels | |
| ⬜ | **ui_tap** | [`ui_tap.wav`](Source/UI/ui_tap.wav) · 0.06 s<br>Kenney `click_001.ogg` | [`ui_tap.wav`](Source/_alt/ui_tap/ui_tap.wav) · 0.05 s<br>Kenney `click2.ogg` | |
| ⬜ | **ui_unlock** | [`ui_unlock.wav`](Source/UI/ui_unlock.wav) · 1.34 s<br>[achievement-sparkle](https://freesound.org/people/SkySpeira/sounds/715067/) de SkySpeira | [`ui_unlock.wav`](Source/_alt/ui_unlock/ui_unlock.wav) · 0.32 s<br>Kenney `powerUp7.ogg` | |
| ⬜ | **stg_nuevo_record** | [`stg_nuevo_record.wav`](Source/Stingers/stg_nuevo_record.wav) · 1.44 s<br>[Congrats](https://freesound.org/people/Fupicat/sounds/607207/) de Fupicat | [`stg_nuevo_record.wav`](Source/_alt/stg_nuevo_record/stg_nuevo_record.wav) · 2.07 s<br>[WinMutedGuitar.wav](https://freesound.org/people/Fupicat/sounds/521646/) de Fupicat | |
| ⬜ | **mus_endless_lead** | [`mus_endless_lead.wav`](Source/Music/mus_endless_lead.wav) · 21.33 s<br>[synth_loop_054_90bpm.wav](https://freesound.org/people/stixthule/sounds/412901/) de stixthule | [`mus_endless_lead.wav`](Source/_alt/mus_endless_lead/mus_endless_lead.wav) · 21.33 s<br>[REESEY BASS 2, 90BPM, D](https://freesound.org/people/dubbwebb/sounds/265152/) de dubbwebb | |

Notas sobre las alternativas:

- **net_swish_chain**: Red de cadena: tiro que entra y sacude el aro + cadenas cortas. Alternativa: 3 sacudidas de una misma cadena.
- **ball_hit_rim**: 2 golpes en el caño de un aro real + 2 golpes de un tiro errado al aro. Alternativa: anillos y piezas de metal sueltas, más agudos.
- **ball_throw**: Whooshes cortos. Alternativa: whooshes de bambú más agudos.
- **score_plus_one**: Blip con sexta mayor, sube de tono con la racha. Alternativa: moneda 8-bit.
- **timer_tick**: Tic de reloj real. Alternativa: tic más seco.
- **timer_out**: Buzzer deportivo real. Alternativa: buzzer más estridente.
- **ui_tap**: Click seco. Alternativa: click más blando.
- **ui_unlock**: Destello de logro. Alternativa: power up digital de Kenney.
- **stg_nuevo_record**: Jingle arcade corto de felicitación. Alternativa: fanfarria con guitarra muteada.
- **mus_endless_lead**: Capa 3 (lead): loop de synth 90 BPM (Mi mayor). Alternativa: bajo reese en Re menor, más oscuro.

## Lista completa

### Música

| Estado | Sonido | Archivos | Origen | Por qué / notas | Comentario |
|---|---|---|---|---|---|
| ⬜ | **mus_menu** | [`mus_menu.wav`](Source/Music/mus_menu.wav) · 22.59 s | [Drum and Piano loop 2](https://freesound.org/people/EEE3333E/sounds/842606/) de EEE3333E | Batería + piano, 85 BPM, tranquilo para el menú. | |
| ⬜ | **mus_endless_base** | [`mus_endless_base.wav`](Source/Music/mus_endless_base.wav) · 21.33 s | [BoomBap Drums 90 BPM.wav](https://freesound.org/people/holizna/sounds/629139/) de holizna | Capa 1 (base): batería boom bap 90 BPM, 8 compases. | |
| ⬜ | **mus_endless_perc** | [`mus_endless_perc.wav`](Source/Music/mus_endless_perc.wav) · 21.33 s | [Shaker - 90 BPM](https://freesound.org/people/jgreer/sounds/337239/) de jgreer | Capa 2 (percusión): shaker 90 BPM, 8 compases. | |
| ⬜ | **mus_endless_lead** | [`mus_endless_lead.wav`](Source/Music/mus_endless_lead.wav) · 21.33 s | [synth_loop_054_90bpm.wav](https://freesound.org/people/stixthule/sounds/412901/) de stixthule | Capa 3 (lead): loop de synth 90 BPM (Mi mayor). Alternativa: bajo reese en Re menor, más oscuro. | |
| ⬜ | **mus_desafio** | [`mus_desafio.wav`](Source/Music/mus_desafio.wav) · 96.00 s | [Electro Beat Bgm](https://freesound.org/people/Ncone/sounds/722311/) de Ncone | Electro beat de acción para los desafíos (120 BPM, loop de 48 compases). | |
| ⬜ | **mus_contrarreloj** | [`mus_contrarreloj.wav`](Source/Music/mus_contrarreloj.wav) · 97.25 s | [Fight Music Synth Tense Loop](https://freesound.org/people/SnowFightStudios/sounds/676998/) de SnowFightStudios | Synth tenso para la contrarreloj (~88,8 BPM estimado, loop de 36 compases). | |

### Stingers

| Estado | Sonido | Archivos | Origen | Por qué / notas | Comentario |
|---|---|---|---|---|---|
| ⬜ | **stg_nuevo_record** | [`stg_nuevo_record.wav`](Source/Stingers/stg_nuevo_record.wav) · 1.44 s | [Congrats](https://freesound.org/people/Fupicat/sounds/607207/) de Fupicat | Jingle arcade corto de felicitación. Alternativa: fanfarria con guitarra muteada.<br>⚠ stg_nuevo_record.wav: dura 1.44 s (pedido: 1,5–3 s) | |
| ⬜ | **stg_victoria** | [`stg_victoria.wav`](Source/Stingers/stg_victoria.wav) · 1.74 s | [magic_game_win_success.wav](https://freesound.org/people/MLaudio/sounds/615099/) de MLaudio | Jingle de victoria. | |
| ⬜ | **stg_derrota** | [`stg_derrota.wav`](Source/Stingers/stg_derrota.wav) · 2.50 s | [End Game - Fail](https://freesound.org/people/davidbain/sounds/135831/) de davidbain | Jingle de derrota. | |
| ⬜ | **stg_game_over** | [`stg_game_over.wav`](Source/Stingers/stg_game_over.wav) · 2.13 s | [game over](https://freesound.org/people/Leszek_Szary/sounds/133283/) de Leszek_Szary | Jingle de game over. | |

### Ambientes

| Estado | Sonido | Archivos | Origen | Por qué / notas | Comentario |
|---|---|---|---|---|---|
| ⬜ | **amb_plaza** | [`amb_plaza.wav`](Source/Ambience/amb_plaza.wav) · 48.00 s | [city park Tel Aviv Israel.wav](https://freesound.org/people/yonts/sounds/268903/) de yonts | Parque urbano + chicos jugando a lo lejos (grabado en Argentina). Sin tráfico marcado. | |
| ⬜ | **amb_azotea** | [`amb_azotea.wav`](Source/Ambience/amb_azotea.wav) · 60.00 s | [Ambience City Quiet Night Air Tone](https://freesound.org/people/leonelmail/sounds/427841/) de leonelmail | Ciudad de noche + zumbido de neón bajo. | |
| ⬜ | **amb_playa** | [`amb_playa.wav`](Source/Ambience/amb_playa.wav) · 60.00 s | [Gentle ocean waves birdsong and gull.WAV](https://freesound.org/people/jackmichaelking/sounds/518637/) de jackmichaelking | Olas suaves, pájaros y gaviota. | |
| ⬜ | **amb_nevado** | [`amb_nevado.wav`](Source/Ambience/amb_nevado.wav) · 50.00 s | [Wind__Artic__Cold.wav](https://freesound.org/people/cobratronik/sounds/117136/) de cobratronik | Viento ártico frío. | |
| ⬜ | **amb_desierto** | [`amb_desierto.wav`](Source/Ambience/amb_desierto.wav) · 60.00 s | [Dry grass rustling in the wind, in the desert of Chile.](https://freesound.org/people/felix.blume/sounds/146436/) de felix.blume | Pasto seco con viento (Atacama). No tiene insectos. | |
| ⬜ | **amb_espacio** | [`amb_espacio.wav`](Source/Ambience/amb_espacio.wav) · 60.00 s | [Space Station Drone](https://freesound.org/people/db3005/sounds/686237/) de db3005 | Drone grave de estación espacial. | |

### Pelota

| Estado | Sonido | Archivos | Origen | Por qué / notas | Comentario |
|---|---|---|---|---|---|
| ⬜ | **ball_grab** | [`ball_grab.wav`](Source/Ball/ball_grab.wav) · 0.18 s | [BallCatch.wav](https://freesound.org/people/ToastHatter/sounds/514989/) de ToastHatter | Atrapada de pelota de goma con la mano. | |
| ⬜ | **ball_throw** | [`ball_throw_01.wav`](Source/Ball/ball_throw_01.wav) · 0.51 s<br>[`ball_throw_02.wav`](Source/Ball/ball_throw_02.wav) · 0.18 s<br>[`ball_throw_03.wav`](Source/Ball/ball_throw_03.wav) · 0.41 s | [Whoosh Short](https://freesound.org/people/FartCTO/sounds/789840/) de FartCTO<br>[Throw 1.mp3](https://freesound.org/people/deleted_user_6479820/sounds/394873/) de deleted_user_6479820<br>[Whoosh #1](https://freesound.org/people/Kinoton/sounds/427823/) de Kinoton | Whooshes cortos. Alternativa: whooshes de bambú más agudos. | |
| ⬜ | **ball_air** | [`ball_air.wav`](Source/Ball/ball_air.wav) · 0.80 s | [whoosh_long_low.wav](https://freesound.org/people/DJT4NN3R/sounds/449990/) de DJT4NN3R | Loop corto sacado del medio de un whoosh largo y grave. | |
| ⬜ | **ball_bounce_floor** | [`ball_bounce_floor_01.wav`](Source/Ball/ball_bounce_floor_01.wav) · 0.36 s<br>[`ball_bounce_floor_02.wav`](Source/Ball/ball_bounce_floor_02.wav) · 0.34 s<br>[`ball_bounce_floor_03.wav`](Source/Ball/ball_bounce_floor_03.wav) · 0.28 s<br>[`ball_bounce_floor_04.wav`](Source/Ball/ball_bounce_floor_04.wav) · 0.16 s | [Basketball 3](https://freesound.org/people/zmobie/sounds/319785/) de zmobie<br>[Basketball 2](https://freesound.org/people/zmobie/sounds/319762/) de zmobie<br>[Basketball 9](https://freesound.org/people/zmobie/sounds/319764/) de zmobie<br>[Basketball 8](https://freesound.org/people/zmobie/sounds/319783/) de zmobie | 4 piques de pelota de básquet reales, de la misma serie. | |
| ⬜ | **ball_bounce_soft** | [`ball_bounce_soft_01.wav`](Source/Ball/ball_bounce_soft_01.wav) · 0.32 s<br>[`ball_bounce_soft_02.wav`](Source/Ball/ball_bounce_soft_02.wav) · 0.32 s<br>[`ball_bounce_soft_03.wav`](Source/Ball/ball_bounce_soft_03.wav) · 0.30 s | [basketball bounce.wav](https://freesound.org/people/toddcircle/sounds/451642/) de toddcircle | 3 piques sobre alfombra, más apagados. | |
| ⬜ | **ball_hit_rim** | [`ball_hit_rim_01.wav`](Source/Ball/ball_hit_rim_01.wav) · 1.20 s<br>[`ball_hit_rim_02.wav`](Source/Ball/ball_hit_rim_02.wav) · 1.20 s<br>[`ball_hit_rim_03.wav`](Source/Ball/ball_hit_rim_03.wav) · 0.17 s<br>[`ball_hit_rim_04.wav`](Source/Ball/ball_hit_rim_04.wav) · 0.20 s | [hit - metallic - basketball hoop pipe 02.wav](https://freesound.org/people/Anthousai/sounds/406489/) de Anthousai<br>[hit - metallic - basketball hoop pipe 01.wav](https://freesound.org/people/Anthousai/sounds/406490/) de Anthousai<br>[basketball.wav](https://freesound.org/people/reedhos/sounds/244348/) de reedhos | 2 golpes en el caño de un aro real + 2 golpes de un tiro errado al aro. Alternativa: anillos y piezas de metal sueltas, más agudos. | |
| ⬜ | **ball_hit_backboard** | [`ball_hit_backboard_01.wav`](Source/Ball/ball_hit_backboard_01.wav) · 0.52 s<br>[`ball_hit_backboard_02.wav`](Source/Ball/ball_hit_backboard_02.wav) · 0.44 s<br>[`ball_hit_backboard_03.wav`](Source/Ball/ball_hit_backboard_03.wav) · 0.31 s | [Basketball backboard.wav](https://freesound.org/people/PerMagnusLindborg/sounds/324247/) de PerMagnusLindborg<br>Kenney `impactPlank_medium_000.ogg`<br>Kenney `impactWood_heavy_001.ogg` | Tablero real + 2 golpes de madera de Kenney para variar. | |
| ⬜ | **ball_hit_pole** | [`ball_hit_pole_01.wav`](Source/Ball/ball_hit_pole_01.wav) · 0.43 s<br>[`ball_hit_pole_02.wav`](Source/Ball/ball_hit_pole_02.wav) · 0.16 s | [Metal Pole Hit](https://freesound.org/people/Ev-Dawg/sounds/336000/) de Ev-Dawg<br>Kenney `impactMetal_heavy_000.ogg` | Golpe en poste metálico + metal pesado de Kenney. | |
| ⬜ | **ball_respawn** | [`ball_respawn.wav`](Source/Ball/ball_respawn.wav) · 0.25 s | [Pop in sfx](https://freesound.org/people/Sadiquecat/sounds/824189/) de Sadiquecat | Pop de aparición. | |
| ⬜ | **ball_shake_color** | [`ball_shake_color.wav`](Source/Ball/ball_shake_color.wav) · 0.89 s | [cartoon_wink_magic_sparkle.wav](https://freesound.org/people/MLaudio/sounds/511485/) de MLaudio | Destello mágico corto. | |

### Encestada

| Estado | Sonido | Archivos | Origen | Por qué / notas | Comentario |
|---|---|---|---|---|---|
| ⬜ | **net_swish_chain** | [`net_swish_chain_01.wav`](Source/Score/net_swish_chain_01.wav) · 1.20 s<br>[`net_swish_chain_02.wav`](Source/Score/net_swish_chain_02.wav) · 0.95 s<br>[`net_swish_chain_03.wav`](Source/Score/net_swish_chain_03.wav) · 0.14 s | [Sports Basketball Hit Basket](https://freesound.org/people/amsaenz03/sounds/788259/) de amsaenz03<br>[Chain.ogg](https://freesound.org/people/egomassive/sounds/536737/) de egomassive<br>[Chains Rattling.wav](https://freesound.org/people/mffm/sounds/235960/) de mffm | Red de cadena: tiro que entra y sacude el aro + cadenas cortas. Alternativa: 3 sacudidas de una misma cadena. | |
| ⬜ | **score_clean** | [`score_clean.wav`](Source/Score/score_clean.wav) · 0.29 s | [PowerUp.wav](https://freesound.org/people/kianda/sounds/328120/) de kianda | Ding brillante (power up). | |
| ⬜ | **score_rim** | [`score_rim.wav`](Source/Score/score_rim.wav) · 0.35 s | [sqeeeek_bell_ting2.wav](https://freesound.org/people/sqeeeek/sounds/237105/) de sqeeeek | Campanita metálica corta. | |
| ⬜ | **score_backboard** | [`score_backboard.wav`](Source/Score/score_backboard.wav) · 0.10 s | Kenney `pluck_001.ogg` | Pluck suave de Kenney. | |
| ⬜ | **score_plus_one** | [`score_plus_one.wav`](Source/Score/score_plus_one.wav) · 0.24 s | [UI Cute Select Major 6th](https://freesound.org/people/plasterbrain/sounds/396193/) de plasterbrain | Blip con sexta mayor, sube de tono con la racha. Alternativa: moneda 8-bit. | |
| ⬜ | **score_flash** | [`score_flash.wav`](Source/Score/score_flash.wav) · 0.45 s | Kenney `phaserUp3.ogg` | Barrido ascendente de Kenney. | |
| ⬜ | **miss_out** | [`miss_out.wav`](Source/Score/miss_out.wav) · 0.17 s | [Cancel/miss chime](https://freesound.org/people/Raclure/sounds/405548/) de Raclure | Campana suave de error, sin castigar. | |

### Mecánicas

| Estado | Sonido | Archivos | Origen | Por qué / notas | Comentario |
|---|---|---|---|---|---|
| ⬜ | **timer_tick** | [`timer_tick.wav`](Source/Mechanics/timer_tick.wav) · 0.22 s | [clock_tickC2](https://freesound.org/people/Mohagged/sounds/810938/) de Mohagged | Tic de reloj real. Alternativa: tic más seco. | |
| ⬜ | **timer_urgent** | [`timer_urgent.wav`](Source/Mechanics/timer_urgent.wav) · 0.21 s | [beep.wav](https://freesound.org/people/jeckkech/sounds/391650/) de jeckkech | Beep 8-bit corto para los últimos segundos. | |
| ⬜ | **timer_reset** | [`timer_reset.wav`](Source/Mechanics/timer_reset.wav) · 0.40 s | Kenney `highUp.ogg` | Subida de tono de Kenney. | |
| ⬜ | **timer_out** | [`timer_out.wav`](Source/Mechanics/timer_out.wav) · 2.00 s | [buzzer sportif.wav](https://freesound.org/people/Na%C3%AFma/sounds/510026/) de Naïma | Buzzer deportivo real. Alternativa: buzzer más estridente. | |
| ⬜ | **hoop_shrink** | [`hoop_shrink.wav`](Source/Mechanics/hoop_shrink.wav) · 1.86 s | [Power Down](https://freesound.org/people/noirenex/sounds/159399/) de noirenex | Power down. | |
| ⬜ | **hoop_move_loop** | [`hoop_move_loop.wav`](Source/Mechanics/hoop_move_loop.wav) · 4.00 s | [low_machine_hum.wav](https://freesound.org/people/Foxicity/sounds/587617/) de Foxicity | Zumbido de máquina grave y parejo (reemplaza al torno Dremel, que chillaba). Alternativa: drone eléctrico más grave. | |
| ⬜ | **carousel_loop** | [`carousel_loop.wav`](Source/Mechanics/carousel_loop.wav) · 1.00 s | [Pillar_Drill_Rotating_Loop.wav](https://freesound.org/people/mickyman5000/sounds/340656/) de mickyman5000 | Loop de taladro rotando. | |
| ⬜ | **carousel_hoop_done** | [`carousel_hoop_done.wav`](Source/Mechanics/carousel_hoop_done.wav) · 0.33 s | [ding2.wav](https://freesound.org/people/MashedTatoes2/sounds/515643/) de MashedTatoes2 | Ding corto (sube de tono con cada aro). | |
| ⬜ | **evasive_dash** | [`evasive_dash_01.wav`](Source/Mechanics/evasive_dash_01.wav) · 0.37 s<br>[`evasive_dash_02.wav`](Source/Mechanics/evasive_dash_02.wav) · 0.26 s<br>[`evasive_dash_03.wav`](Source/Mechanics/evasive_dash_03.wav) · 0.19 s | [Whoosh / Dash (2)](https://freesound.org/people/Kastenfrosch/sounds/521996/) de Kastenfrosch<br>[snd_swing_whoosh.wav](https://freesound.org/people/gristi/sounds/562191/) de gristi<br>[Quick Swish](https://freesound.org/people/deleted_user_6479820/sounds/388004/) de deleted_user_6479820 | 3 whooshes rápidos. | |
| ⬜ | **challenge_start** | [`challenge_start.wav`](Source/Mechanics/challenge_start.wav) · 0.52 s | [Referee whistle sound.wav](https://freesound.org/people/Rosa-Orenes256/sounds/538422/) de Rosa-Orenes256 | Silbato de árbitro. | |

### UI

| Estado | Sonido | Archivos | Origen | Por qué / notas | Comentario |
|---|---|---|---|---|---|
| ⬜ | **ui_tap** | [`ui_tap.wav`](Source/UI/ui_tap.wav) · 0.06 s | Kenney `click_001.ogg` | Click seco. Alternativa: click más blando. | |
| ⬜ | **ui_tap_primary** | [`ui_tap_primary.wav`](Source/UI/ui_tap_primary.wav) · 0.29 s | Kenney `confirmation_001.ogg` | Confirmación. | |
| ⬜ | **ui_back** | [`ui_back.wav`](Source/UI/ui_back.wav) · 0.06 s | Kenney `back_001.ogg` | Volver. | |
| ⬜ | **ui_disabled** | [`ui_disabled.wav`](Source/UI/ui_disabled.wav) · 0.10 s | Kenney `error_004.ogg` | Botón deshabilitado o tarjeta bloqueada. | |
| ⬜ | **ui_tab** | [`ui_tab.wav`](Source/UI/ui_tab.wav) · 0.09 s | Kenney `click1.ogg` | Cambio de pestaña. | |
| ⬜ | **ui_select** | [`ui_select.wav`](Source/UI/ui_select.wav) · 0.04 s | Kenney `select_002.ogg` | Seleccionar tarjeta. | |
| ⬜ | **ui_equip** | [`ui_equip.wav`](Source/UI/ui_equip.wav) · 0.19 s | Kenney `drop_002.ogg` | Equipar skin o fondo. | |
| ⬜ | **ui_pause_in** | [`ui_pause_in.wav`](Source/UI/ui_pause_in.wav) · 0.21 s | Kenney `minimize_003.ogg` | Abrir pausa. | |
| ⬜ | **ui_pause_out** | [`ui_pause_out.wav`](Source/UI/ui_pause_out.wav) · 0.21 s | Kenney `maximize_003.ogg` | Cerrar pausa. | |
| ⬜ | **ui_panel_in** | [`ui_panel_in.wav`](Source/UI/ui_panel_in.wav) · 0.31 s | Kenney `open_002.ogg` | Entrada de panel. | |
| ⬜ | **ui_count_up** | [`ui_count_up.wav`](Source/UI/ui_count_up.wav) · 0.06 s | Kenney `tick_004.ogg` | Tic del contador de puntaje. | |
| ⬜ | **ui_progress_fill** | [`ui_progress_fill.wav`](Source/UI/ui_progress_fill.wav) · 0.46 s | Kenney `phaserUp1.ogg` | Barra de progreso llenándose. | |
| ⬜ | **ui_unlock** | [`ui_unlock.wav`](Source/UI/ui_unlock.wav) · 1.34 s | [achievement-sparkle](https://freesound.org/people/SkySpeira/sounds/715067/) de SkySpeira | Destello de logro. Alternativa: power up digital de Kenney. | |
| ⬜ | **ui_confetti** | [`ui_confetti.wav`](Source/UI/ui_confetti.wav) · 0.61 s | [partypopper.flac](https://freesound.org/people/Streety/sounds/26349/) de Streety | Lanzapapeles. | |
| ⬜ | **ui_onboarding** | [`ui_onboarding.wav`](Source/UI/ui_onboarding.wav) · 0.12 s | Kenney `bong_001.ogg` | Aviso suave del tutorial. | |

## Pendientes y decisiones

- **mus_endless**: las 3 capas son loops de autores distintos, todos de 8 compases exactos a 90 BPM, así que van a estar en tempo. La batería y el shaker no tienen tonalidad, pero el lead está en Mi mayor (la alternativa en Re menor). Escuchalas juntas en [`_preview/mus_endless_mix.wav`](Source/_preview/mus_endless_mix.wav) (y con el lead alternativo en [`mus_endless_mix_alt.wav`](Source/_preview/mus_endless_mix_alt.wav)); son 2 vueltas para oír el empalme. Si no pegan, dejo el lead como placeholder hasta encontrar otro.
- **mus_endless_lead**: el loop original no empalma perfecto, así que le puse un micro-fade de 2 ms. Escuchá el cierre del loop.
- **mus_desafio / mus_contrarreloj**: no traían BPM; los estimé (120 y ~88,8) y corté los loops en compases exactos. Escuchá el empalme.
- **stg_nuevo_record**: dura 1,44 s, un poco menos que los 1,5 s pedidos (la alternativa dura 2,07 s).
- **amb_desierto**: viento seco sin insectos; no encontré grillos o cigarras que pegaran. Si querés insectos, busco una capa más.
- **amb_plaza**: tiene parque y chicos, pero el tráfico se oye poco.
- **ball_air, hoop_move_loop, carousel_loop**: son loops cortos armados con crossfade; escuchalos en repetición.
- Las fuentes son previews HQ de Freesound (OGG ~192 kbps); en el juego se recomprimen igual a Vorbis/ADPCM.
