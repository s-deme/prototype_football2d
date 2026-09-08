using System;
using SparkStrikers;

var match = new MatchRules(60f);
match.StartMatch();
Assert(match.Phase == MatchPhase.Kickoff, "match starts at kickoff");
match.StartPlay();
match.AddMeter(150f);
Assert(match.SpecialMeter == MatchRules.MaxMeter, "meter clamps");
Assert(match.SpendSpecial() && match.SpecialMeter == 0f, "special spends full meter");
match.ScoreGoal(true);
Assert(match.HomeScore == 1 && match.Phase == MatchPhase.Kickoff, "goal enters kickoff");
match.StartPlay();
match.Tick(60f);
Assert(match.Phase == MatchPhase.Results && match.SecondsRemaining == 0f, "timer ends match");
int finalScore = match.HomeScore;
match.ScoreGoal(true);
Assert(match.HomeScore == finalScore, "goals are ignored after the whistle");

var goldenGoal = new MatchRules(30f);
goldenGoal.StartMatch();
goldenGoal.StartPlay();
goldenGoal.Tick(30f);
Assert(goldenGoal.IsGoldenGoal && goldenGoal.Phase == MatchPhase.Playing, "tie enters golden goal");
goldenGoal.ScoreGoal(false);
Assert(goldenGoal.AwayScore == 1 && goldenGoal.Phase == MatchPhase.Results, "golden goal ends match");

var cup = new ArcadeCup(3);
Assert(cup.RecordResult(1, 1) == CupOutcome.Retry && cup.RoundIndex == 0, "draw retries round");
Assert(cup.RecordResult(2, 1) == CupOutcome.Advanced && cup.RoundIndex == 1, "win advances cup");
Assert(cup.RecordResult(1, 0) == CupOutcome.Advanced && cup.RoundIndex == 2, "second win reaches final");
Assert(cup.RecordResult(3, 2) == CupOutcome.Champion && cup.IsChampion, "final win completes cup");
cup.Reset();
Assert(cup.RoundIndex == 0 && !cup.IsChampion, "cup resets");

var hiddenCup = new ArcadeCup(4);
Assert(hiddenCup.RecordResult(1, 0) == CupOutcome.Advanced, "hidden cup advances after first win");
Assert(hiddenCup.RecordResult(1, 0) == CupOutcome.Advanced, "hidden cup advances after second win");
Assert(hiddenCup.RecordResult(1, 0) == CupOutcome.Advanced && hiddenCup.RoundIndex == 3, "hidden rival is the fourth round");
Assert(hiddenCup.RecordResult(1, 0) == CupOutcome.Champion, "hidden rival win completes cup");

Assert(SpecialShotRules.LaunchSpeed(SpecialShotKind.Star) > SpecialShotRules.LaunchSpeed(SpecialShotKind.Frost), "star is faster than frost");
Assert(SpecialShotRules.Drag(SpecialShotKind.Blaze) == 0f, "blaze accelerates without drag");
Assert(SpecialShotRules.LaunchSpeed(SpecialShotKind.Star) == 16.5f, "star keeps its launch speed");
Assert(SpecialShotRules.Drag(SpecialShotKind.None) == 1.35f, "ordinary shots keep their drag");
Assert(SpecialShotRules.LaunchSpeed((SpecialShotKind)99) == 0f, "unknown special has no launch speed");
Assert(SpecialShotRules.Drag((SpecialShotKind)99) == 0.25f, "unknown special keeps fallback drag");
Assert(SpecialShotRules.Knockback(SpecialShotKind.Blaze) > SpecialShotRules.Knockback(SpecialShotKind.Star), "blaze has extra knockback");
Assert(SpecialShotRules.SlowsPlayers(SpecialShotKind.Frost), "frost slows defenders");
Assert(SpecialShotRules.LaunchSpeed(SpecialShotKind.Nova) > SpecialShotRules.LaunchSpeed(SpecialShotKind.Star), "nova has the fastest launch");
Assert(SpecialShotRules.Drag(SpecialShotKind.Nova) < SpecialShotRules.Drag(SpecialShotKind.Eclipse), "nova keeps its speed");
Assert(SpecialShotRules.Knockback(SpecialShotKind.Nova) > SpecialShotRules.Knockback(SpecialShotKind.Star), "nova has extra knockback");
Assert(DifficultyRules.Speed(GameDifficulty.Rookie) < DifficultyRules.Speed(GameDifficulty.Arcade), "rookie AI is slower");
Assert(DifficultyRules.Decisions(GameDifficulty.Legend) > DifficultyRules.Decisions(GameDifficulty.Arcade), "legend AI decides faster");
Assert(DifficultyRules.SpecialCharge(GameDifficulty.Rookie) < DifficultyRules.SpecialCharge(GameDifficulty.Legend), "difficulty scales rival specials");
Assert(DifficultyRules.Cycle(GameDifficulty.Rookie, -1) == GameDifficulty.Legend, "difficulty wraps backward");
Assert(DifficultyRules.Cycle(GameDifficulty.Legend, 1) == GameDifficulty.Rookie, "difficulty wraps forward");
Console.WriteLine("Smoke test passed.");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
