# Lucky Sevens — Unity Slot Machine

A classic 3-reel slot machine built in **Unity 6 (6000.3.11f1)** with the provided pixel-art cabinet, symbols and UI kit. It features a cryptographically secure RNG, physically-inspired reel animation, a transparent paytable balanced to **~96.8% RTP**, and bonus features (Wilds, Scatters and Free Spins).

> **Play it:** open `Build/WebGL/index.html` through a web server — see [Running the WebGL build](#running-the-webgl-build).

---

## Game Overview

| | |
|---|---|
| **Layout** | 3 reels × 3 visible rows, one payline (the centre row) |
| **Win** | All three reels show the same symbol on the payline |
| **Bet levels** | 5 · 10 · 25 · 50 · 100 credits (starting balance: 1,000) |
| **Controls** | **SPIN** button, pull the **lever**, or press **Space / Enter**. Arrows change the bet. |

**How a round plays**

1. Press SPIN (or pull the lever). The bet is deducted and the lever animates.
2. Reels kick back, accelerate, spin, then stop left-to-right with a small bounce.
3. Winning symbols pulse inside a gold frame while the rest dim; the win and balance count up.
4. Press SPIN again **while spinning** to quick-stop, or **during a celebration** to skip it.

### Paytable (pays × bet for 3 on the payline)

| Symbol | Pays | Stops per reel (of 31) | Chance of 3 on the line\* |
|---|---:|---:|---:|
| 🍒 Cherry | ×3 | 10 | 1 in 22 |
| 🔔 Bell | ×6 | 8 | 1 in 41 |
| BAR | ×12 | 6 | 1 in 87 |
| 7 Seven | ×35 | 4 | 1 in 240 |
| ⭐ WILD | ×100 (three Wilds) | 1 | 1 in 29,791 |
| 💎 SCATTER | 3 anywhere → 10 free spins + ×2 | 2 | 1 in 138 |

\*Including Wild substitutions.

---

## Bonus Features

- **WILD (star)** — substitutes for any regular symbol on the payline. Each Wild in a win **doubles** it (one Wild ×2, two Wilds ×4). Three Wilds pay the top prize, ×100.
- **SCATTER (gem) → Free Spins** — three Scatters **anywhere** in the 3×3 window pay ×2 and award **10 free spins**. During free spins all line wins are **doubled**, spins play automatically at the triggering bet, re-triggers add more spins, and a **Feature Win** summary is shown at the end.
- **Anticipation** — when the last reel decides a feature (two Scatters already showing) or a big line (7-7-? or WILD-7-?), it slows down for extra suspense. This uses only symbols already visible, so it never leaks the result.
- **Quick-stop and skip** — SPIN while spinning slams the reels; SPIN during a win skips the count-up.
- **Win tiers** — Big (≥5× bet) and Mega (≥20× bet) wins get an animated banner and fanfare.
- **Out-of-credits refill** — YES/NO dialog offers a top-up back to the starting balance.
- **Sound** — lever ratchet, spin loop, reel thunks with rising pitch, coin ticks, jingles; mute toggle (remembered).

---

## Running the WebGL build

Browsers block WebGL builds opened directly from disk (`file://`), so serve the folder over HTTP.

**Option A — GitHub Pages (recommended for sharing)**
1. In the repository: **Settings → Pages → Build and deployment → Deploy from a branch**, branch `main`, folder `/ (root)`.
2. Open `https://<your-user>.github.io/<repo-name>/Build/WebGL/`.

**Option B — Local web server**
```bash
cd Build/WebGL
python -m http.server 8000      # or: npx serve .
```
Then open <http://localhost:8000>.

The build uses gzip compression with Unity's *decompression fallback*, so it runs on any static host (including GitHub Pages) without special server headers.

---

## Opening and building the project

1. Install **Unity 6 (6000.3.11f1)** with the **WebGL Build Support** module and open the repository folder in Unity Hub.
2. Open `Assets/Scenes/SlotGame.unity` and press **Play**.
   *If the scene is missing* (e.g. it was deleted), Unity offers to generate it on first open, or run **Slot Game → Build Scene and Assets**.
3. **Slot Game → Build WebGL** writes the build to `Build/WebGL/`.
   CI / command line:
   ```bash
   Unity -batchmode -quit -projectPath . -executeMethod SlotGame.EditorTools.WebGLBuilder.BuildFromCommandLine
   ```
4. **Slot Game → Run RTP Simulation (1M spins)** plays a million spins with the real game code and reports RTP, hit rate and feature frequency.
5. **Window → General → Test Runner → EditMode** runs the unit tests.

---

## Thought Process & Approach

### 1. Keep the maths separate from Unity
Everything that decides money lives in `Assets/Scripts/Core` — an assembly with `noEngineReferences: true`, so it **cannot** depend on frames, animation or scene state. That gave three benefits:

- The rules are unit-tested in isolation (25 EditMode tests).
- The same code powers the game, the tests and the RTP simulator, so balancing numbers are the real numbers.
- Unity-side code is purely presentation, which keeps every class small with one job.

### 2. Fair, unpredictable RNG
- `SecureRandomProvider` uses the platform's **cryptographic RNG** (`RandomNumberGenerator`) with **rejection sampling**, so every value in a range is exactly equally likely (a plain `random % n` is biased towards small numbers). If a platform lacks a crypto RNG it falls back to a GUID-seeded `System.Random`.
- The RNG sits behind `IRandomProvider`, so tests and simulations swap in a seeded generator for reproducibility (a *Fixed Seed* debug toggle on the controller does the same in-game).
- Tests verify range coverage and uniformity (chi-squared, p = 0.001).

### 3. Outcome first, animation second
Like real machines, each reel is a **virtual reel strip** (31 stops, symbols repeated by weight). A spin draws **one uniform stop per reel** *before anything moves*; the reels are then animated to land on those stops. The animation can never change a result, and odds are auditable: P(symbol) = weight ÷ 31.

Scatters are kept at least 3 stops apart on every strip. Without this, a shuffle that put two Scatters next to each other could show both on one reel and silently raise that session's feature odds. With it, RTP is identical for every shuffle (the simulator confirms 96.4–97.6% across seeds against 96.8% theory).

### 4. Smooth, realistic reels
`ReelController` models a reel as a single float position along its strip; rendering is a pure function of that position, using a pool of just 5 recycled symbol views per reel. The motion has five phases:

| Phase | Curve | Purpose |
|---|---|---|
| Wind-up | ease in-out | small upward kick like a real reel |
| Accelerate | ease-in | the reel "catches" before full speed |
| Spin | constant | ~22 symbols/s, with fake motion blur (stretch + fade) |
| Decelerate | ease-out cubic | duration set to **3 × distance ÷ speed**, so the starting slope equals the current speed — no visible jerk |
| Settle | ease in-out | overshoot by ~0.2 symbols, then bounce back onto the exact stop |

All timing lives in a `ReelAnimationProfile` asset, so the feel can be tuned live in Play Mode.

### 5. Balancing the paytable
I solved for expected value analytically (line wins with 0–2 Wild substitutions, plus a free-spin feature expressed as a geometric series to include re-triggers), then confirmed with Monte-Carlo runs:

- **Theoretical RTP: 96.8%** (line wins 82.4%, scatter/free-spin feature 14.4%)
- **Hit frequency:** ~9% of spins

### 6. Data-driven, designer-friendly
Symbols (`SymbolData`), the paytable/economy (`SlotConfig`) and the reel feel (`ReelAnimationProfile`) are ScriptableObjects. The paytable popup is generated from `SlotConfig`, so it cannot disagree with the maths.

### 7. Reproducible project setup
Scene, prefabs, AnimationClips and AnimatorControllers are generated by editor code (`SlotSceneBuilder`, `AnimationAssetBuilder`), so the whole project, including every animation, can be rebuilt from scripts. The art was prepared by `Tools/build_art.py` (slices button state sheets, crops cabinet layers, draws the Wild/Scatter symbols in the provided palette); sound effects are synthesised by `Tools/build_sounds.py`.

### Architecture

```
Core (pure C#)              Unity presentation
──────────────              ──────────────────────────────────────────
IRandomProvider             SlotMachineController  (state machine: Idle → Spinning → PresentingWin)
 ├ SecureRandomProvider       ├ ReelController ×3  → SymbolView pool (Animator: win pulse)
 └ SeededRandomProvider       ├ WinPresenter       → highlights, count-up, WinBanner (Animator)
ReelStrip (virtual reel)      ├ SlotHUD            (passive view, raises events)
PaytableEvaluator             ├ LeverView          (Animator: pull)
CreditWallet                  ├ PaytablePanel / ConfirmDialog (PopupView, Animator: fade/scale)
GameSession  ◀── owns ──────  └ AudioManager
SlotMathSimulator
```

`GameSession` splits a round into `TryBeginSpin` (take stake, decide outcome) and `Settle` (pay win, award free spins), and refuses to start a new round until the previous one is settled.

---

## Project Structure

```
Assets/
├── Animations/      Generated clips + AnimatorControllers (Lever, SymbolView, WinBanner, Popup)
├── Data/            SlotConfig, ReelAnimationProfile, Symbols/*.asset
├── Prefabs/         SymbolView, Reel, SlotMachine, ControlPanel, PaytableRow, PaytablePanel, ConfirmDialog
├── Scenes/          SlotGame.unity
├── Scripts/
│   ├── Core/        Pure C# game maths (own assembly, no UnityEngine)
│   ├── Data/        ScriptableObject definitions
│   ├── Reels/       Reel animation + symbol view
│   ├── Game/        SlotMachineController, WinPresenter
│   ├── UI/          HUD, lever, popups, paytable, banner
│   ├── Audio/       AudioManager
│   └── Editor/      Scene/prefab/animation generators, WebGL build, RTP simulation
├── Sounds/          Synthesised WAV effects
├── Tests/EditMode/  NUnit tests for the maths
└── UI/              Sprites (Symbols, Machine, Buttons, Panels, Sprites) and Fonts
Build/WebGL/         WebGL build output
SourceArt/           Original provided art (unmodified)
Tools/               Python art and sound pipelines
```

---

## Credits

- Cabinet, reel, lever, symbol (Cherry, Bell, BAR, 7) and UI button/panel art: provided with the assignment. WILD and SCATTER symbols drawn to match.
- Font: [Bebas Neue](https://github.com/dharmatype/Bebas-Neue) — SIL Open Font License (`Assets/UI/Fonts/BebasNeue-OFL.txt`).
- Sound effects: synthesised for this project (`Tools/build_sounds.py`).
