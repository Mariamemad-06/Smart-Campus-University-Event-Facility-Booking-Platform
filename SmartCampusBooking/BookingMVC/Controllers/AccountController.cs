
using BookingMVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace BookingMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Display Login page
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // Process Login form
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("BookingApi");

            try
            {
                var response = await client.PostAsJsonAsync(
                    "api/auth/login",
                    new
                    {
                        model.Email,
                        model.Password
                    });

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "", "Invalid email or password.");
                    return View(model);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<LoginApiResponse>();

                if (result == null ||
                    string.IsNullOrEmpty(result.Token) ||
                    string.IsNullOrEmpty(result.UserId) ||
                    result.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    ModelState.AddModelError(
                        "", "Login response was invalid.");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        result.UserId),

                    new Claim(
                        ClaimTypes.Name,
                        result.FullName),

                    new Claim(
                        ClaimTypes.Email,
                        result.Email),

                    new Claim(
                        ClaimTypes.Role,
                        result.Role)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var properties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = result.ExpiresAt,
                    AllowRefresh = false
                };

                // Store the API access token in the protected
                // MVC authentication ticket.
                properties.StoreTokens(new[]
                {
                    new AuthenticationToken
                    {
                        Name = "access_token",
                        Value = result.Token
                    }
                });

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    properties);

                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Authentication service is unavailable.");

                return View(model);
            }
        }

        // Display Registration page
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // Process Registration form
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Role != "Student" &&
                model.Role != "Faculty")
            {
                ModelState.AddModelError(
                    "Role", "Invalid role.");

                return View(model);
            }

            var client = _httpClientFactory.CreateClient("BookingApi");

            try
            {
                var response = await client.PostAsJsonAsync(
                    "api/auth/register",
                    new
                    {
                        model.FullName,
                        model.Email,
                        model.Password,
                        model.UniversityId,
                        model.Role
                    });

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "",
                        "Registration failed. Check your details or whether the email is already registered.");

                    return View(model);
                }

                TempData["SuccessMessage"] =
                    "Registration successful. Please log in.";

                return RedirectToAction("Login");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Registration service is unavailable.");

                return View(model);
            }
        }

        // Display Profile page
        // Only authenticated users can access it.
        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            return View();
        }

        // Logout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }

        // Represents the JSON response from BookingAPI
        private class LoginApiResponse
        {
            public string Token { get; set; } = string.Empty;

            public string UserId { get; set; } = string.Empty;

            public string FullName { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public string Role { get; set; } = string.Empty;

            public DateTimeOffset ExpiresAt { get; set; }
        }
    }
}
