namespace Autobuilder
{
    // A single named score within a group, e.g. "Balance" within "STAB Coverage".
    // Score is the raw, unclamped value from TeamScorer (usually 0-1, but can dip fractionally
    // below 0 from floating-point rounding, or - for base-stat categories - exceed 1.0 for very
    // high-stat teams) - DisplayScore is the clamped 0-1 value safe for bars/charts.
    public class ScoreCategory(string name, string description, double score)
    {
        public string Name { get; } = name;
        public string Description { get; } = description;
        public double Score { get; } = score;
        public double DisplayScore => Math.Clamp(Score, 0.0, 1.0);
    }

    // A named group of related categories, e.g. "STAB Coverage" grouping All Types/Balance/Amount.
    public class ScoreGroup(string name, List<ScoreCategory> categories)
    {
        public string Name { get; } = name;
        public List<ScoreCategory> Categories { get; } = categories;
        public double AverageScore => Categories.Count == 0 ? 0 : Categories.Average(c => c.DisplayScore);
    }

    // Groups the 19 flat TeamScorer categories for display - see TeamScorer.CalculateBreakdown.
    public class TeamScoreBreakdown(List<ScoreGroup> groups, double totalScore)
    {
        public List<ScoreGroup> Groups { get; } = groups;
        public double TotalScore { get; } = totalScore;
        public double MaxScore => 19.0;
    }
}
