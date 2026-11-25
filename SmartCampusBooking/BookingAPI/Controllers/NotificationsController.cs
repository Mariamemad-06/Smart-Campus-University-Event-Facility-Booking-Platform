using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingAPI.Extensions;

namespace BookingAPI.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private readonly INotificationService _svc;

        public NotificationsController(INotificationService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool unreadOnly = false)
            => Ok(await _svc.GetForUserAsync(User.GetUserId(), unreadOnly));

        [HttpGet("unread-count")]
        public async Task<IActionResult> Count()
            => Ok(new { count = await _svc.UnreadCountAsync(User.GetUserId()) });

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> Read(int id)
            => await _svc.MarkReadAsync(id, User.GetUserId()) ? NoContent() : NotFound();

        [HttpPut("read-all")]
        public async Task<IActionResult> ReadAll()
        {
            await _svc.MarkAllReadAsync(User.GetUserId());
            return NoContent();
        }

        public record SendDto(string UserId, string Title, string Message, string? Type);

        [HttpPost("send")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Send(SendDto d)
        {
            await _svc.CreateAsync(d.UserId, d.Title, d.Message, d.Type ?? "Info");
            return Ok();
        }
    }
}
