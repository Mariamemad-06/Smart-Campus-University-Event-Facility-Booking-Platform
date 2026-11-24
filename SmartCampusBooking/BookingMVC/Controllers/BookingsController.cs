
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

                return RedirectToAction("Create");
            }

            var errorMessage = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, errorMessage);

            return View(model);
        }
    }

    public class BookingResponse
    {
        public string? Message { get; set; }
        public int BookingId { get; set; }
        public string? FacilityName { get; set; }
    }
}
