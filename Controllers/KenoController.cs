using Microsoft.AspNetCore.Mvc;
using TeleCasino.KenoGameService.Models;
using TeleCasino.KenoGameService.Services.Interface;

namespace TeleCasino.KenoGameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KenoController : ControllerBase
    {
        private readonly IKenoGameService _kenoGameService;

        public KenoController(IKenoGameService kenoGameService)
        {
            _kenoGameService = kenoGameService;
        }

        /// <summary>
        /// Plays a Keno game and returns the result with a generated video file path.
        /// </summary>
        /// <param name="wager">Amount wagered.</param>
        /// <param name="numbers">List of player numbers (7–15 distinct numbers, 1–80).</param>
        /// <param name="gameSessionId">Game session identifier.</param>
        [HttpPost("play")]
        [ProducesResponseType(typeof(KenoResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PlayGame(
            [FromQuery] int wager,
            [FromBody] List<int> numbers,
            [FromQuery] int gameSessionId)
        {
            if (wager <= 0)
                return BadRequest("Wager must be a positive integer.");

            if (numbers == null || numbers.Count < 7 || numbers.Count > 15)
                return BadRequest("You must select between 7 and 15 numbers.");

            if (numbers.Distinct().Count() != numbers.Count)
                return BadRequest("Numbers must be unique.");

            if (numbers.Any(n => n < 1 || n > 80))
                return BadRequest("Numbers must be between 1 and 80.");

            var result = await _kenoGameService.PlayGameAsync(wager, numbers, gameSessionId);

            return Ok(result);
        }
    }
}
