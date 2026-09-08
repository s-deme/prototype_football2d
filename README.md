# Spark Strikers

Unity 2D arcade football game. A no-fouls three-rival Arcade Cup combines rough hits, knockdowns, loose-ball scrambles, quick passing, charge shots, escalating AI, rival-specific special moves, procedural sound, persistent records, English/Japanese UI, and unlockable secrets—including a hidden fourth challenger after the first championship.

![Title screen](Screenshots/title-1280x720.png)

![Match screen](Screenshots/match-1280x720.png)

## Play

1. Open the repository with Unity `6000.3.22f1` (Unity 6.3 LTS).
2. Open `Assets/Scenes/Main.unity`.
3. Enter Play Mode.

Art, effects, and the looping stadium ambience are generated in-game. The included Noto Sans JP font is licensed under OFL 1.1; its notice is in `ThirdPartyNotices.txt` and is copied beside every Windows build.

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Move | WASD / arrows | Left stick |
| Shoot | Hold and release Z / Space | A |
| Pass / rough hit (without the ball) | X | X |
| Dash / tackle | Shift | B |
| Switch player | Q | LB |
| Special | C | Y |
| Pause | Esc | Menu |

Menus support the keyboard or left stick, with A to confirm and B to go back.
Windowed play maintains a minimum 1024×576 viewport so menus and HUD text remain readable.

There are no fouls: a rough hit or running tackle briefly knocks its target down and spills a carried ball. Successful hits also charge the attacker's special meter, so fighting for space is part of the scoring strategy.

Quit, restart-match, and mid-match return-to-title actions use a confirmation screen that defaults to cancel.
Active matches pause automatically when the game loses focus; returning to the window never resumes play without input.

## Special shots

- `STAR BREAKER` is a fast, straight power shot.
- `BLAZE CANNON` accelerates and has extra knockback.
- `ZERO DRIVE` slows defenders it touches.
- `ECLIPSE ARC` changes direction mid-flight.
- The champion-only `SUPERNOVA STRIKE` launches fastest and weaves through defense.
- The hidden `COMET BREAKER` bends toward goal with a rainbow trail.

The UI defaults to the operating-system language for English or Japanese and can be changed in Settings. ROOKIE, ARCADE, and LEGEND difficulty levels tune rival movement, decisions, and special-shot charge. Screen shake, full-screen flashes, and high-contrast team colors can be changed independently.

A tied match continues as Golden Goal; the next score decides the round immediately.

## Support

Report problems through the [GitHub issue tracker](https://github.com/s-deme/prototype_football2d/issues). Include the game version shown on the title screen, reproduction steps, and the Windows player log from `%USERPROFILE%\AppData\LocalLow\Spark Strikers\Spark Strikers\Player.log`.
