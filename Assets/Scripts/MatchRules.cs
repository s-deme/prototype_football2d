using System;

namespace SparkStrikers
{
    public enum MatchPhase
    {
        Title,
        Kickoff,
        Playing,
        Paused,
        Results
    }

    public enum CupOutcome
    {
        Retry,
        Advanced,
        Champion
    }

    public enum SpecialShotKind
    {
        None,
        Star,
        Comet,
        Blaze,
        Frost,
        Eclipse,
        Nova
    }

    public enum GameDifficulty
    {
        Rookie,
        Arcade,
        Legend
    }

    public static class DifficultyRules
    {
        public static float Speed(GameDifficulty difficulty) => difficulty == GameDifficulty.Rookie ? 0.88f : difficulty == GameDifficulty.Legend ? 1.12f : 1f;
        public static float Decisions(GameDifficulty difficulty) => difficulty == GameDifficulty.Rookie ? 0.8f : difficulty == GameDifficulty.Legend ? 1.25f : 1f;
        public static float SpecialCharge(GameDifficulty difficulty) => difficulty == GameDifficulty.Rookie ? 0.75f : difficulty == GameDifficulty.Legend ? 1.25f : 1f;
        public static GameDifficulty Cycle(GameDifficulty difficulty, int direction) => (GameDifficulty)(((int)difficulty + direction + 3) % 3);
    }

    public static class SpecialShotRules
    {
        public static float LaunchSpeed(SpecialShotKind kind)
        {
            switch (kind)
            {
                case SpecialShotKind.Star: return 16.5f;
                case SpecialShotKind.Comet: return 16.2f;
                case SpecialShotKind.Blaze: return 16f;
                case SpecialShotKind.Frost: return 14.8f;
                case SpecialShotKind.Eclipse: return 15.5f;
                case SpecialShotKind.Nova: return 17.2f;
                default: return 0f;
            }
        }

        public static float Drag(SpecialShotKind kind)
        {
            switch (kind)
            {
                case SpecialShotKind.None: return 1.35f;
                case SpecialShotKind.Blaze: return 0f;
                case SpecialShotKind.Frost: return 0.6f;
                case SpecialShotKind.Star: return 0.45f;
                case SpecialShotKind.Nova: return 0.08f;
                default: return 0.25f;
            }
        }

        public static float Knockback(SpecialShotKind kind) => kind == SpecialShotKind.Blaze ? 0.42f : kind == SpecialShotKind.Nova ? 0.34f : 0.22f;
        public static bool SlowsPlayers(SpecialShotKind kind) => kind == SpecialShotKind.Frost;
    }

    [Serializable]
    public sealed class ArcadeCup
    {
        public int RoundCount { get; }
        public int RoundIndex { get; private set; }
        public bool IsChampion { get; private set; }

        public ArcadeCup(int roundCount)
        {
            if (roundCount < 2) throw new ArgumentOutOfRangeException(nameof(roundCount));
            RoundCount = roundCount;
        }

        public CupOutcome RecordResult(int homeScore, int awayScore)
        {
            if (homeScore < 0 || awayScore < 0) throw new ArgumentOutOfRangeException();
            if (homeScore <= awayScore) return CupOutcome.Retry;
            if (RoundIndex < RoundCount - 1)
            {
                RoundIndex++;
                return CupOutcome.Advanced;
            }

            IsChampion = true;
            return CupOutcome.Champion;
        }

        public void Reset()
        {
            RoundIndex = 0;
            IsChampion = false;
        }
    }

    [Serializable]
    public sealed class MatchRules
    {
        public const float MaxMeter = 100f;

        public int HomeScore { get; private set; }
        public int AwayScore { get; private set; }
        public float SecondsRemaining { get; private set; }
        public float SpecialMeter { get; private set; }
        public bool IsGoldenGoal { get; private set; }
        public MatchPhase Phase { get; private set; } = MatchPhase.Title;

        readonly float matchLength;

        public MatchRules(float matchLengthSeconds)
        {
            if (matchLengthSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(matchLengthSeconds));

            matchLength = matchLengthSeconds;
            SecondsRemaining = matchLength;
        }

        public void StartMatch()
        {
            HomeScore = 0;
            AwayScore = 0;
            SecondsRemaining = matchLength;
            SpecialMeter = 0f;
            IsGoldenGoal = false;
            Phase = MatchPhase.Kickoff;
        }

        public void StartPlay()
        {
            if (Phase == MatchPhase.Kickoff)
                Phase = MatchPhase.Playing;
        }

        public void Tick(float deltaSeconds)
        {
            if (Phase != MatchPhase.Playing || deltaSeconds <= 0f || IsGoldenGoal)
                return;

            SecondsRemaining = Math.Max(0f, SecondsRemaining - deltaSeconds);
            if (SecondsRemaining == 0f)
            {
                if (HomeScore == AwayScore) IsGoldenGoal = true;
                else Phase = MatchPhase.Results;
            }
        }

        public void ScoreGoal(bool homeTeam)
        {
            if (Phase != MatchPhase.Playing)
                return;

            if (homeTeam) HomeScore++;
            else AwayScore++;
            Phase = IsGoldenGoal ? MatchPhase.Results : MatchPhase.Kickoff;
        }

        public void AddMeter(float amount)
        {
            if (amount > 0f)
                SpecialMeter = Math.Min(MaxMeter, SpecialMeter + amount);
        }

        public bool SpendSpecial()
        {
            if (Phase != MatchPhase.Playing || SpecialMeter < MaxMeter)
                return false;

            SpecialMeter = 0f;
            return true;
        }

        public void TogglePause()
        {
            if (Phase == MatchPhase.Playing) Phase = MatchPhase.Paused;
            else if (Phase == MatchPhase.Paused) Phase = MatchPhase.Playing;
        }

        public void ReturnToTitle() => Phase = MatchPhase.Title;
    }
}
