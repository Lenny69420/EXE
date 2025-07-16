using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuatBook.Helpers;

namespace QuatBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly GeminiHelper _geminiService;

        public ChatController(GeminiHelper geminiService)
        {
            _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
                return BadRequest(new { Response = "Message cannot be empty." });

            var response = await _geminiService.GetChatResponseAsync(request.Message);
            return Ok(new { Response = response });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
