
using BookingMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BookingMVC.Controllers
{
    [Authorize(Roles = "Student,Faculty")]
    public class NotificationsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public NotificationsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private async Task<HttpClient?> GetAuthorizedClient()
        {
            var token = await HttpContext.GetTokenAsync("access_token");

            if (string.IsNullOrEmpty(token))
                return null;

            var client = _httpClientFactory.CreateClient("BookingApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = await GetAuthorizedClient();

            if (client == null)
                return Challenge();

            // Get all notifications
            var response = await client.GetAsync("api/notifications");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    await response.Content.ReadAsStringAsync();

                return View(new List<NotificationViewModel>());
            }

            var notifications = await response.Content
                .ReadFromJsonAsync<List<NotificationViewModel>>();

            // Get unread notifications count
            var countResponse = await client.GetAsync(
                "api/notifications/unread-count");

            if (countResponse.IsSuccessStatusCode)
            {
                var countData = await countResponse.Content
                    .ReadFromJsonAsync<UnreadCountResponse>();

                ViewBag.UnreadCount = countData?.Count ?? 0;
            }
            else
            {
                ViewBag.UnreadCount = 0;
            }

            return View(notifications ?? new List<NotificationViewModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var client = await GetAuthorizedClient();

            if (client == null)
                return Challenge();

            var response = await client.PutAsync(
                $"api/notifications/{id}/read", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] =
                    "Notification marked as read.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    await response.Content.ReadAsStringAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var client = await GetAuthorizedClient();

            if (client == null)
                return Challenge();

            var response = await client.PutAsync(
                "api/notifications/read-all", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] =
                    "All notifications marked as read.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    await response.Content.ReadAsStringAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var client = await GetAuthorizedClient();

            if (client == null)
                return Unauthorized();

            var response = await client.GetAsync(
                "api/notifications/unread-count");

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var countData = await response.Content
                .ReadFromJsonAsync<UnreadCountResponse>();

            return Json(new { count = countData?.Count ?? 0 });
        }
    }

    public class UnreadCountResponse
    {
        public int Count { get; set; }
    }
}
