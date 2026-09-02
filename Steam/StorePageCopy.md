# Spark Strikers — Steam store copy

Only advertise items marked **ready** below. The current build supports English and Japanese in-game text.

## English

### Short description

Charge impossible shots and conquer a three-rival arcade cup in fast 3-on-3 soccer. Pass, dash, tackle, and unleash spectacular specials—then discover the secret shot hidden beyond the final whistle.

### About this game

[h2]THREE RIVALS. ONE CUP. IMPOSSIBLE SHOTS.[/h2]

Spark Strikers is a fast single-player 3-on-3 arcade soccer game built around quick decisions and spectacular finishers. Keep possession, pass through pressure, and take charged shots to fill the Spark Meter. When it reaches maximum, unleash a match-changing special.

[h2]FACE THREE SPECIALIST TEAMS[/h2]

Fight through the Arcade Cup against BLAZE, FROST, and ECLIPSE. Each rival changes the match with a distinct special: explosive knockback, freezing slowdown, or a shot that bends through your defense.

[h2]FIND THE IMPOSSIBLE SHOT[/h2]

Old button legends still echo through the stadium. Discover the hidden COMET BREAKER and turn the pitch into a rainbow trail of sparks.

[list]
[*]Fast 3-on-3 single-player arcade soccer
[*]Charged shots, passes, dashes, tackles, and manual player switching
[*]Distinct spectacular special-shot styles
[*]Three escalating cup rivals and sudden-death Golden Goal
[*]ROOKIE, ARCADE, and LEGEND difficulty levels
[*]A persistent secret unlock and cup records
[*]Keyboard and gamepad play
[*]Adjustable screen shake, full-screen flashes, high-contrast team colors, and volume
[/list]

## 日本語

### 短い説明

必殺シュートをためて放ち、3つのライバルチームが待つ3対3のアーケードカップを勝ち抜こう。パス、ダッシュ、タックル、ド派手な必殺技、そして伝説の隠しシュートが試合を熱くする。

### このゲームについて

[h2]3つのライバル、1つのカップ、常識外れの必殺シュート。[/h2]

『Spark Strikers』は、素早い判断とド派手なフィニッシュを楽しむ1人用3対3アーケードサッカーゲームです。パスで守備を崩し、チャージシュートを狙い、ボールを支配してSPARK METERをためましょう。満タンになれば、試合をひっくり返す必殺技を放てます。

[h2]必殺技を持つ3チームに挑め[/h2]

BLAZE、FROST、ECLIPSEが待つアーケードカップを勝ち抜きましょう。強烈な吹き飛ばし、選手を鈍らせる冷気、守備を曲がり抜ける軌道など、相手ごとに異なる必殺技が襲いかかります。

[h2]伝説の隠しシュートを探せ[/h2]

スタジアムには、昔から語り継がれるボタン伝説があります。隠された「COMET BREAKER」を見つけ、虹色の火花でピッチを切り裂きましょう。

[list]
[*]テンポよく遊べる1人用3対3アーケードサッカー
[*]チャージシュート、パス、ダッシュ、タックル、選手切り替え
[*]特徴の異なる必殺シュート
[*]強さを増す3チームと、同点時のゴールデンゴール
[*]ROOKIE、ARCADE、LEGENDの3段階難易度
[*]セーブされる隠し要素とカップ戦績
[*]キーボード／ゲームパッド操作
[*]画面揺れ、全画面フラッシュ、ハイコントラスト配色、音量を個別設定
[/list]

## Steam fields

- Genres: Action, Sports, Indie
- Suggested tags: Soccer, Sports, Arcade, Action, 2D, Singleplayer, Controller
- Features ready now: Single-player
- Add after hands-on hardware verification: Full Controller Support
- Add after dashboard configuration and verification: Steam Cloud
- Interface: English and Japanese; no voiced dialogue or subtitles
- Mature content: None
- Support: https://github.com/s-deme/prototype_football2d/issues

## Provisional Windows system requirements

These values are conservative release-page placeholders and must be confirmed on representative low-end hardware before submission.

| | Minimum | Recommended |
| --- | --- | --- |
| OS | Windows 10 21H1 (64-bit) | Windows 11 (64-bit) |
| Processor | x64 CPU with SSE2 support | Dual-core 2.5 GHz or faster |
| Memory | 4 GB RAM | 8 GB RAM |
| Graphics | DirectX 10-capable GPU | DirectX 11-capable GPU |
| DirectX | Version 10 | Version 11 |
| Storage | 200 MB available space | 200 MB available space |

Minimum supported viewport: 1024×576.

## Screenshot alt text

1. Blue player launches the rainbow-trailing COMET BREAKER toward an orange goal.
2. BLAZE fires a red-orange BLAZE CANNON across the neon soccer pitch.
3. FROST unleashes the icy blue ZERO DRIVE as defenders close in.
4. ECLIPSE bends a purple ECLIPSE ARC around the player defense.
5. Blue-team players celebrate a goal amid confetti and bright stadium effects.

## External release gates

- [ ] Complete title-to-quit play with a physical gamepad, including disconnect/reconnect and pause; only then enable **Full Controller Support**.
- [ ] Listen on speakers and headphones at 0%, 50%, and 100% volume, checking title ambience, kicks, specials, goals, and pause transitions.
- [ ] Run the final build on representative minimum-spec hardware and replace the provisional system requirements with measured values.
- [ ] After App ID and Depot ID assignment, create the [SteamPipe build scripts](https://partner.steamgames.com/doc/sdk/uploading), upload to a password-protected test branch, install through Steam, and verify `SparkStrikers.exe` as the launch option.
- [ ] Configure [Steam Auto-Cloud](https://partner.steamgames.com/doc/features/cloud): `WinAppDataLocalLow`, `Spark Strikers/Spark Strikers`, `progress.sav`, Windows, non-recursive; publish it and verify upload/download on two PCs.
- [ ] Confirm and archive the commercial-distribution evidence requested by `AssetProvenance.md` for the icon and three Steam key-art masters.
- [ ] Submit the store page first and then the default-branch build for [Valve review](https://partner.steamgames.com/doc/store/Review_Process) at least seven business days before the intended release.
