# Tomorrow — Chapters 1–3

Open **`Assets/Game/Scenes/Title.unity`** and press **Play**. Pick a chapter. Finishing a chapter moves on to the next one.

| Scene | Story |
|---|---|
| Chapter1 | The workshop and the town. Borrow, use, build, return. |
| Chapter2 | The harbor. Two debts at once; the earlier one comes first. |
| Chapter3 | Market day and the greenhouse. Pink boxes from friends' tomorrows; late costs that friend a heart. |

## Controls
**Click inside the Game window first** so it receives the keyboard. The on-screen **MOVE**, **USE** and **NEXT** buttons work with the mouse too.

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Walk | WASD or arrow keys | Left stick |
| Use / talk / pick up | **E** | A (south) |
| Next line of dialogue | Space, E, Enter or click | A |
| Choose | 1 or 2, or click | — |
| Turn the camera | Hold right mouse and drag | Right stick |
| Zoom | Mouse wheel | — |
| Photo (the frozen seagull, Chapter 2) | P | — |

## How the chapters play
- **Due Board** (top-left): every open loan, earliest first, with its due time and recipe.
  - The Return Box only takes loans in order: the earlier one comes first.
- **Workbench and Return Box:** every chapter has its own. They're in the workshop in Chapter 1, by the harbor in Chapter 2, at the market in Chapter 3, plus Grandma's toolbox on the island.
- **Late returns:**
  - From tomorrow (blue box): you owe one extra part of interest. It appears by the Return Box.
  - From a friend (pink box): that friend loses a heart, and some loans have their own scene (the frozen seagull, Nell by the roses).
- **Hearts** (top-right) show how each friend feels.

## Voices
- Every line in the script is spoken. Clips are in `Assets/Game/Resources/Voice` (143 lines, 24 kHz mono).
- They are made by `Tools/make_voices.py` with **Kokoro**, an open-source text-to-speech model under the
  Apache 2.0 licence, which allows commercial use of the audio. It runs offline on a Mac or PC, with no paid service.

  | Character | Kokoro voice | Speed |
  |---|---|---|
  | Pip | am_puck | 1.05 |
  | Older Pip | am_michael | 0.95 |
  | Tock | bm_fable | 1.12 |
  | Captain Oskar | bm_george | 0.92 |
  | Nell | af_sky | 1.04 |
  | Mira | af_heart | 1.0 |
  | Grandma Ada | bf_emma | 0.9 |
  | Bram | bm_lewis | 1.0 |
  | Notes / narrator | bf_isabella | 0.97 |

- **One-time setup** (about 350 MB, kept outside the project): see the commands at the top of `Tools/make_voices.py`.
- **After you change any dialogue:** run `~/Library/Caches/tomorrow-tts/venv/bin/python Tools/make_voices.py`.
  It makes the new lines and removes clips for lines that no longer exist. Use `--all` to remake everything
  and `--cast` to show the cast.
- Each clip is named after the first 12 hex characters of md5("SPEAKER|line"). `GameUI.VoiceKey()` finds it at runtime.
  Lines without a clip simply play silently, so voice actors' recordings can replace the clips one by one.
- All clips are levelled to the same loudness.

## Music
- Four looping tracks in `Assets/Game/Resources/Music`, synthesised by `Tools/make_music.py` (no samples, no online service):
  title (music box), Chapter 1 workshop (electric piano), Chapter 2 harbor (accordion and whistle, 6/8), Chapter 3 market (marimba and ukulele).
- `MusicPlayer` starts by itself, fades between tracks when the scene changes, and gets quieter while someone speaks.
- Remake: `/Applications/Blender.app/Contents/Resources/5.1/python/bin/python3.13 Tools/make_music.py` (needs numpy, which Blender's Python has). Change chords and tempos in that file.

## Look and feel
- **Logo and icon:** `Tools/make_logo.py` (renders in Blender in the background) makes `Resources/UI/Logo.png` (title screen) and `Art/Icon.png` (app icon, set by the build menu).
- **Font:** Fredoka (`Resources/Fonts/Fredoka.ttf`, free OFL licence, see `OFL.txt`) for all text.
- **UI:** rounded warm panels (`GameUI.RoundedSprite`), brown and brass palette.
- **Colour grade:** `Assets/Game/Settings/Tomorrow_Warm.asset`, rebuilt by `Chapter1Builder.PostProfile()`: warmer white balance, split toning (cool shadows, golden highlights), soft brown vignette.
- **Buildings:** `Tools/make_buildings.py` models Workshop_Front, Clock_Tower, Striped_Awning, Market_Stall, Fountain and Park_Bench (all quads, UV'd, small `.glb` files in `Assets/Models/Props`, sources in `ModelSources/Buildings`). Run it with
  `/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python Tools/make_buildings.py`, then **Tomorrow ▸ Build All Scenes**.

## Changing things
- **Rebuild every scene:** **Tomorrow ▸ Build All Scenes**. This overwrites hand edits to the scenes, so change the builder scripts instead:
  - `Assets/Game/Editor/Chapter1Builder.cs`: the town, plus the shared building helpers
  - `Chapter2Builder.cs`: the harbor and the island
  - `Chapter3Builder.cs`: the market and greenhouse extras
  - `TitleBuilder.cs`: the title scene
- **Story and dialogue:** `Assets/Game/Scripts/Chapter1Director.cs`, `Chapter2Director.cs`, `Chapter3Director.cs`.
- **Shared rules** (boxes, Due Board, hearts, workbench, Return Box): `ChapterDirector.cs`.
- **Day length:** `DayClock.secondsPerHour`, set per chapter in each director's `Setup()`.
- **Models:**
  - `Assets/Models`
  - Pip and Tock have separate legs and arms for walking. Their source files are in `ModelSources/`.
  - Mira is a recoloured Nell, standing in until you have her own art.
- **Playtest bot:** `Assets/Game/Scripts/Debug/Autopilot.cs` (not used in normal play).
