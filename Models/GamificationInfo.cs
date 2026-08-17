namespace WeightTracker.Models;

public class BadgeInfo
{
    public string Id { get; set; } = "";
    public string Emoji { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Unlocked { get; set; }
}

public class QuestInfo
{
    public string Title { get; set; } = "";
    public int Progress { get; set; }
    public int Target { get; set; }
    public bool Completed => Progress >= Target;
}

public class GamificationInfo
{
    public int TotalXp { get; set; }
    public int Level { get; set; }
    public string RankTitle { get; set; } = "";
    public int XpIntoLevel { get; set; }
    public int XpForNextLevel { get; set; }
    public List<BadgeInfo> Badges { get; set; } = new();
    public QuestInfo WeeklyQuest { get; set; } = new();
}
