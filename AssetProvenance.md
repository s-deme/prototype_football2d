# Spark Strikers asset provenance

This ledger records the origin and release status of distributable assets. Do not mark the project ready for commercial release while an entry is pending.

| Asset | Origin | License or rights status |
| --- | --- | --- |
| Runtime field, players, ball, effects, and audio | Generated at runtime by `Assets/Scripts/ArcadeFootballGame.cs` | Original project code; ready |
| `Assets/Resources/Fonts/NotoSansJP-Regular.otf` | Noto Sans CJK from the notofonts project | SIL Open Font License 1.1; notice included in `ThirdPartyNotices.txt`; ready |
| `Assets/Brand/SparkStrikersIcon.png` | Added with repository commit `408921d` | Original authoring/export record is not present; publisher confirmation required |
| `Steam/StoreAssets/Masters/key-art-landscape.png` | Added with repository commit `408921d` | Original authoring/export record is not present; publisher confirmation required |
| `Steam/StoreAssets/Masters/key-art-vertical.png` | Added with repository commit `408921d` | Original authoring/export record is not present; publisher confirmation required |
| `Steam/StoreAssets/Masters/key-art-library-hero.png` | Added with repository commit `408921d` | Original authoring/export record is not present; publisher confirmation required |
| Steam capsules, library images, and community icons | Deterministically derived by `Tools/GenerateSteamCapsules.ps1` from the masters and icon above | Inherits the source asset status |
| Steam gameplay screenshots | Captured from the built game | Original project output; ready |

For each pending source asset, archive its editable source or generation/commission receipt and written commercial-distribution terms, then replace “publisher confirmation required” with the evidence location and approval date.
