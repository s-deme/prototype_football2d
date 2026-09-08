using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SparkStrikers
{
    public sealed class ArcadeFootballGame : MonoBehaviour
    {
        const float FieldHalfWidth = 8f;
        const float FieldHalfHeight = 4.35f;
        const float GoalHalfHeight = 1.45f;
        const float MatchLength = 75f;
        const int StandardRivalCount = 3;
        const int MinimumWindowWidth = 1024;
        const int MinimumWindowHeight = 576;

        static readonly KeyCode[] SecretCode =
        {
            KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.DownArrow,
            KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.Z, KeyCode.X
        };

        static readonly string[] RivalNames = { "BLAZE", "FROST", "ECLIPSE", "NOVA" };
        static readonly string[] RivalMarks = { "B", "F", "E", "N" };
        static readonly string[] RivalSpecials = { "BLAZE CANNON!", "ZERO DRIVE!", "ECLIPSE ARC!", "SUPERNOVA STRIKE!" };
        static readonly string[] DifficultyNames = { "ROOKIE", "ARCADE", "LEGEND" };
        static readonly string[] JapaneseDifficultyNames = { "ルーキー", "アーケード", "レジェンド" };
        static readonly Color[] RivalColors =
        {
            new Color(1f, 0.28f, 0.12f),
            new Color(0.55f, 0.35f, 1f),
            new Color(0.95f, 0.18f, 0.62f),
            new Color(1f, 0.72f, 0.12f)
        };

        sealed class Footballer
        {
            public GameObject GameObject;
            public SpriteRenderer Body;
            public TextMesh Mark;
            public Vector2 Position;
            public Vector2 Velocity;
            public Vector2 Facing;
            public Vector2 StartPosition;
            public bool Home;
            public int Index;
            public float DecisionTimer;
            public float StealCooldown;
            public float SlowTimer;
            public float DownTimer;
        }

        sealed class BallState
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public Footballer Owner;
            public Footballer LastTouch;
            public float PickupLock;
            public SpecialShotKind SpecialKind;
            public float SpecialAge;
            public float CurveDirection;
            public float TrailTimer;
            public bool IsSpecial => SpecialKind != SpecialShotKind.None;
        }

        enum MenuPage
        {
            Main,
            Help,
            Settings
        }

        enum ConfirmAction
        {
            None,
            Quit,
            RestartMatch,
            ReturnToTitle
        }

        readonly List<Footballer> home = new List<Footballer>();
        readonly List<Footballer> away = new List<Footballer>();
        readonly List<Footballer> everyone = new List<Footballer>();

        MatchRules rules;
        ArcadeCup cup;
        BallState ball;
        Footballer controlled;
        Camera gameCamera;
        Transform worldRoot;
        GameObject selectionRing;
        Sprite circleSprite;
        Sprite squareSprite;
        Sprite ballSprite;
        Texture2D circleTexture;
        Texture2D squareTexture;
        Material lineMaterial;
        Material particleMaterial;
        Font font;
        AudioSource audioSource;
        AudioSource ambienceSource;
        AudioClip kickSound;
        AudioClip passSound;
        AudioClip whistleSound;
        AudioClip goalSound;
        AudioClip specialSound;

        MenuPage menuPage;
        ConfirmAction confirmAction;
        int menuIndex;
        int confirmIndex = 1;
        int secretCodeIndex;
        int activeRound;
        int cupsWon;
        int bestRound;
        bool secretUnlocked;
        bool resultRecorded;
        bool shootWasHeld;
        bool shakeEnabled;
        bool flashEnabled;
        bool highContrast;
        bool japanese;
        bool saveFailed;
        GameDifficulty difficulty;
        bool menuVerticalReady = true;
        bool menuHorizontalReady = true;
        bool automationRun;
        float masterVolume;
        float kickoffTimer;
        float shotCharge;
        float rivalMeter;
        float selectTimer;
        float manualControlLock;
        float cameraShake;
        float flash;
        float bannerTimer;
        float celebrationTimer;
        float celebrationSide;
        string banner = string.Empty;
        string progressPath;
        string screenshotPath;
        float screenshotDelay = 1.5f;
        int screenshotFrames = 1;
        CupOutcome cupOutcome;

        GUIStyle titleStyle;
        GUIStyle subtitleStyle;
        GUIStyle headingStyle;
        GUIStyle bodyStyle;
        GUIStyle centeredBodyStyle;
        GUIStyle buttonStyle;
        GUIStyle hudStyle;
        GUIStyle hugeStyle;
        GUIStyle smallStyle;

        Color homeColor => highContrast ? new Color(0.1f, 0.9f, 1f) : new Color(0.05f, 0.55f, 1f);
        Color awayColor => highContrast ? new Color(1f, 0.25f, 0.85f) : RivalColors[activeRound];
        string opponentName => RivalNames[activeRound];
        int availableRivalCount => cupsWon > 0 ? RivalNames.Length : StandardRivalCount;
        string difficultyName => (japanese ? JapaneseDifficultyNames : DifficultyNames)[(int)difficulty];
        string T(string english, string japaneseText) => japanese ? japaneseText : english;
        string ToggleText(bool value) => T(value ? "ON" : "OFF", value ? "オン" : "オフ");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<ArcadeFootballGame>() == null)
                new GameObject("Spark Strikers").AddComponent<ArcadeFootballGame>();
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;

            string[] arguments = System.Environment.GetCommandLineArgs();
            int screenshotArgument = System.Array.IndexOf(arguments, "--capture-proof");
            automationRun = screenshotArgument >= 0 || System.Array.IndexOf(arguments, "-smoke-test") >= 0;
            Application.runInBackground = automationRun;
            progressPath = Path.Combine(automationRun ? Application.temporaryCachePath : Application.persistentDataPath,
                automationRun ? "spark-strikers-automation-progress.sav" : "progress.sav");
            if (automationRun) DeleteAutomationProgress();

            masterVolume = automationRun ? 0.8f : PlayerPrefs.GetFloat("volume", 0.8f);
            shakeEnabled = automationRun || PlayerPrefs.GetInt("screen_shake", 1) == 1;
            flashEnabled = automationRun || PlayerPrefs.GetInt("flash_effects", 1) == 1;
            highContrast = !automationRun && PlayerPrefs.GetInt("high_contrast", 0) == 1;
            japanese = !automationRun && PlayerPrefs.GetInt("language", Application.systemLanguage == SystemLanguage.Japanese ? 1 : 0) == 1;
            if (System.Array.IndexOf(arguments, "--capture-japanese") >= 0) japanese = true;
            else if (System.Array.IndexOf(arguments, "--capture-english") >= 0) japanese = false;
            difficulty = automationRun ? GameDifficulty.Arcade :
                (GameDifficulty)Mathf.Clamp(PlayerPrefs.GetInt("difficulty", (int)GameDifficulty.Arcade), 0, DifficultyNames.Length - 1);
            if (automationRun)
            {
                secretUnlocked = false;
                cupsWon = 0;
                bestRound = 1;
            }
            else LoadProgress();

            rules = new MatchRules(MatchLength);
            cup = new ArcadeCup(availableRivalCount);
            BuildAssets();
            BuildWorld();
            BuildAudio();
            ResetPositions();
            if (screenshotArgument >= 0 && screenshotArgument + 1 < arguments.Length)
            {
                screenshotPath = arguments[screenshotArgument + 1];
                if (System.Array.IndexOf(arguments, "--capture-high-contrast") >= 0) highContrast = true;
                if (System.Array.IndexOf(arguments, "--capture-hidden-rival") >= 0)
                {
                    cupsWon = Mathf.Max(cupsWon, 1);
                    cup = new ArcadeCup(availableRivalCount);
                }
                int roundArgument = System.Array.IndexOf(arguments, "--capture-round");
                if (roundArgument >= 0 && roundArgument + 1 < arguments.Length &&
                    int.TryParse(arguments[roundArgument + 1], out int requestedRound))
                {
                    for (int round = 1; round < Mathf.Clamp(requestedRound, 1, cup.RoundCount); round++)
                        cup.RecordResult(1, 0);
                }
                if (System.Array.IndexOf(arguments, "--capture-match") >= 0) StartMatch();
                if (System.Array.IndexOf(arguments, "--capture-focus-pause") >= 0)
                {
                    StartMatch();
                    rules.StartPlay();
                    OnApplicationFocus(false);
                }
                if (System.Array.IndexOf(arguments, "--capture-result") >= 0)
                {
                    StartMatch();
                    rules.StartPlay();
                    rules.Tick(MatchLength);
                    rules.ScoreGoal(false);
                    RecordCupResult();
                }
                if (System.Array.IndexOf(arguments, "--capture-golden") >= 0)
                {
                    StartMatch();
                    rules.StartPlay();
                    rules.Tick(MatchLength);
                    AnnounceGoldenGoal();
                }
                if (System.Array.IndexOf(arguments, "--capture-settings") >= 0)
                {
                    menuPage = MenuPage.Settings;
                    menuIndex = 1;
                }
                if (System.Array.IndexOf(arguments, "--capture-help") >= 0) menuPage = MenuPage.Help;
                if (System.Array.IndexOf(arguments, "--capture-pause-settings") >= 0)
                {
                    StartMatch();
                    rules.StartPlay();
                    rules.TogglePause();
                    menuPage = MenuPage.Settings;
                    menuIndex = 1;
                }
                if (System.Array.IndexOf(arguments, "--capture-confirmation") >= 0)
                {
                    StartMatch();
                    rules.StartPlay();
                    rules.TogglePause();
                    RequestConfirmation(ConfirmAction.RestartMatch);
                }
                if (System.Array.IndexOf(arguments, "--capture-quit-confirmation") >= 0)
                    RequestConfirmation(ConfirmAction.Quit);
                if (System.Array.IndexOf(arguments, "--capture-save-error") >= 0) saveFailed = true;
                if (System.Array.IndexOf(arguments, "--capture-special") >= 0) StartCoroutine(LaunchCaptureSpecial());
                if (System.Array.IndexOf(arguments, "--capture-rival-special") >= 0) StartCoroutine(LaunchCaptureRivalSpecial());
                if (System.Array.IndexOf(arguments, "--capture-goal") >= 0) StartCoroutine(LaunchCaptureGoal());
                Debug.Log("Spark Strikers visual capture requested: " + screenshotPath);
                StartCoroutine(CaptureScreenshotWhenReady());
            }
            else if (System.Array.IndexOf(arguments, "-smoke-test") >= 0)
            {
                StartRuntimeSmokeTest();
            }
        }

        void StartRuntimeSmokeTest()
        {
            if (!Mathf.Approximately(masterVolume, 0.8f) || !shakeEnabled || !flashEnabled || highContrast ||
                difficulty != GameDifficulty.Arcade || secretUnlocked || cupsWon != 0 || bestRound != 1)
                throw new System.InvalidOperationException("Runtime smoke automation defaults failed.");
            bool axisReady = true;
            if (MenuAxisStep(0.8f, ref axisReady) != 1 || MenuAxisStep(0.8f, ref axisReady) != 0 ||
                MenuAxisStep(0f, ref axisReady) != 0 || MenuAxisStep(-0.8f, ref axisReady) != -1)
                throw new System.InvalidOperationException("Runtime smoke menu axis edge detection failed.");
            void CheckSettingToggle(ref bool value, string key)
            {
                bool original = value;
                bool existed = PlayerPrefs.HasKey(key);
                int saved = PlayerPrefs.GetInt(key);
                try
                {
                    ToggleSetting(ref value, key);
                    if (value == original || PlayerPrefs.GetInt(key, -1) != (value ? 1 : 0))
                        throw new System.InvalidOperationException("Runtime smoke setting toggle failed: " + key);
                }
                finally
                {
                    value = original;
                    if (existed) PlayerPrefs.SetInt(key, saved);
                    else PlayerPrefs.DeleteKey(key);
                }
            }
            CheckSettingToggle(ref shakeEnabled, "screen_shake");
            CheckSettingToggle(ref flashEnabled, "flash_effects");
            CheckSettingToggle(ref highContrast, "high_contrast");
            if (font == null || !font.HasCharacter('日'))
                throw new System.InvalidOperationException("Runtime smoke Japanese font coverage failed.");
            if (string.IsNullOrWhiteSpace(Application.version))
                throw new System.InvalidOperationException("Runtime smoke build version was empty.");
            float[] ambienceSamples = new float[1024];
            if (ambienceSource == null || ambienceSource.clip == null || !ambienceSource.loop ||
                !ambienceSource.clip.GetData(ambienceSamples, 0) ||
                !System.Array.Exists(ambienceSamples, sample => Mathf.Abs(sample) > 0.001f))
                throw new System.InvalidOperationException("Runtime smoke stadium ambience failed.");
            bool originalLanguage = japanese;
            japanese = true;
            if (T("English", "日本語") != "日本語") throw new System.InvalidOperationException("Runtime smoke localization failed.");
            japanese = originalLanguage;
            cupsWon = 0;
            StartCup();
            if (cup.RoundCount != StandardRivalCount)
                throw new System.InvalidOperationException("Runtime smoke first cup rival count failed.");
            secretUnlocked = true;
            cupsWon = 2;
            bestRound = RivalNames.Length;
            SaveProgress();
            secretUnlocked = false;
            cupsWon = 0;
            bestRound = 1;
            LoadProgress();
            if (!secretUnlocked || cupsWon != 2 || bestRound != RivalNames.Length)
                throw new System.InvalidOperationException("Runtime smoke progress save round-trip failed.");
            File.Copy(progressPath, progressPath + ".bak", true);
            File.WriteAllText(progressPath, "{broken");
            secretUnlocked = false;
            cupsWon = 0;
            bestRound = 1;
            LoadProgress();
            if (!secretUnlocked || cupsWon != 2 || bestRound != RivalNames.Length)
                throw new System.InvalidOperationException("Runtime smoke progress backup recovery failed.");
            if (!TryLoadProgress(progressPath) || !TryLoadProgress(progressPath + ".bak"))
                throw new System.InvalidOperationException("Runtime smoke progress self-repair failed.");
            string validProgressPath = progressPath;
            string blockerPath = progressPath + ".blocked";
            File.WriteAllText(blockerPath, "block");
            progressPath = Path.Combine(blockerPath, "progress.sav");
            SaveProgress();
            if (!saveFailed) throw new System.InvalidOperationException("Runtime smoke save failure was not reported.");
            progressPath = validProgressPath;
            File.Delete(blockerPath);
            SaveProgress();
            if (saveFailed) throw new System.InvalidOperationException("Runtime smoke save recovery did not clear the warning.");
            StartCup();
            if (cup.RoundCount != RivalNames.Length)
                throw new System.InvalidOperationException("Runtime smoke hidden rival cup failed.");
            rules.StartPlay();
            shootWasHeld = true;
            shotCharge = 0.6f;
            OnApplicationFocus(false);
            if (rules.Phase != MatchPhase.Paused || shootWasHeld || shotCharge != 0f)
                throw new System.InvalidOperationException("Runtime smoke focus-loss pause failed.");
            OnApplicationFocus(true);
            if (rules.Phase != MatchPhase.Paused)
                throw new System.InvalidOperationException("Runtime smoke focus regain resumed without input.");
            ActivatePauseMenu(1);
            if (menuPage != MenuPage.Settings || menuIndex != 0 || rules.Phase != MatchPhase.Paused)
                throw new System.InvalidOperationException("Runtime smoke pause settings selection failed.");
            BackToMainMenu();
            ActivatePauseMenu(0);
            if (rules.Phase != MatchPhase.Playing)
                throw new System.InvalidOperationException("Runtime smoke pause resume selection failed.");
            ActivatePauseMenu(3);
            if (confirmIndex != 1 || confirmAction != ConfirmAction.ReturnToTitle)
                throw new System.InvalidOperationException("Runtime smoke confirmation default was not cancel.");
            CancelConfirmation();
            if (confirmAction != ConfirmAction.None || rules.Phase != MatchPhase.Playing)
                throw new System.InvalidOperationException("Runtime smoke confirmation cancel failed.");
            ActivatePauseMenu(3);
            confirmIndex = 0;
            ExecuteConfirmedAction();
            if (rules.Phase != MatchPhase.Title)
                throw new System.InvalidOperationException("Runtime smoke confirmed title return failed.");
            ArcadeCup completedCup = cup;
            cupOutcome = CupOutcome.Champion;
            ActivateResultsMenu(0);
            if (ReferenceEquals(cup, completedCup) || rules.Phase != MatchPhase.Kickoff)
                throw new System.InvalidOperationException("Runtime smoke champion menu selection failed.");
            rules.StartPlay();
            ActivatePauseMenu(2);
            if (confirmAction != ConfirmAction.RestartMatch)
                throw new System.InvalidOperationException("Runtime smoke pause restart selection failed.");
            confirmIndex = 0;
            ExecuteConfirmedAction();
            if (rules.Phase != MatchPhase.Kickoff)
                throw new System.InvalidOperationException("Runtime smoke confirmed restart failed.");
            ArcadeCup currentCup = cup;
            ActivateResultsMenu(1);
            if (rules.Phase != MatchPhase.Title)
                throw new System.InvalidOperationException("Runtime smoke results title selection failed.");
            ActivateResultsMenu(0);
            if (!ReferenceEquals(cup, currentCup) || rules.Phase != MatchPhase.Kickoff)
                throw new System.InvalidOperationException("Runtime smoke retry menu selection failed.");
            rules.StartPlay();
            Footballer tackler = home[0];
            Footballer victim = away[0];
            tackler.Position = Vector2.zero;
            victim.Position = Vector2.right * 0.5f;
            TakePossession(victim);
            LandRoughHit(tackler, victim);
            SyncVisuals();
            if (victim.DownTimer <= 0f || ball.Owner != null || ball.Velocity.sqrMagnitude <= 0f || rules.SpecialMeter <= 0f)
                throw new System.InvalidOperationException("Runtime smoke rough hit failed.");
            ResetPositions();
            Footballer previous = controlled;
            SwitchControlledPlayer();
            if (controlled == previous) throw new System.InvalidOperationException("Runtime smoke did not switch players.");
            controlled = home[1];
            TakePossession(controlled);
            rules.AddMeter(MatchRules.MaxMeter);
            if (!rules.SpendSpecial()) throw new System.InvalidOperationException("Runtime smoke could not charge special.");
            KickBall(controlled, Vector2.right, 0f, true);
            if (ball.SpecialKind != SpecialShotKind.Comet) throw new System.InvalidOperationException("Runtime smoke did not launch COMET.");
            RegisterGoal(true);
            if (celebrationTimer <= 0f || rules.Phase != MatchPhase.Kickoff)
                throw new System.InvalidOperationException("Runtime smoke did not enter goal celebration.");
            Invoke(nameof(FinishRuntimeSmokeTest), 2f);
        }

        IEnumerator LaunchCaptureSpecial()
        {
            secretUnlocked = true;
            flashEnabled = false;
            screenshotDelay = 0f;
            screenshotFrames = 18;
            yield return new WaitForEndOfFrame();
            StartMatch();
            rules.StartPlay();
            controlled = home[1];
            TakePossession(controlled);
            rules.AddMeter(MatchRules.MaxMeter);
            rules.SpendSpecial();
            KickBall(controlled, Vector2.right, 0f, true);
        }

        IEnumerator LaunchCaptureRivalSpecial()
        {
            flashEnabled = false;
            screenshotDelay = 0f;
            screenshotFrames = 18;
            yield return new WaitForEndOfFrame();
            StartMatch();
            rules.StartPlay();
            Footballer striker = away[1];
            TakePossession(striker);
            KickBall(striker, Vector2.left, 0f, true);
        }

        IEnumerator LaunchCaptureGoal()
        {
            flashEnabled = false;
            screenshotDelay = 0f;
            screenshotFrames = 8;
            yield return new WaitForEndOfFrame();
            StartCup();
            rules.StartPlay();
            ball.Position = new Vector2(FieldHalfWidth + 0.4f, 0f);
            RegisterGoal(true);
            SyncVisuals();
        }

        void FinishRuntimeSmokeTest()
        {
            if (celebrationTimer > 0f || rules.Phase != MatchPhase.Kickoff)
                throw new System.InvalidOperationException("Runtime smoke did not finish goal celebration.");
            DeleteAutomationProgress();
            Debug.Log("Spark Strikers runtime smoke test passed.");
            Application.Quit(0);
        }

        IEnumerator CaptureScreenshotWhenReady()
        {
            yield return new WaitForSecondsRealtime(screenshotDelay);
            for (int frame = 0; frame < screenshotFrames; frame++) yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(screenshotPath);
            Debug.Log("Spark Strikers screenshot captured: " + screenshotPath);
            yield return new WaitForSecondsRealtime(1.5f);
            DeleteAutomationProgress();
            Application.Quit(0);
        }

        void Update()
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed &&
                (Screen.width < MinimumWindowWidth || Screen.height < MinimumWindowHeight))
                Screen.SetResolution(Mathf.Max(Screen.width, MinimumWindowWidth), Mathf.Max(Screen.height, MinimumWindowHeight), FullScreenMode.Windowed);

            float unscaledDelta = Time.unscaledDeltaTime;
            ambienceSource.volume = masterVolume * (rules.Phase == MatchPhase.Title || rules.Phase == MatchPhase.Paused ? 0.08f : 0.16f);
            flash = Mathf.Max(0f, flash - unscaledDelta * 2.8f);
            bannerTimer = Mathf.Max(0f, bannerTimer - unscaledDelta);
            UpdateCamera(unscaledDelta);

            if (confirmAction != ConfirmAction.None)
            {
                UpdateConfirmationInput();
                return;
            }

            if (rules.Phase == MatchPhase.Title)
            {
                UpdateTitleInput();
                UpdateAttractMode(Time.time);
                return;
            }

            if (rules.Phase == MatchPhase.Paused)
            {
                UpdatePauseInput();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
            {
                rules.TogglePause();
                menuIndex = 0;
                return;
            }

            if (rules.Phase == MatchPhase.Kickoff)
            {
                if (celebrationTimer > 0f)
                {
                    celebrationTimer = Mathf.Max(0f, celebrationTimer - unscaledDelta);
                    if (celebrationTimer == 0f)
                    {
                        ResetPositions();
                        kickoffTimer = 3.15f;
                    }
                    return;
                }
                UpdateKickoff(unscaledDelta);
                return;
            }

            if (rules.Phase != MatchPhase.Playing)
            {
                if (rules.Phase == MatchPhase.Results) RecordCupResult();
                UpdateResultsInput();
                return;
            }

            float delta = Time.deltaTime;
            bool wasGoldenGoal = rules.IsGoldenGoal;
            rules.Tick(delta);
            if (!wasGoldenGoal && rules.IsGoldenGoal) AnnounceGoldenGoal();
            if (rules.Phase == MatchPhase.Results)
            {
                RecordCupResult();
                whistleSound.Play(audioSource, masterVolume);
                menuIndex = 0;
                return;
            }

            UpdateControlledPlayer(delta);
            UpdateComputerPlayers(delta);
            SeparatePlayers();
            UpdateBall(delta);
            SyncVisuals();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus || rules == null || rules.Phase != MatchPhase.Playing) return;
            rules.TogglePause();
            menuPage = MenuPage.Main;
            menuIndex = 0;
            shootWasHeld = false;
            shotCharge = 0f;
        }

        void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            float offsetX = (Screen.width - 1920f * scale) * 0.5f;
            float offsetY = (Screen.height - 1080f * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY), Quaternion.identity, new Vector3(scale, scale, 1f));
            bool confirmationOpen = confirmAction != ConfirmAction.None;
            bool previousEnabled = GUI.enabled;
            if (confirmationOpen) GUI.enabled = false;

            if (rules.Phase == MatchPhase.Title) DrawTitle();
            else DrawMatchHud();

            if (rules.Phase == MatchPhase.Paused)
            {
                if (menuPage == MenuPage.Settings) DrawPauseSettings();
                else DrawPause();
            }
            else if (rules.Phase == MatchPhase.Results) DrawResults();

            if (flashEnabled && flash > 0f)
                DrawRect(new Rect(0f, 0f, 1920f, 1080f), new Color(1f, 0.95f, 0.55f, flash * 0.38f));
            GUI.enabled = previousEnabled;
            if (confirmationOpen) DrawConfirmation();
            if (saveFailed) DrawSaveWarning();
        }

        void BuildAssets()
        {
            circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = "Generated Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                pixels[y * 64 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(33f - distance));
            }
            circleTexture.SetPixels(pixels);
            circleTexture.Apply();
            circleSprite = Sprite.Create(circleTexture, new Rect(0f, 0f, 64f, 64f), Vector2.one * 0.5f, 64f);

            squareTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "Generated Square",
                filterMode = FilterMode.Point
            };
            var squarePixels = new Color[16];
            for (int i = 0; i < squarePixels.Length; i++) squarePixels[i] = Color.white;
            squareTexture.SetPixels(squarePixels);
            squareTexture.Apply();
            squareSprite = Sprite.Create(squareTexture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f, 4f);

            ballSprite = circleSprite;
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            particleMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = circleTexture };
            font = Resources.Load<Font>("Fonts/NotoSansJP-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void BuildWorld()
        {
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Destroy(camera.gameObject);

            var cameraObject = new GameObject("Game Camera");
            cameraObject.transform.SetParent(transform);
            gameCamera = cameraObject.AddComponent<Camera>();
            gameCamera.orthographic = true;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.018f, 0.025f, 0.055f);
            gameCamera.transform.position = new Vector3(0f, 0f, -10f);

            worldRoot = new GameObject("Pitch").transform;
            worldRoot.SetParent(transform);

            CreateSprite("Stadium", squareSprite, Vector2.zero, new Vector2(19f, 11f), new Color(0.025f, 0.045f, 0.09f), -20);
            CreateSprite("Pitch", squareSprite, Vector2.zero, new Vector2(16.5f, 8.9f), new Color(0.045f, 0.34f, 0.21f), -10);
            for (int i = 0; i < 8; i++)
            {
                var stripe = i % 2 == 0 ? new Color(0.04f, 0.3f, 0.18f) : new Color(0.055f, 0.37f, 0.22f);
                CreateSprite("Grass Stripe", squareSprite, new Vector2(-7f + i * 2f, 0f), new Vector2(2f, 8.7f), stripe, -9);
            }

            Color line = new Color(0.85f, 1f, 0.9f, 0.78f);
            CreateLine("Touchline", new[]
            {
                new Vector2(-FieldHalfWidth, -FieldHalfHeight), new Vector2(FieldHalfWidth, -FieldHalfHeight),
                new Vector2(FieldHalfWidth, FieldHalfHeight), new Vector2(-FieldHalfWidth, FieldHalfHeight)
            }, line, 0.055f, true, -3);
            CreateLine("Halfway", new[] { new Vector2(0f, -FieldHalfHeight), new Vector2(0f, FieldHalfHeight) }, line, 0.045f, false, -3);
            CreateCircleLine("Center Circle", Vector2.zero, 1.15f, line, 0.045f, -3);
            CreateSprite("Center Mark", circleSprite, Vector2.zero, Vector2.one * 0.13f, line, -2);
            CreateBoxLines("Home Box", new Rect(-FieldHalfWidth, -2.25f, 2.1f, 4.5f), line);
            CreateBoxLines("Away Box", new Rect(FieldHalfWidth - 2.1f, -2.25f, 2.1f, 4.5f), line);
            CreateGoal(-1);
            CreateGoal(1);
            CreateCrowd();

            Vector2[] homeStarts = { new Vector2(-5.9f, 0f), new Vector2(-3.1f, -2.1f), new Vector2(-3.1f, 2.1f) };
            Vector2[] awayStarts = { new Vector2(5.9f, 0f), new Vector2(3.1f, 2.1f), new Vector2(3.1f, -2.1f) };
            for (int i = 0; i < 3; i++) home.Add(CreateFootballer(true, i, homeStarts[i]));
            for (int i = 0; i < 3; i++) away.Add(CreateFootballer(false, i, awayStarts[i]));
            everyone.AddRange(home);
            everyone.AddRange(away);

            var selection = CreateCircleLine("Selection", Vector2.zero, 0.48f, new Color(1f, 0.95f, 0.25f), 0.08f, 8);
            selection.useWorldSpace = false;
            selectionRing = selection.gameObject;

            var ballObject = CreateSprite("Ball", ballSprite, Vector2.zero, Vector2.one * 0.38f, Color.white, 10);
            ball = new BallState { GameObject = ballObject, Renderer = ballObject.GetComponent<SpriteRenderer>() };
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                var dot = CreateSprite("Ball Mark", circleSprite, Vector2.zero, Vector2.one * 0.07f, new Color(0.03f, 0.04f, 0.08f), 11, ballObject.transform);
                dot.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.27f, Mathf.Sin(angle) * 0.27f, 0f);
            }
        }

        void BuildAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.ignoreListenerPause = true;
            ambienceSource.clip = CreateCrowdLoop();
            ambienceSource.loop = true;
            ambienceSource.volume = masterVolume * 0.08f;
            ambienceSource.Play();
            kickSound = CreateTone("Kick", 105f, 0.08f, 0.35f, -65f);
            passSound = CreateTone("Pass", 330f, 0.07f, 0.2f, 140f);
            whistleSound = CreateTone("Whistle", 1250f, 0.18f, 0.2f, 180f);
            goalSound = CreateTone("Goal", 440f, 0.65f, 0.28f, 600f);
            specialSound = CreateTone("Special", 180f, 0.75f, 0.32f, 1100f);
        }

        AudioClip CreateTone(string clipName, float startHz, float duration, float volume, float sweep)
        {
            const int sampleRate = 44100;
            int count = Mathf.CeilToInt(duration * sampleRate);
            var samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)count;
                float frequency = Mathf.Max(30f, startHz + sweep * progress);
                phase += frequency * Mathf.PI * 2f / sampleRate;
                float envelope = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.35f);
                samples[i] = Mathf.Sin(phase) * envelope * volume;
            }

            var clip = AudioClip.Create(clipName, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateCrowdLoop()
        {
            const int sampleRate = 22050;
            const int seconds = 4;
            float[] frequencies = { 73f, 97f, 131f, 173f, 229f, 307f, 401f };
            var samples = new float[sampleRate * seconds];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)sampleRate;
                float murmur = 0f;
                for (int tone = 0; tone < frequencies.Length; tone++)
                    murmur += Mathf.Sin(time * frequencies[tone] * Mathf.PI * 2f + tone * 1.37f);
                samples[i] = murmur / frequencies.Length * (0.5f + Mathf.Sin(time * Mathf.PI) * 0.08f);
            }

            var clip = AudioClip.Create("Stadium Crowd", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        Footballer CreateFootballer(bool isHome, int index, Vector2 start)
        {
            var bodyObject = CreateSprite(isHome ? "Spark Player" : "Rival Player", circleSprite, start, Vector2.one * 0.72f,
                isHome ? homeColor : awayColor, 4);
            var markObject = new GameObject("Team Mark");
            markObject.transform.SetParent(bodyObject.transform, false);
            var mark = markObject.AddComponent<TextMesh>();
            mark.text = isHome ? "S" : RivalMarks[activeRound];
            mark.font = font;
            mark.fontSize = 48;
            mark.characterSize = 0.08f;
            mark.anchor = TextAnchor.MiddleCenter;
            mark.alignment = TextAlignment.Center;
            mark.color = Color.white;
            markObject.GetComponent<MeshRenderer>().sortingOrder = 6;

            var player = new Footballer
            {
                GameObject = bodyObject,
                Body = bodyObject.GetComponent<SpriteRenderer>(),
                Mark = mark,
                Position = start,
                StartPosition = start,
                Facing = isHome ? Vector2.right : Vector2.left,
                Home = isHome,
                Index = index
            };
            return player;
        }

        GameObject CreateSprite(string objectName, Sprite sprite, Vector2 position, Vector2 scale, Color color, int order, Transform parent = null)
        {
            var instance = new GameObject(objectName);
            instance.transform.SetParent(parent == null ? worldRoot : parent, false);
            instance.transform.position = new Vector3(position.x, position.y, 0f);
            instance.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return instance;
        }

        LineRenderer CreateLine(string objectName, Vector2[] points, Color color, float width, bool loop, int order)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(worldRoot);
            var line = lineObject.AddComponent<LineRenderer>();
            line.material = lineMaterial;
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = points.Length;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = order;
            for (int i = 0; i < points.Length; i++) line.SetPosition(i, points[i]);
            return line;
        }

        LineRenderer CreateCircleLine(string objectName, Vector2 center, float radius, Color color, float width, int order)
        {
            const int segments = 48;
            var points = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return CreateLine(objectName, points, color, width, true, order);
        }

        void CreateBoxLines(string objectName, Rect rect, Color color)
        {
            CreateLine(objectName, new[]
            {
                new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax)
            }, color, 0.045f, true, -3);
        }

        void CreateGoal(int side)
        {
            float lineX = side * FieldHalfWidth;
            float backX = side * (FieldHalfWidth + 0.72f);
            Color net = new Color(0.75f, 0.9f, 1f, 0.75f);
            CreateLine("Goal Frame", new[]
            {
                new Vector2(lineX, -GoalHalfHeight), new Vector2(backX, -GoalHalfHeight),
                new Vector2(backX, GoalHalfHeight), new Vector2(lineX, GoalHalfHeight)
            }, Color.white, 0.09f, false, 1);
            for (int i = -2; i <= 2; i++)
            {
                float y = i * GoalHalfHeight / 2f;
                CreateLine("Goal Net", new[] { new Vector2(lineX, y), new Vector2(backX, y) }, net, 0.025f, false, 0);
            }
        }

        void CreateCrowd()
        {
            Color[] crowd =
            {
                new Color(0.2f, 0.8f, 1f), new Color(1f, 0.3f, 0.2f),
                new Color(1f, 0.9f, 0.2f), new Color(0.75f, 0.35f, 1f)
            };
            for (int i = 0; i < 36; i++)
            {
                float x = -8.7f + i * 0.5f;
                CreateSprite("Crowd", circleSprite, new Vector2(x, 4.85f + (i % 3) * 0.13f), Vector2.one * 0.16f, crowd[i % crowd.Length], -5);
                CreateSprite("Crowd", circleSprite, new Vector2(-x, -4.85f - (i % 3) * 0.13f), Vector2.one * 0.16f, crowd[(i + 2) % crowd.Length], -5);
            }
        }

        void StartCup()
        {
            cup = new ArcadeCup(availableRivalCount);
            StartMatch();
        }

        void StartMatch()
        {
            activeRound = cup.RoundIndex;
            resultRecorded = false;
            cupOutcome = CupOutcome.Retry;
            ApplyOpponentIdentity();
            rules.StartMatch();
            rivalMeter = 0f;
            shotCharge = 0f;
            shootWasHeld = false;
            manualControlLock = 0f;
            kickoffTimer = 3.1f;
            banner = T("KICK OFF", "キックオフ");
            bannerTimer = 1.2f;
            menuPage = MenuPage.Main;
            ResetPositions();
            whistleSound.Play(audioSource, masterVolume);
        }

        void ApplyOpponentIdentity()
        {
            foreach (var player in away)
            {
                player.Mark.text = RivalMarks[activeRound];
                player.GameObject.name = opponentName + " Player";
            }
        }

        void LoadProgress()
        {
            if (TryLoadProgress(progressPath)) return;
            string backupPath = progressPath + ".bak";
            if (TryLoadProgress(backupPath))
            {
                RepairProgressFromBackup(backupPath);
                return;
            }

            secretUnlocked = PlayerPrefs.GetInt("comet_ball", 0) == 1;
            cupsWon = Mathf.Max(0, PlayerPrefs.GetInt("cups_won", 0));
            bestRound = Mathf.Clamp(PlayerPrefs.GetInt("best_round", 1), 1, RivalNames.Length);
            SaveProgress();
        }

        bool TryLoadProgress(string path)
        {
            if (!File.Exists(path)) return false;
            try
            {
                string[] lines = File.ReadAllLines(path);
                if (lines.Length != 4 || lines[0] != "version=1") throw new InvalidDataException("Unsupported progress format.");
                int cometBall = ParseProgressValue(lines[1], "comet");
                if (cometBall != 0 && cometBall != 1) throw new InvalidDataException("Invalid comet value.");
                secretUnlocked = cometBall == 1;
                cupsWon = Mathf.Max(0, ParseProgressValue(lines[2], "cups"));
                bestRound = Mathf.Clamp(ParseProgressValue(lines[3], "best"), 1, RivalNames.Length);
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Could not load progress file " + path + ": " + exception.Message);
                return false;
            }
        }

        void RepairProgressFromBackup(string backupPath)
        {
            try
            {
                string temporaryPath = progressPath + ".tmp";
                File.Copy(backupPath, temporaryPath, true);
                CommitProgressFile(temporaryPath, null);
                Debug.Log("Recovered primary progress file from backup.");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Could not repair primary progress file: " + exception.Message);
            }
        }

        static int ParseProgressValue(string line, string key)
        {
            string prefix = key + "=";
            if (!line.StartsWith(prefix) || !int.TryParse(line.Substring(prefix.Length), out int value))
                throw new InvalidDataException("Invalid progress field: " + key);
            return value;
        }

        void SaveProgress()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(progressPath));
                string temporaryPath = progressPath + ".tmp";
                File.WriteAllLines(temporaryPath, new[]
                {
                    "version=1",
                    "comet=" + (secretUnlocked ? 1 : 0),
                    "cups=" + cupsWon,
                    "best=" + bestRound
                });
                CommitProgressFile(temporaryPath, progressPath + ".bak");
                saveFailed = false;
            }
            catch (System.Exception exception)
            {
                saveFailed = true;
                Debug.LogError("Could not save progress: " + exception.Message);
            }
        }

        void CommitProgressFile(string temporaryPath, string backupPath)
        {
            if (File.Exists(progressPath)) File.Replace(temporaryPath, progressPath, backupPath);
            else File.Move(temporaryPath, progressPath);
        }

        void DeleteAutomationProgress()
        {
            if (!automationRun) return;
            foreach (string path in new[] { progressPath, progressPath + ".bak", progressPath + ".tmp" })
                if (File.Exists(path)) File.Delete(path);
        }

        void RecordCupResult()
        {
            if (resultRecorded) return;
            resultRecorded = true;
            cupOutcome = cup.RecordResult(rules.HomeScore, rules.AwayScore);
            bestRound = Mathf.Max(bestRound, cupOutcome == CupOutcome.Champion ? cup.RoundCount : cup.RoundIndex + 1);
            if (cupOutcome == CupOutcome.Champion) cupsWon++;
            SaveProgress();
        }

        void ResetPositions()
        {
            celebrationTimer = 0f;
            celebrationSide = 0f;
            foreach (var player in everyone)
            {
                player.Position = player.StartPosition;
                player.Velocity = Vector2.zero;
                player.Facing = player.Home ? Vector2.right : Vector2.left;
                player.DecisionTimer = UnityEngine.Random.Range(0.2f, 0.8f);
                player.StealCooldown = 0f;
                player.SlowTimer = 0f;
                player.DownTimer = 0f;
            }

            if (ball != null)
            {
                ball.Position = Vector2.zero;
                ball.Velocity = Vector2.zero;
                ball.Owner = null;
                ball.LastTouch = null;
                ball.PickupLock = 0.65f;
                ball.SpecialKind = SpecialShotKind.None;
                ball.SpecialAge = 0f;
                ball.Renderer.color = Color.white;
            }
            controlled = home.Count > 1 ? home[1] : null;
            SyncVisuals();
        }

        void UpdateKickoff(float delta)
        {
            kickoffTimer -= delta;
            if (kickoffTimer > 2.2f) banner = "3";
            else if (kickoffTimer > 1.4f) banner = "2";
            else if (kickoffTimer > 0.6f) banner = "1";
            else banner = "GO!";
            bannerTimer = 0.2f;
            if (kickoffTimer > 0f) return;

            rules.StartPlay();
            banner = "GO!";
            bannerTimer = 0.65f;
            ball.PickupLock = 0f;
            whistleSound.Play(audioSource, masterVolume * 0.7f);
        }

        void AnnounceGoldenGoal()
        {
            banner = T("GOLDEN GOAL!", "ゴールデンゴール！");
            bannerTimer = 2.4f;
            flash = flashEnabled ? 0.65f : 0f;
            cameraShake = 0.45f;
            whistleSound.Play(audioSource, masterVolume);
        }

        void UpdateControlledPlayer(float delta)
        {
            manualControlLock = Mathf.Max(0f, manualControlLock - delta);
            bool switchPressed = Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton4);
            if (switchPressed && (ball.Owner == null || !ball.Owner.Home) && home.Count > 1)
            {
                SwitchControlledPlayer();
                passSound.Play(audioSource, masterVolume * 0.45f);
            }

            selectTimer -= delta;
            if (selectTimer <= 0f)
            {
                selectTimer = 0.12f;
                if (ball.Owner != null && ball.Owner.Home) controlled = ball.Owner;
                else if (manualControlLock <= 0f) controlled = ClosestPlayer(home, ball.Position);
            }

            if (controlled == null) return;
            Vector2 input = ReadMoveInput();
            bool dash = DashHeld();
            MovePlayer(controlled, input, dash ? 6.2f : 4.7f, delta);

            bool shootHeld = Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.JoystickButton0);
            bool ownsBall = ball.Owner == controlled;
            if (ownsBall && shootHeld) shotCharge = Mathf.Min(1f, shotCharge + delta * 1.25f);
            if (ownsBall && shootWasHeld && !shootHeld)
            {
                Vector2 direction = AimDirection(controlled, input);
                KickBall(controlled, direction, Mathf.Lerp(8.5f, 14f, shotCharge), false);
                shotCharge = 0f;
            }
            if (!ownsBall && !shootHeld) shotCharge = 0f;
            shootWasHeld = shootHeld;

            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2))
            {
                if (ownsBall) PassBall(controlled);
                else TryRoughHit(controlled);
            }

            if (ownsBall && (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.JoystickButton3)) && rules.SpendSpecial())
                KickBall(controlled, AimDirection(controlled, input), 16.5f, true);
        }

        void SwitchControlledPlayer()
        {
            int currentIndex = Mathf.Max(0, home.IndexOf(controlled));
            for (int step = 1; step <= home.Count; step++)
            {
                Footballer candidate = home[(currentIndex + step) % home.Count];
                if (candidate.DownTimer > 0f) continue;
                controlled = candidate;
                break;
            }
            manualControlLock = 1.2f;
            selectTimer = 0.12f;
        }

        Vector2 ReadMoveInput()
        {
            Vector2 keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2 gamepad = new Vector2(Input.GetAxisRaw("PadHorizontal"), Input.GetAxisRaw("PadVertical"));
            Vector2 input = gamepad.sqrMagnitude > keyboard.sqrMagnitude ? gamepad : keyboard;
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        bool DashHeld() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.JoystickButton1);

        Vector2 AimDirection(Footballer player, Vector2 input)
        {
            float attack = player.Home ? 1f : -1f;
            var direction = new Vector2(attack * Mathf.Max(0.65f, 0.85f + input.x * attack * 0.25f), input.y * 0.72f);
            return direction.normalized;
        }

        void MovePlayer(Footballer player, Vector2 direction, float speed, float delta)
        {
            if (player.DownTimer > 0f) direction = Vector2.zero;
            if (direction.sqrMagnitude > 0.02f) player.Facing = direction.normalized;
            if (player.SlowTimer > 0f) speed *= 0.55f;
            Vector2 targetVelocity = direction * speed;
            player.Velocity = Vector2.MoveTowards(player.Velocity, targetVelocity, 18f * delta);
            player.Position += player.Velocity * delta;
            player.Position = new Vector2(
                Mathf.Clamp(player.Position.x, -FieldHalfWidth + 0.25f, FieldHalfWidth - 0.25f),
                Mathf.Clamp(player.Position.y, -FieldHalfHeight + 0.25f, FieldHalfHeight - 0.25f));
        }

        void UpdateComputerPlayers(float delta)
        {
            Footballer homeChaser = ClosestPlayer(home, ball.Position, controlled);
            Footballer awayChaser = ClosestPlayer(away, ball.Position);
            foreach (var player in everyone)
            {
                player.DecisionTimer -= delta;
                player.StealCooldown = Mathf.Max(0f, player.StealCooldown - delta);
                player.SlowTimer = Mathf.Max(0f, player.SlowTimer - delta);
                player.DownTimer = Mathf.Max(0f, player.DownTimer - delta);
                if (player == controlled) continue;
                if (player.DownTimer > 0f)
                {
                    MovePlayer(player, Vector2.zero, 0f, delta);
                    continue;
                }

                bool chaser = player == (player.Home ? homeChaser : awayChaser);
                Vector2 target;
                float speed = 4.15f;
                if (ball.Owner == player)
                {
                    float attack = player.Home ? 1f : -1f;
                    target = new Vector2(attack * 7.2f, Mathf.Clamp(player.Position.y * 0.45f, -1.8f, 1.8f));
                    if (player.DecisionTimer <= 0f)
                    {
                        float decisionSpeed = player.Home ? 1f : (1f + activeRound * 0.14f) * DifficultyRules.Decisions(difficulty);
                        player.DecisionTimer = UnityEngine.Random.Range(0.55f, 1.05f) / decisionSpeed;
                        float goalDistance = Mathf.Abs(attack * FieldHalfWidth - player.Position.x);
                        if (!player.Home && rivalMeter >= MatchRules.MaxMeter)
                        {
                            rivalMeter = 0f;
                            KickBall(player, AimDirection(player, Vector2.up * UnityEngine.Random.Range(-0.5f, 0.5f)), 15.5f + activeRound * 0.45f, true);
                        }
                        else if (goalDistance < 5.4f || UnityEngine.Random.value < 0.32f)
                        {
                            KickBall(player, AimDirection(player, Vector2.up * UnityEngine.Random.Range(-0.8f, 0.8f)), UnityEngine.Random.Range(9.5f, 12.5f), false);
                        }
                        else if (UnityEngine.Random.value < 0.35f)
                        {
                            PassBall(player);
                        }
                    }
                }
                else if (chaser || (ball.Owner != null && ball.Owner.Home != player.Home && Vector2.Distance(player.Position, ball.Owner.Position) < 2.2f))
                {
                    target = ball.Owner == null ? ball.Position : ball.Owner.Position;
                    speed = 4.55f;
                }
                else
                {
                    float ballInfluenceX = Mathf.Clamp(ball.Position.x * 0.2f, -1.35f, 1.35f);
                    float ballInfluenceY = Mathf.Clamp(ball.Position.y * 0.18f, -0.75f, 0.75f);
                    target = player.StartPosition + new Vector2(ballInfluenceX, ballInfluenceY);
                }

                if (!player.Home) speed *= (1f + activeRound * 0.1f) * DifficultyRules.Speed(difficulty);
                Vector2 direction = target - player.Position;
                MovePlayer(player, direction.sqrMagnitude > 0.04f ? direction.normalized : Vector2.zero, speed, delta);
            }

            if (ball.Owner != null)
            {
                if (ball.Owner.Home) rules.AddMeter(delta * 5f);
                else rivalMeter = Mathf.Min(MatchRules.MaxMeter, rivalMeter + delta * (6.5f + activeRound * 1.5f) * DifficultyRules.SpecialCharge(difficulty));
            }
        }

        Footballer ClosestPlayer(List<Footballer> team, Vector2 target, Footballer exclude = null)
        {
            Footballer closest = null;
            float closestDistance = float.MaxValue;
            foreach (var player in team)
            {
                if (player == exclude || player.DownTimer > 0f) continue;
                float distance = (player.Position - target).sqrMagnitude;
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                closest = player;
            }
            return closest;
        }

        void SeparatePlayers()
        {
            for (int i = 0; i < everyone.Count; i++)
            for (int j = i + 1; j < everyone.Count; j++)
            {
                Vector2 offset = everyone[j].Position - everyone[i].Position;
                float distance = offset.magnitude;
                if (distance >= 0.64f || distance <= 0.001f) continue;
                Vector2 push = offset / distance * (0.64f - distance) * 0.5f;
                everyone[i].Position -= push;
                everyone[j].Position += push;
            }
        }

        void UpdateBall(float delta)
        {
            ball.PickupLock = Mathf.Max(0f, ball.PickupLock - delta);
            if (ball.Owner != null)
            {
                ball.Position = ball.Owner.Position + ball.Owner.Facing * 0.48f;
                ball.Velocity = ball.Owner.Velocity;
                TryTackleBallCarrier(ball.Owner);
            }
            else
            {
                UpdateSpecialShot(delta);
                ball.Position += ball.Velocity * delta;
                ball.Velocity = Vector2.MoveTowards(ball.Velocity, Vector2.zero, SpecialShotRules.Drag(ball.SpecialKind) * delta);
                ResolveBallBounds();
                ResolvePlayerContacts();
            }

            if (ball.IsSpecial)
            {
                ball.TrailTimer -= delta;
                ball.Renderer.color = SpecialColor(ball.SpecialKind);
                if (ball.TrailTimer <= 0f)
                {
                    ball.TrailTimer = 0.035f;
                    SpawnBurst(ball.Position, ball.Renderer.color, 2, 0.12f);
                }
                if (ball.Velocity.magnitude < 6f)
                {
                    ClearSpecialShot();
                }
            }
        }

        void UpdateSpecialShot(float delta)
        {
            if (!ball.IsSpecial) return;
            ball.SpecialAge += delta;
            float speed = ball.Velocity.magnitude;
            if (speed <= 0.01f) return;

            if (ball.SpecialKind == SpecialShotKind.Blaze)
            {
                ball.Velocity = ball.Velocity.normalized * Mathf.MoveTowards(speed, 19f, delta * 4f);
            }
            else if (ball.SpecialKind == SpecialShotKind.Comet)
            {
                float attack = ball.LastTouch != null && ball.LastTouch.Home ? 1f : -1f;
                Vector2 target = new Vector2(attack * (FieldHalfWidth + 0.4f), 0f) - ball.Position;
                ball.Velocity = Vector2.Lerp(ball.Velocity.normalized, target.normalized, delta * 1.8f).normalized * speed;
            }
            else if (ball.SpecialKind == SpecialShotKind.Eclipse)
            {
                float turn = (ball.SpecialAge < 0.42f ? ball.CurveDirection : -ball.CurveDirection) * 70f * delta;
                ball.Velocity = Rotate(ball.Velocity, turn);
            }
            else if (ball.SpecialKind == SpecialShotKind.Nova)
            {
                ball.Velocity = Rotate(ball.Velocity.normalized * Mathf.MoveTowards(speed, 20f, delta * 3f),
                    Mathf.Sin(ball.SpecialAge * 18f) * 55f * delta);
            }
        }

        Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        Color SpecialColor(SpecialShotKind kind)
        {
            switch (kind)
            {
                case SpecialShotKind.Comet: return Color.HSVToRGB(Mathf.Repeat(Time.time * 0.8f, 1f), 0.85f, 1f);
                case SpecialShotKind.Blaze: return new Color(1f, 0.25f, 0.05f);
                case SpecialShotKind.Frost: return new Color(0.35f, 0.95f, 1f);
                case SpecialShotKind.Eclipse: return new Color(1f, 0.18f, 0.7f);
                case SpecialShotKind.Nova: return Color.Lerp(new Color(1f, 0.25f, 0.7f), new Color(1f, 0.9f, 0.1f), Mathf.PingPong(Time.time * 4f, 1f));
                default: return new Color(1f, 0.85f, 0.12f);
            }
        }

        void ResolveBallBounds()
        {
            if (ball.Position.x > FieldHalfWidth + 0.35f && Mathf.Abs(ball.Position.y) < GoalHalfHeight)
            {
                RegisterGoal(true);
                return;
            }
            if (ball.Position.x < -FieldHalfWidth - 0.35f && Mathf.Abs(ball.Position.y) < GoalHalfHeight)
            {
                RegisterGoal(false);
                return;
            }

            if (Mathf.Abs(ball.Position.y) > FieldHalfHeight)
            {
                ball.Position.y = Mathf.Sign(ball.Position.y) * FieldHalfHeight;
                ball.Velocity.y = -ball.Velocity.y * 0.82f;
                kickSound.Play(audioSource, masterVolume * 0.35f);
            }
            bool insideGoalMouth = Mathf.Abs(ball.Position.y) < GoalHalfHeight;
            if (Mathf.Abs(ball.Position.x) > FieldHalfWidth && !insideGoalMouth)
            {
                ball.Position.x = Mathf.Sign(ball.Position.x) * FieldHalfWidth;
                ball.Velocity.x = -ball.Velocity.x * 0.82f;
                kickSound.Play(audioSource, masterVolume * 0.35f);
            }
        }

        void ResolvePlayerContacts()
        {
            foreach (var player in everyone)
            {
                Vector2 offset = ball.Position - player.Position;
                if (offset.sqrMagnitude > 0.25f) continue;

                if (ball.IsSpecial && ball.LastTouch != null && ball.LastTouch.Home != player.Home)
                {
                    player.Position -= ball.Velocity.normalized * SpecialShotRules.Knockback(ball.SpecialKind);
                    player.DownTimer = Mathf.Max(player.DownTimer, 0.55f);
                    if (SpecialShotRules.SlowsPlayers(ball.SpecialKind)) player.SlowTimer = 1.8f;
                    cameraShake = Mathf.Max(cameraShake, 0.12f);
                    SpawnBurst(player.Position, SpecialColor(ball.SpecialKind), 6, 0.2f);
                    continue;
                }

                if (player.DownTimer <= 0f && ball.PickupLock <= 0f && ball.Velocity.magnitude < 10f)
                {
                    TakePossession(player);
                    return;
                }

                Vector2 normal = offset.sqrMagnitude > 0.001f ? offset.normalized : player.Facing;
                ball.Velocity = Vector2.Reflect(ball.Velocity, normal) * 0.72f;
                ball.Position = player.Position + normal * 0.52f;
            }
        }

        void TryRoughHit(Footballer attacker)
        {
            if (attacker.DownTimer > 0f || attacker.StealCooldown > 0f) return;

            attacker.StealCooldown = 0.42f;
            Footballer victim = null;
            float closestDistance = 1f;
            foreach (Footballer candidate in attacker.Home ? away : home)
            {
                Vector2 offset = candidate.Position - attacker.Position;
                float distance = offset.sqrMagnitude;
                if (candidate.DownTimer > 0f || distance > closestDistance ||
                    (distance > 0.001f && Vector2.Dot(attacker.Facing, offset.normalized) < 0.1f)) continue;
                victim = candidate;
                closestDistance = distance;
            }

            if (victim == null)
            {
                SpawnBurst(attacker.Position + attacker.Facing * 0.48f, Color.white, 3, 0.1f);
                kickSound.Play(audioSource, masterVolume * 0.35f);
                return;
            }

            LandRoughHit(attacker, victim);
        }

        void TryTackleBallCarrier(Footballer owner)
        {
            foreach (var challenger in everyone)
            {
                if (challenger.Home == owner.Home || challenger.DownTimer > 0f || challenger.StealCooldown > 0f) continue;
                if ((challenger.Position - owner.Position).sqrMagnitude > 0.42f) continue;

                bool humanTackle = challenger == controlled && DashHeld();
                if (!humanTackle && challenger.Velocity.sqrMagnitude < 9f) continue;

                LandRoughHit(challenger, owner);
                return;
            }
        }

        void LandRoughHit(Footballer attacker, Footballer victim)
        {
            Vector2 direction = victim.Position - attacker.Position;
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : attacker.Facing;
            attacker.StealCooldown = Mathf.Max(attacker.StealCooldown, 0.48f);
            victim.StealCooldown = Mathf.Max(victim.StealCooldown, 1.05f);
            victim.DownTimer = Mathf.Max(victim.DownTimer, 0.9f);
            victim.SlowTimer = 0f;
            victim.Velocity = direction * 6.2f;

            if (ball.Owner == victim)
            {
                ball.Owner = null;
                ball.LastTouch = victim;
                ball.Position = victim.Position + direction * 0.35f;
                ball.Velocity = direction * 6.8f + Vector2.up * UnityEngine.Random.Range(-1.4f, 1.4f);
                ball.PickupLock = 0.28f;
                ClearSpecialShot();
            }

            if (attacker.Home) rules.AddMeter(10f);
            else rivalMeter = Mathf.Min(MatchRules.MaxMeter, rivalMeter + 10f * DifficultyRules.SpecialCharge(difficulty));
            banner = attacker.Home ? T("ROUGH HIT!", "ラフヒット！") : T("RIVAL ROUGH HIT!", "相手のラフプレー！");
            bannerTimer = 0.65f;
            cameraShake = Mathf.Max(cameraShake, 0.28f);
            SpawnBurst(victim.Position, Color.white, 10, 0.24f);
            kickSound.Play(audioSource, masterVolume * 0.8f);
        }

        void TakePossession(Footballer player)
        {
            if (player.DownTimer > 0f) return;
            ball.Owner = player;
            ball.LastTouch = player;
            ball.Velocity = Vector2.zero;
            ClearSpecialShot();
            if (player.Home) rules.AddMeter(1.5f);
        }

        void ClearSpecialShot()
        {
            ball.SpecialKind = SpecialShotKind.None;
            ball.Renderer.color = Color.white;
        }

        void PassBall(Footballer player)
        {
            List<Footballer> team = player.Home ? home : away;
            Footballer best = null;
            float bestScore = float.MinValue;
            float attack = player.Home ? 1f : -1f;
            foreach (var candidate in team)
            {
                if (candidate == player || candidate.DownTimer > 0f) continue;
                float score = candidate.Position.x * attack - Vector2.Distance(player.Position, candidate.Position) * 0.18f;
                if (score <= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            Vector2 direction = best == null ? player.Facing : (best.Position - player.Position).normalized;
            KickBall(player, direction, 8.2f, false);
            if (player.Home) rules.AddMeter(5f);
            passSound.Play(audioSource, masterVolume);
        }

        void KickBall(Footballer player, Vector2 direction, float speed, bool isSpecial)
        {
            if (ball.Owner != player) return;
            SpecialShotKind specialKind = isSpecial ? SpecialKindFor(player) : SpecialShotKind.None;
            ball.Owner = null;
            ball.LastTouch = player;
            ball.Position = player.Position + direction * 0.56f;
            ball.Velocity = direction.normalized * (isSpecial ? SpecialShotRules.LaunchSpeed(specialKind) : speed);
            ball.PickupLock = isSpecial ? 0.75f : 0.18f;
            ball.SpecialKind = specialKind;
            ball.SpecialAge = 0f;
            ball.CurveDirection = player.Position.y >= 0f ? -1f : 1f;
            ball.TrailTimer = 0f;
            shotCharge = 0f;
            if (player.Home && !isSpecial) rules.AddMeter(7f);
            if (isSpecial)
            {
                banner = player.Home ? (secretUnlocked ? "COMET BREAKER!" : "STAR BREAKER!") : RivalSpecials[activeRound];
                bannerTimer = 1.25f;
                flash = flashEnabled ? 1f : 0f;
                cameraShake = 0.65f;
                SpawnBurst(ball.Position, SpecialColor(specialKind), 8, 0.2f);
                specialSound.Play(audioSource, masterVolume);
            }
            else
            {
                cameraShake = Mathf.Max(cameraShake, 0.08f + speed * 0.004f);
                kickSound.Play(audioSource, masterVolume);
            }
        }

        SpecialShotKind SpecialKindFor(Footballer player)
        {
            if (player.Home) return secretUnlocked ? SpecialShotKind.Comet : SpecialShotKind.Star;
            if (activeRound == 0) return SpecialShotKind.Blaze;
            if (activeRound == 1) return SpecialShotKind.Frost;
            return activeRound == 2 ? SpecialShotKind.Eclipse : SpecialShotKind.Nova;
        }

        void RegisterGoal(bool homeScored)
        {
            bool decidingGoal = rules.IsGoldenGoal;
            rules.ScoreGoal(homeScored);
            banner = homeScored ? T("GOOOAL!", "ゴーーール！") : T("RIVAL GOAL", "相手ゴール");
            bannerTimer = 2f;
            flash = flashEnabled ? 0.8f : 0f;
            cameraShake = 0.85f;
            SpawnBurst(ball.Position, homeScored ? homeColor : awayColor, 42, 0.75f);
            goalSound.Play(audioSource, masterVolume);
            if (decidingGoal)
            {
                menuIndex = 0;
                return;
            }
            ball.Velocity = Vector2.zero;
            celebrationTimer = 1.35f;
            celebrationSide = homeScored ? 1f : -1f;
        }

        void SpawnBurst(Vector2 position, Color color, int count, float size)
        {
            var burstObject = new GameObject("Spark Burst");
            burstObject.transform.SetParent(worldRoot);
            burstObject.transform.position = position;
            var particles = burstObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 4.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.35f, size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;
            var renderer = burstObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = particleMaterial;
            renderer.sortingOrder = 12;
            particles.Emit(count);
            particles.Play();
        }

        void SyncVisuals()
        {
            foreach (var player in everyone)
            {
                player.GameObject.transform.position = new Vector3(player.Position.x, player.Position.y, 0f);
                Color teamColor = player.Home ? homeColor : awayColor;
                bool down = player.DownTimer > 0f;
                player.GameObject.transform.localScale = down ? new Vector3(0.82f, 0.34f, 1f) : Vector3.one * 0.72f;
                player.Mark.text = down ? "×" : player.Home ? "S" : RivalMarks[activeRound];
                player.Body.color = down ? Color.Lerp(teamColor, Color.black, 0.45f) :
                    player.SlowTimer > 0f ? Color.Lerp(teamColor, new Color(0.5f, 1f, 1f), 0.55f) : teamColor;
            }
            ball.GameObject.transform.position = new Vector3(ball.Position.x, ball.Position.y, 0f);
            float pulse = ball.IsSpecial ? 1f + Mathf.Sin(Time.time * 24f) * 0.13f : 1f;
            ball.GameObject.transform.localScale = Vector3.one * (0.38f * pulse);
            ball.GameObject.transform.Rotate(0f, 0f, ball.Velocity.magnitude * Time.deltaTime * -80f);
            selectionRing.SetActive(controlled != null && rules.Phase != MatchPhase.Title);
            if (controlled != null)
                selectionRing.transform.position = new Vector3(controlled.Position.x, controlled.Position.y, 0f);
        }

        void UpdateCamera(float delta)
        {
            if (gameCamera == null) return;
            float aspect = Mathf.Max(0.5f, Screen.width / (float)Mathf.Max(1, Screen.height));
            float celebrationFocus = Mathf.Clamp01(celebrationTimer / 0.35f);
            gameCamera.orthographicSize = Mathf.Max(5.65f, 9.45f / aspect) - celebrationFocus * 0.2f;
            cameraShake = Mathf.Max(0f, cameraShake - delta * 1.9f);
            Vector2 shake = shakeEnabled && cameraShake > 0f ? UnityEngine.Random.insideUnitCircle * cameraShake * 0.18f : Vector2.zero;
            gameCamera.transform.position = new Vector3(celebrationSide * celebrationFocus * 0.65f + shake.x, shake.y, -10f);
        }

        void UpdateAttractMode(float time)
        {
            if (home.Count == 0) return;
            for (int i = 0; i < everyone.Count; i++)
            {
                var player = everyone[i];
                player.Position = player.StartPosition + new Vector2(Mathf.Sin(time * 0.8f + i) * 0.35f, Mathf.Cos(time * 1.05f + i) * 0.22f);
            }
            ball.Position = new Vector2(Mathf.Sin(time * 0.7f) * 1.6f, Mathf.Cos(time * 1.1f) * 0.7f);
            ball.GameObject.transform.Rotate(0f, 0f, -55f * Time.deltaTime);
            SyncVisuals();
        }

        void UpdateTitleInput()
        {
            foreach (KeyCode key in SecretCode)
            {
                if (!Input.GetKeyDown(key)) continue;
                if (key == SecretCode[secretCodeIndex]) secretCodeIndex++;
                else secretCodeIndex = key == SecretCode[0] ? 1 : 0;
                if (secretCodeIndex < SecretCode.Length) break;

                secretCodeIndex = 0;
                secretUnlocked = true;
                SaveProgress();
                banner = T("SECRET FOUND: COMET BALL!", "隠し技発見：COMET BREAKER！");
                bannerTimer = 3f;
                specialSound.Play(audioSource, masterVolume);
                break;
            }

            if (menuPage != MenuPage.Main)
            {
                if (menuPage == MenuPage.Help && ConfirmPressed())
                {
                    BackToMainMenu();
                    return;
                }

                if (menuPage == MenuPage.Settings) UpdateSettingsInput();
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)) BackToMainMenu();
                return;
            }

            int menuMove = ReadMenuVertical();
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || menuMove > 0) menuIndex = (menuIndex + 1) % 4;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || menuMove < 0) menuIndex = (menuIndex + 3) % 4;
            if (ConfirmPressed()) ActivateMainMenu(menuIndex);
        }

        static int MenuAxisStep(float axis, ref bool ready)
        {
            if (Mathf.Abs(axis) < 0.35f)
            {
                ready = true;
                return 0;
            }
            if (!ready || Mathf.Abs(axis) < 0.65f) return 0;
            ready = false;
            return axis > 0f ? 1 : -1;
        }

        int ReadMenuVertical() => -MenuAxisStep(Input.GetAxisRaw("PadVertical"), ref menuVerticalReady);
        int ReadMenuHorizontal() => MenuAxisStep(Input.GetAxisRaw("PadHorizontal"), ref menuHorizontalReady);
        static bool ConfirmPressed() => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);

        void ActivateMainMenu(int index)
        {
            menuIndex = index;
            if (index == 0) StartCup();
            else if (index == 1)
            {
                menuPage = MenuPage.Help;
                menuIndex = 0;
            }
            else if (index == 2)
            {
                menuPage = MenuPage.Settings;
                menuIndex = 0;
            }
            else
            {
                RequestConfirmation(ConfirmAction.Quit);
                return;
            }
            passSound.Play(audioSource, masterVolume * 0.65f);
        }

        void UpdateSettingsInput()
        {
            int menuMove = ReadMenuVertical();
            int volumeMove = ReadMenuHorizontal();
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || menuMove > 0) menuIndex = (menuIndex + 1) % 8;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || menuMove < 0) menuIndex = (menuIndex + 7) % 8;
            if (menuIndex == 0)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || volumeMove < 0) masterVolume = Mathf.Max(0f, masterVolume - 0.1f);
                if (Input.GetKeyDown(KeyCode.RightArrow) || volumeMove > 0) masterVolume = Mathf.Min(1f, masterVolume + 0.1f);
                PlayerPrefs.SetFloat("volume", masterVolume);
            }
            else if (menuIndex == 1 && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || volumeMove != 0))
            {
                ToggleLanguage();
            }
            else if (menuIndex == 2 && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || volumeMove != 0))
            {
                CycleDifficulty(Input.GetKeyDown(KeyCode.LeftArrow) || volumeMove < 0 ? -1 : 1);
            }

            if (!ConfirmPressed()) return;
            if (menuIndex == 1)
            {
                ToggleLanguage();
            }
            else if (menuIndex == 2)
            {
                CycleDifficulty(1);
            }
            else if (menuIndex == 3)
            {
                ToggleSetting(ref shakeEnabled, "screen_shake");
            }
            else if (menuIndex == 4)
            {
                ToggleSetting(ref flashEnabled, "flash_effects");
            }
            else if (menuIndex == 5)
            {
                ToggleSetting(ref highContrast, "high_contrast");
            }
            else if (menuIndex == 6)
            {
                Screen.fullScreenMode = Screen.fullScreen ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            }
            else if (menuIndex == 7)
            {
                BackToMainMenu();
            }
        }

        static void ToggleSetting(ref bool value, string key)
        {
            value = !value;
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        void ToggleLanguage()
        {
            japanese = !japanese;
            PlayerPrefs.SetInt("language", japanese ? 1 : 0);
            passSound.Play(audioSource, masterVolume * 0.55f);
        }

        void CycleDifficulty(int direction)
        {
            difficulty = DifficultyRules.Cycle(difficulty, direction);
            PlayerPrefs.SetInt("difficulty", (int)difficulty);
            passSound.Play(audioSource, masterVolume * 0.55f);
        }

        void RequestConfirmation(ConfirmAction action)
        {
            confirmAction = action;
            confirmIndex = 1;
            passSound.Play(audioSource, masterVolume * 0.55f);
        }

        void UpdateConfirmationInput()
        {
            int menuMove = ReadMenuVertical();
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow) ||
                Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || menuMove != 0)
                confirmIndex = 1 - confirmIndex;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                CancelConfirmation();
                return;
            }

            if (!ConfirmPressed()) return;
            if (confirmIndex == 0) ExecuteConfirmedAction();
            else CancelConfirmation();
        }

        void CancelConfirmation()
        {
            confirmAction = ConfirmAction.None;
            passSound.Play(audioSource, masterVolume * 0.45f);
        }

        void ExecuteConfirmedAction()
        {
            ConfirmAction action = confirmAction;
            confirmAction = ConfirmAction.None;
            if (action == ConfirmAction.Quit)
            {
                PlayerPrefs.Save();
                Application.Quit();
            }
            else if (action == ConfirmAction.RestartMatch) StartMatch();
            else if (action == ConfirmAction.ReturnToTitle) ReturnToTitle();
        }

        void BackToMainMenu()
        {
            PlayerPrefs.Save();
            menuPage = MenuPage.Main;
            menuIndex = 0;
        }

        void UpdatePauseInput()
        {
            if (menuPage == MenuPage.Settings)
            {
                UpdateSettingsInput();
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)) BackToMainMenu();
                return;
            }

            int menuMove = ReadMenuVertical();
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || menuMove > 0) menuIndex = (menuIndex + 1) % 4;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || menuMove < 0) menuIndex = (menuIndex + 3) % 4;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
            {
                rules.TogglePause();
                return;
            }
            if (ConfirmPressed()) ActivatePauseMenu(menuIndex);
        }

        void ActivatePauseMenu(int index)
        {
            menuIndex = index;
            if (index == 0) rules.TogglePause();
            else if (index == 1)
            {
                menuPage = MenuPage.Settings;
                menuIndex = 0;
            }
            else if (index == 2) RequestConfirmation(ConfirmAction.RestartMatch);
            else RequestConfirmation(ConfirmAction.ReturnToTitle);
        }

        void UpdateResultsInput()
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || ReadMenuVertical() != 0)
                menuIndex = 1 - menuIndex;
            if (ConfirmPressed()) ActivateResultsMenu(menuIndex);
        }

        void ActivateResultsMenu(int index)
        {
            menuIndex = index;
            if (index == 0)
            {
                if (cupOutcome == CupOutcome.Champion) StartCup();
                else StartMatch();
            }
            else ReturnToTitle();
        }

        void ReturnToTitle()
        {
            rules.ReturnToTitle();
            cup.Reset();
            activeRound = 0;
            ApplyOpponentIdentity();
            menuPage = MenuPage.Main;
            menuIndex = 0;
            ResetPositions();
        }

        void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = NewStyle(86, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            subtitleStyle = NewStyle(27, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.45f, 0.95f, 1f));
            headingStyle = NewStyle(42, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            bodyStyle = NewStyle(26, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.9f, 0.95f, 1f));
            bodyStyle.wordWrap = true;
            centeredBodyStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter };
            buttonStyle = NewStyle(30, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            buttonStyle.hover.textColor = new Color(1f, 0.95f, 0.3f);
            buttonStyle.padding = new RectOffset(20, 20, 10, 10);
            hudStyle = NewStyle(38, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            hugeStyle = NewStyle(72, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            smallStyle = NewStyle(24, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.82f, 0.9f, 0.95f));
        }

        GUIStyle NewStyle(int size, TextAnchor anchor, FontStyle style, Color color)
        {
            var result = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                alignment = anchor,
                fontStyle = style
            };
            result.normal.textColor = color;
            return result;
        }

        void DrawTitle()
        {
            DrawRect(new Rect(0f, 0f, 1920f, 1080f), new Color(0.01f, 0.02f, 0.05f, 0.42f));
            DrawRect(new Rect(215f, 115f, 1490f, 850f), new Color(0.025f, 0.045f, 0.1f, 0.9f));
            DrawRect(new Rect(215f, 115f, 14f, 850f), homeColor);
            DrawRect(new Rect(1691f, 115f, 14f, 850f), awayColor);

            if (menuPage == MenuPage.Main) DrawMainMenu();
            else if (menuPage == MenuPage.Help) DrawHelp();
            else DrawSettings();

            if (bannerTimer > 0f)
                GUI.Label(new Rect(350f, 925f, 1220f, 70f), banner, subtitleStyle);
        }

        void DrawMainMenu()
        {
            GUI.Label(new Rect(1430f, 130f, 220f, 38f), "v" + Application.version, smallStyle);
            GUI.Label(new Rect(300f, 165f, 1320f, 110f), "SPARK STRIKERS", titleStyle);
            GUI.Label(new Rect(300f, 265f, 1320f, 55f), cupsWon > 0 ?
                T("FOUR RIVALS. ONE CUP. IMPOSSIBLE SHOTS.", "4つの宿敵。1つのカップ。常識外れの必殺技。") :
                T("THREE RIVALS. ONE CUP. IMPOSSIBLE SHOTS.", "3つの宿敵。1つのカップ。常識外れの必殺技。"), subtitleStyle);
            string[] labels = { T("ARCADE CUP", "アーケードカップ"), T("HOW TO PLAY", "あそびかた"), T("SETTINGS", "設定"), T("QUIT", "終了") };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(670f, 380f + i * 92f, 580f, 68f);
                bool selected = menuIndex == i;
                DrawRect(rect, selected ? new Color(homeColor.r, homeColor.g, homeColor.b, 0.85f) : new Color(0.08f, 0.13f, 0.23f, 0.95f));
                if (GUI.Button(rect, (selected ? ">  " : string.Empty) + labels[i], buttonStyle)) ActivateMainMenu(i);
            }
            string secret = cupsWon > 0 ?
                (secretUnlocked ? T("★ COMET BREAKER + NOVA DISCOVERED", "★ COMET BREAKER + NOVA 発見済み") : T("★ NEW CHALLENGER: NOVA", "★ 新たな挑戦者：NOVA")) :
                (secretUnlocked ? T("★ COMET BALL DISCOVERED", "★ COMET BREAKER 発見済み") : T("The crowd remembers old button legends...", "観客は昔のボタン伝説を覚えている…"));
            GUI.Label(new Rect(410f, 775f, 1100f, 42f), secret, smallStyle);
            GUI.Label(new Rect(410f, 820f, 1100f, 42f), T("CUPS WON: ", "優勝：") + cupsWon + T("   •   BEST ROUND: ", "回   •   最高到達：") + bestRound + " / " + availableRivalCount, smallStyle);
            GUI.Label(new Rect(410f, 865f, 1100f, 42f), T("ARROWS / WASD / LEFT STICK TO CHOOSE   •   ENTER / A TO CONFIRM", "矢印 / WASD / 左スティックで選択   •   ENTER / Aで決定"), smallStyle);
        }

        void DrawHelp()
        {
            GUI.Label(new Rect(350f, 165f, 1220f, 80f), T("HOW TO PLAY", "あそびかた"), headingStyle);
            GUI.Label(new Rect(460f, 275f, 1000f, 390f),
                T("MOVE       WASD / ARROWS / LEFT STICK\n" +
                  "SHOOT      HOLD Z OR SPACE / A, RELEASE TO FIRE\n" +
                  "PASS / HIT X / X BUTTON (HIT WITHOUT THE BALL)\n" +
                  "DASH / TACKLE  SHIFT / B BUTTON\n" +
                  "SWITCH     Q / LB (WHILE DEFENDING)\n" +
                  "SPECIAL    C / Y BUTTON WHEN THE SPARK METER IS FULL\n" +
                  "PAUSE      ESC / MENU BUTTON",
                  "移動　　　WASD / 矢印 / 左スティック\n" +
                  "シュート　Z または SPACE / Aを長押しして離す\n" +
                  "パス / 攻撃　X / Xボタン（ボールなしで攻撃）\n" +
                  "ダッシュ / タックル　SHIFT / Bボタン\n" +
                  "選手切替　Q / LB（守備中）\n" +
                  "必殺技　　SPARK METER満タン時に C / Y\n" +
                  "ポーズ　　ESC / MENUボタン"), bodyStyle);
            GUI.Label(new Rect(420f, 680f, 1080f, 115f), T("No fouls: rough hits knock players down and spill the ball.\nPass, shoot, keep possession, or land hits to charge your impossible shot.\nBeat BLAZE, FROST and ECLIPSE to claim the Arcade Cup.", "反則なし：ラフプレーで相手を倒し、ボールをこぼさせろ。\nパス、シュート、ボール保持、攻撃で必殺ゲージをためよう。\nBLAZE、FROST、ECLIPSEを倒してカップをつかめ。"), centeredBodyStyle);
            if (DrawButton(new Rect(710f, 820f, 500f, 68f), T("BACK", "もどる"), true)) BackToMainMenu();
        }

        void DrawSettings()
        {
            GUI.Label(new Rect(350f, 150f, 1220f, 80f), T("SETTINGS", "設定"), headingStyle);
            if (menuIndex == 0) DrawRect(new Rect(500f, 250f, 920f, 68f), new Color(homeColor.r, homeColor.g, homeColor.b, 0.45f));
            GUI.Label(new Rect(530f, 260f, 330f, 55f), (menuIndex == 0 ? ">  " : string.Empty) + T("MASTER VOLUME", "マスター音量"), bodyStyle);
            float newVolume = GUI.HorizontalSlider(new Rect(870f, 275f, 430f, 28f), masterVolume, 0f, 1f);
            if (!Mathf.Approximately(newVolume, masterVolume))
            {
                masterVolume = newVolume;
                PlayerPrefs.SetFloat("volume", masterVolume);
            }
            GUI.Label(new Rect(1315f, 260f, 100f, 55f), Mathf.RoundToInt(masterVolume * 100f) + "%", bodyStyle);

            if (DrawButton(new Rect(610f, 335f, 700f, 64f), T("LANGUAGE:  <  ENGLISH  >", "言語：  <  日本語  >"), menuIndex == 1))
                ToggleLanguage();
            if (DrawButton(new Rect(610f, 410f, 700f, 64f), T("DIFFICULTY:  <  ", "難易度：  <  ") + difficultyName + "  >", menuIndex == 2))
                CycleDifficulty(1);
            if (DrawButton(new Rect(610f, 485f, 700f, 64f), T("SCREEN SHAKE: ", "画面揺れ：") + ToggleText(shakeEnabled), menuIndex == 3))
            {
                ToggleSetting(ref shakeEnabled, "screen_shake");
            }
            if (DrawButton(new Rect(610f, 560f, 700f, 64f), T("FLASH EFFECTS: ", "全画面フラッシュ：") + ToggleText(flashEnabled), menuIndex == 4))
            {
                ToggleSetting(ref flashEnabled, "flash_effects");
            }
            if (DrawButton(new Rect(610f, 635f, 700f, 64f), T("HIGH CONTRAST: ", "ハイコントラスト：") + ToggleText(highContrast), menuIndex == 5))
            {
                ToggleSetting(ref highContrast, "high_contrast");
            }
            if (DrawButton(new Rect(610f, 710f, 700f, 64f), T("FULLSCREEN: ", "フルスクリーン：") + ToggleText(Screen.fullScreen), menuIndex == 6))
                Screen.fullScreenMode = Screen.fullScreen ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            if (DrawButton(new Rect(710f, 810f, 500f, 64f), T("SAVE & BACK", "保存してもどる"), menuIndex == 7)) BackToMainMenu();
        }

        void DrawMatchHud()
        {
            DrawRect(new Rect(620f, 8f, 680f, 120f), new Color(0.02f, 0.035f, 0.08f, 0.92f));
            GUI.Label(new Rect(760f, 8f, 400f, 30f), T("ROUND ", "ラウンド ") + (activeRound + 1) + " / " + cup.RoundCount + "  •  " + difficultyName, smallStyle);
            GUI.Label(new Rect(640f, 37f, 160f, 42f), "SPARK", smallStyle);
            GUI.Label(new Rect(1120f, 37f, 160f, 42f), opponentName, smallStyle);
            GUI.Label(new Rect(795f, 30f, 330f, 84f), rules.HomeScore + "  -  " + rules.AwayScore, hudStyle);
            int seconds = Mathf.CeilToInt(rules.SecondsRemaining);
            string clock = rules.IsGoldenGoal ? T("GOLDEN GOAL", "ゴールデンゴール") : (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            GUI.Label(new Rect(830f, 99f, 260f, 36f), clock, smallStyle);

            DrawRect(new Rect(470f, 940f, 980f, 112f), new Color(0.01f, 0.02f, 0.05f, 0.88f));
            DrawMeter(new Rect(520f, 970f, 880f, 34f), rules.SpecialMeter / MatchRules.MaxMeter, homeColor, "SPARK METER");
            DrawMeter(new Rect(1510f, 180f, 280f, 22f), rivalMeter / MatchRules.MaxMeter, awayColor, T("RIVAL POWER", "相手ゲージ"));
            string playerSpecial = secretUnlocked ? "COMET BREAKER" : "STAR BREAKER";
            GUI.Label(new Rect(510f, 1015f, 900f, 34f), rules.SpecialMeter >= MatchRules.MaxMeter ? playerSpecial + T(" READY — PRESS C / Y", " 発動可能 — C / Y") : T("X HIT   •   SHIFT TACKLE   •   ROUGH HITS = POWER", "X 攻撃   •   SHIFT タックル   •   ラフプレー = ゲージ"), smallStyle);

            if (shotCharge > 0f)
                DrawMeter(new Rect(760f, 900f, 400f, 24f), shotCharge, new Color(1f, 0.85f, 0.2f), T("SHOT", "シュート"));
            if (rules.Phase == MatchPhase.Kickoff || bannerTimer > 0f)
                GUI.Label(new Rect(390f, 405f, 1140f, 130f), banner, hugeStyle);
        }

        void DrawMeter(Rect rect, float value, Color color, string label)
        {
            DrawRect(rect, new Color(0.01f, 0.02f, 0.05f, 0.88f));
            Rect fill = new Rect(rect.x + 5f, rect.y + 5f, (rect.width - 10f) * Mathf.Clamp01(value), rect.height - 10f);
            DrawRect(fill, color);
            GUI.Label(new Rect(rect.x, rect.y - 4f, rect.width, rect.height + 8f), label, smallStyle);
        }

        void DrawPause()
        {
            DrawOverlayPanel(T("PAUSED", "ポーズ"));
            string[] labels = { T("RESUME", "試合にもどる"), T("SETTINGS", "設定"), T("RESTART MATCH", "試合をやり直す"), T("RETURN TO TITLE", "タイトルにもどる") };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(690f, 405f + i * 78f, 540f, 62f);
                if (DrawButton(rect, labels[i], menuIndex == i)) ActivatePauseMenu(i);
            }
        }

        void DrawPauseSettings()
        {
            DrawRect(new Rect(0f, 0f, 1920f, 1080f), new Color(0f, 0f, 0.02f, 0.82f));
            DrawRect(new Rect(350f, 120f, 1220f, 820f), new Color(0.025f, 0.045f, 0.1f, 0.98f));
            DrawSettings();
        }

        void DrawConfirmation()
        {
            string heading = confirmAction == ConfirmAction.Quit ? T("QUIT GAME?", "ゲームを終了しますか？") :
                confirmAction == ConfirmAction.RestartMatch ? T("RESTART MATCH?", "試合をやり直しますか？") :
                T("RETURN TO TITLE?", "タイトルにもどりますか？");
            string detail = confirmAction == ConfirmAction.Quit ?
                (saveFailed ? T("Progress could not be saved. Quitting may lose progress.", "進行状況を保存できませんでした。終了すると失われる可能性があります。") : T("Your progress is saved automatically.", "進行状況は自動保存されています。")) :
                T("Current match progress will be lost.", "現在の試合状況は失われます。");
            string confirm = confirmAction == ConfirmAction.Quit ? T("YES — QUIT", "はい — 終了") :
                confirmAction == ConfirmAction.RestartMatch ? T("YES — RESTART", "はい — やり直す") :
                T("YES — TITLE", "はい — タイトルへ");

            DrawRect(new Rect(0f, 0f, 1920f, 1080f), new Color(0f, 0f, 0.02f, 0.78f));
            DrawRect(new Rect(520f, 245f, 880f, 570f), new Color(0.025f, 0.045f, 0.1f, 0.98f));
            GUI.Label(new Rect(570f, 295f, 780f, 80f), heading, headingStyle);
            GUI.Label(new Rect(620f, 385f, 680f, 60f), detail, centeredBodyStyle);

            Rect yes = new Rect(690f, 500f, 540f, 66f);
            DrawRect(yes, confirmIndex == 0 ? RivalColors[0] : new Color(0.08f, 0.13f, 0.23f));
            if (GUI.Button(yes, (confirmIndex == 0 ? ">  " : string.Empty) + confirm, buttonStyle))
            {
                confirmIndex = 0;
                ExecuteConfirmedAction();
                return;
            }

            Rect no = new Rect(690f, 590f, 540f, 66f);
            if (DrawButton(no, T("NO — GO BACK", "いいえ — もどる"), confirmIndex == 1)) CancelConfirmation();
        }

        void DrawResults()
        {
            string result = cupOutcome == CupOutcome.Champion ? T("CUP CHAMPION!", "カップ優勝！") :
                cupOutcome == CupOutcome.Advanced ? T("ROUND CLEARED!", "ラウンド突破！") :
                rules.HomeScore < rules.AwayScore ? opponentName + T(" WINS", " 勝利") : T("DRAW", "引き分け");
            DrawOverlayPanel(result);
            GUI.Label(new Rect(660f, 380f, 600f, 70f), rules.HomeScore + "  -  " + rules.AwayScore, headingStyle);
            string detail = cupOutcome == CupOutcome.Advanced ? T("NEXT: ", "次：") + RivalNames[cup.RoundIndex] + T("   •   ROUND ", "   •   ラウンド ") + (cup.RoundIndex + 1) + " / " + cup.RoundCount :
                cupOutcome == CupOutcome.Champion && cupsWon == 1 ? T("NEW CHALLENGER UNLOCKED: NOVA", "新たな挑戦者解放：NOVA") :
                cupOutcome == CupOutcome.Champion ? T("CUPS WON: ", "優勝回数：") + cupsWon :
                T("WIN TO ADVANCE   •   ROUND ", "勝てば次へ   •   ラウンド ") + (activeRound + 1) + " / " + cup.RoundCount;
            GUI.Label(new Rect(600f, 450f, 720f, 44f), detail, smallStyle);
            string primary = cupOutcome == CupOutcome.Advanced ? T("NEXT MATCH", "次の試合") : cupOutcome == CupOutcome.Champion ? T("NEW CUP", "もう一度カップ") : T("RETRY", "リトライ");
            string[] labels = { primary, T("RETURN TO TITLE", "タイトルにもどる") };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(690f, 510f + i * 88f, 540f, 66f);
                if (DrawButton(rect, labels[i], menuIndex == i)) ActivateResultsMenu(i);
            }
        }

        void DrawOverlayPanel(string heading)
        {
            DrawRect(new Rect(0f, 0f, 1920f, 1080f), new Color(0f, 0f, 0.02f, 0.72f));
            DrawRect(new Rect(520f, 245f, 880f, 570f), new Color(0.025f, 0.045f, 0.1f, 0.96f));
            GUI.Label(new Rect(570f, 285f, 780f, 95f), heading, hugeStyle);
        }

        bool DrawButton(Rect rect, string label, bool highlighted)
        {
            DrawRect(rect, highlighted ? homeColor : new Color(0.08f, 0.13f, 0.23f));
            return GUI.Button(rect, (highlighted ? ">  " : string.Empty) + label, buttonStyle);
        }

        void DrawSaveWarning()
        {
            Rect rect = new Rect(24f, 24f, 620f, 58f);
            DrawRect(rect, new Color(0.48f, 0.055f, 0.035f, 0.98f));
            GUI.Label(rect, T("!  PROGRESS NOT SAVED", "!  進行状況を保存できません"), smallStyle);
        }

        void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, squareTexture);
            GUI.color = previous;
        }

        void OnDestroy()
        {
            if (lineMaterial != null) Destroy(lineMaterial);
            if (particleMaterial != null) Destroy(particleMaterial);
            if (circleSprite != null) Destroy(circleSprite);
            if (squareSprite != null) Destroy(squareSprite);
            if (circleTexture != null) Destroy(circleTexture);
            if (squareTexture != null) Destroy(squareTexture);
        }
    }

    static class AudioClipExtensions
    {
        public static void Play(this AudioClip clip, AudioSource source, float volume)
        {
            if (clip != null && source != null && volume > 0f)
                source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}
