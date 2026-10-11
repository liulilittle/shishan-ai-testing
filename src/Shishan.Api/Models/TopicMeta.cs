namespace Shishan.Api.Models;

public class TopicMeta
{
    public string Code { get; set; } = "";
    public string Tier { get; set; } = "";
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Scenario { get; set; } = "";
    public string Flow { get; set; } = "";
    public string Phenomenon { get; set; } = "";
    public string Expected { get; set; } = "";
    public List<string> Dimensions { get; set; } = new();
    public int Chance { get; set; }
    public string ScoreRule { get; set; } = "";
}
