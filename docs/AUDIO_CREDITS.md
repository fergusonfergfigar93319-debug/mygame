# Audio Credits

This project uses a mixed audio pipeline:

- BGM and ambient wind are external CC0 game-audio resources downloaded from OpenGameArt.
- Short SFX are procedurally generated for this project and can be used in the app without third-party attribution.

## External CC0 Audio

| Project file | Source |
| --- | --- |
| `Assets/Resources/PenguinRun/bgm_endless.ogg` / `app/src/main/res/raw/bgm_endless.ogg` | OpenGameArt direct asset: https://opengameart.org/sites/default/files/snow_stage_bpm162.ogg |
| `Assets/Resources/PenguinRun/bgm_lobby_cozy.ogg` / `app/src/main/res/raw/bgm_lobby_cozy.ogg` | OpenGameArt direct asset: https://opengameart.org/sites/default/files/rememberWinter.ogg |
| `Assets/Resources/PenguinRun/bgm_story.ogg` / `app/src/main/res/raw/bgm_story.ogg` | OpenGameArt direct asset: https://opengameart.org/sites/default/files/theme-loop.ogg |
| `Assets/Resources/PenguinRun/ambience_wind.ogg` | OpenGameArt direct asset: https://opengameart.org/sites/default/files/wind.ogg |

## Generated Project SFX

The following files were generated locally with a procedural synthesizer for this project:

- `jump.wav`
- `land.wav`
- `slide.wav`
- `coin_pickup.wav`
- `eat_fish.wav`
- `ui_select.wav`
- `power_up.wav`
- `magnet_pickup.wav`
- `shield_bounce.wav`
- `ice_crack.wav`
- `boss_land.wav`
- `boss_shield_break.wav`
- `boss_defeat.wav`
- `dash_pickup.wav`
- `score_star.wav`
- `glide_feather.wav`
- `crash.wav`
- `checkpoint.wav`

Each generated SFX exists in both:

- `app/src/main/res/raw/`
- `Assets/Resources/PenguinRun/`

## Generated Per-Theme Audio

Built by `tools/generate_theme_audio.py` from noise and oscillators only, so they
carry no third-party licence. The generator seeds each theme deterministically,
so a rebuild is byte-identical and diffs stay reviewable.

| File | Theme | Texture | Spectral centroid |
| --- | --- | --- | --- |
| `ambience_theme_ice_lake.wav` / `surface_ice_lake.wav` | IceLakeEcho | Low wind, sparse cracks echoing over the lake | 2543 Hz |
| `ambience_theme_cedar_ruins.wav` / `surface_cedar_ruins.wav` | CedarRuins | Mid-band woodland wind, slow timber creaks | 1275 Hz |
| `ambience_theme_aurora_field.wav` / `surface_aurora_field.wav` | AuroraField | Electromagnetic hum, sweeping hiss | 4945 Hz |
| `ambience_theme_mist_dike.wav` / `surface_mist_dike.wav` | MistDike | Muffled low bed, distant horn | 449 Hz |
| `ambience_theme_ocean_reef.wav` / `surface_ocean_reef.wav` | OceanReef | Slow surge, rising bubble trails | 879 Hz |
| `ambience_theme_sky_flight.wav` / `surface_sky_flight.wav` | SkyFlight | Broadband rush, thin high whistle | 3189 Hz |

Ambience beds are 8 s mono at 22.05 kHz and loop seamlessly (the tail is
crossfaded back over the head). `tools/VerifyThemeAudioDifferentiation.ps1`
holds every theme to a distinct bed and surface timbre.

Regenerate with:

```powershell
python tools/generate_theme_audio.py
```
