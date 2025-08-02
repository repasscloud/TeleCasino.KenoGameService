using TeleCasino.KenoGameService.Models;

namespace TeleCasino.KenoGameService.Services.Interface;

public interface IKenoGameService
{
    Task<KenoResult> PlayGameAsync(int wager, List<int> numbers, int gameSessionId);
}