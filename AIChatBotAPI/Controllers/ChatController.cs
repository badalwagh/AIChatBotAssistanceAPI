using AIChatBotAPI.Models;
using AIChatBotAPI.Service;
using Microsoft.AspNetCore.Mvc;

namespace AIChatBotAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly GeminiService _service;

        // Inject the service instead of `new`-ing it
        public ChatController(GeminiService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message is required." });

            var response = await _service.GetResponse(request.Message);
            return Ok(new { reply = response });
        }
    }
}