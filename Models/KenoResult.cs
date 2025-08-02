namespace TeleCasino.KenoGameService.Models;

public class KenoResult
{
    public required string Id { get; set; }
    public int Wager { get; set; }
    public decimal Payout { get; set; }
    public decimal NetGain { get; set; }
    public required string VideoFile { get; set; }
    public bool Win { get; set; }

    // game mechanics
    public List<int> PlayerNumbers { get; set; } = new();
    public List<int> DrawnNumbers { get; set; } = new();
    public int KenoHits { get; set; }
    public int GameSessionId { get; set; }
}