
using BookingMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
namespace BookingMVC.Controllers
{
    [Authorize(Roles = "Student,Faculty")]
    public class BookingsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BookingsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("BookingApi");

            var token = await HttpContext.GetTokenAsync("access_token");

            if (string.IsNullOrEmpty(token))
            {
                return Challenge();
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync(
                "api/bookings", model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BookingResponse>();

                TempData["SuccessMessage"] =
                    $"Booking created successfully! Your Booking ID is {result?.BookingId}.";

                return RedirectToAction("MyBookings");
            }

            var errorMessage = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, errorMessage);

            return View(model);
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
        public async Task<IActionResult> MyBookings()
        {
            var client = await GetAuthorizedClient();

            if (client == null)
                return Challenge();

            var response = await client.GetAsync(
                "api/bookings/my-bookings");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    await response.Content.ReadAsStringAsync();

                return View(new List<BookingViewModel>());
            }

            var bookings = await response.Content
                .ReadFromJsonAsync<List<BookingViewModel>>();

            return View(bookings ?? new List<BookingViewModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var client = await GetAuthorizedClient();

            if (client == null)
                return Challenge();

            var response = await client.PutAsync(
                $"api/bookings/{id}/cancel", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] =
                    "Booking cancelled successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();

                TempData["ErrorMessage"] = error;
            }

            return RedirectToAction(nameof(MyBookings));
        }
    }

    public class BookingResponse
    {
        public string? Message { get; set; }
        public int BookingId { get; set; }
        public string? FacilityName { get; set; }
    }
}
