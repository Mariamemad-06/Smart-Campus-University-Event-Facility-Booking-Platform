using BookingMVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookingMVC.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminFacilitiesController(IHttpClientFactory factory) : Controller
{
    private const string Endpoint = "api/admin/facilities";
    private async Task<HttpClient?> ClientAsync()
    {
        var token = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token)) return null;
        var client = factory.CreateClient("BookingApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> ErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.TryGetProperty("message", out var message)) return message.GetString() ?? "Request failed.";
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                return string.Join(" ", errors.EnumerateObject().SelectMany(e => e.Value.EnumerateArray().Select(v => v.GetString())));
            if (root.TryGetProperty("title", out var title)) return title.GetString() ?? "Request failed.";
        }
        catch (JsonException) { }
        return $"Request failed (HTTP {(int)response.StatusCode}).";
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword)
    {
        using var client = await ClientAsync();
        if (client == null) return Challenge();
        using var response = await client.GetAsync(Endpoint);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return Challenge();
        if (response.StatusCode == HttpStatusCode.Forbidden) return Forbid();
        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = await ErrorAsync(response);
            return View(new List<FacilityAdminViewModel>());
        }
        var all = await response.Content.ReadFromJsonAsync<List<FacilityAdminViewModel>>() ?? new();
        ViewBag.Total = all.Count;
        ViewBag.Available = all.Count(f => f.Status == "Available");
        ViewBag.Maintenance = all.Count(f => f.Status == "Maintenance");
        ViewBag.Keyword = keyword;
        var q = keyword?.Trim();
        return View(string.IsNullOrEmpty(q) ? all : all.Where(f =>
            f.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            f.Location.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            f.Type.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    [HttpGet]
    public Task<IActionResult> Details(int id) => ReadAsync(id, "Details");
    [HttpGet]
    public Task<IActionResult> Edit(int id) => ReadAsync(id, "Form");

    private async Task<IActionResult> ReadAsync(int id, string view)
    {
        using var client = await ClientAsync();
        if (client == null) return Challenge();
        using var response = await client.GetAsync($"{Endpoint}/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return NotFound();
        if (response.StatusCode == HttpStatusCode.Unauthorized) return Challenge();
        if (response.StatusCode == HttpStatusCode.Forbidden) return Forbid();
        if (!response.IsSuccessStatusCode)
        {
            TempData["FacilityError"] = await ErrorAsync(response);
            return RedirectToAction(nameof(Index));
        }
        var model = await response.Content.ReadFromJsonAsync<FacilityAdminViewModel>();
        if (model == null) return NotFound();
        ViewBag.IsEdit = true;
        return View(view, model);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new FacilityAdminViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacilityAdminViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        using var client = await ClientAsync();
        if (client == null) return Challenge();
        using var response = await client.PostAsJsonAsync(Endpoint, model);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return Challenge();
        if (response.StatusCode == HttpStatusCode.Forbidden) return Forbid();
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await ErrorAsync(response));
            return View("Form", model);
        }
        TempData["FacilitySuccess"] = "Facility added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FacilityAdminViewModel model, bool confirmImpact = false)
    {
        model.Id = id;
        ViewBag.IsEdit = true;
        if (!ModelState.IsValid) return View("Form", model);
        using var client = await ClientAsync();
        if (client == null) return Challenge();
        using var response = await client.PutAsJsonAsync($"{Endpoint}/{id}?confirmImpact={confirmImpact.ToString().ToLowerInvariant()}", model);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return Challenge();
        if (response.StatusCode == HttpStatusCode.Forbidden) return Forbid();
        if (response.StatusCode == HttpStatusCode.NotFound) return NotFound();
        if (!response.IsSuccessStatusCode)
        {
            var error = await ErrorAsync(response);
            if (response.StatusCode == HttpStatusCode.Conflict && !confirmImpact && error.StartsWith("Maintenance will cancel", StringComparison.Ordinal))
            {
                ViewBag.Warning = error;
                return View("ConfirmMaintenance", model);
            }
            ModelState.AddModelError(string.Empty, error);
            return View("Form", model);
        }
        TempData["FacilitySuccess"] = "Facility updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool confirmImpact = false)
    {
        using var client = await ClientAsync();
        if (client == null) return Challenge();
        using var response = await client.DeleteAsync($"{Endpoint}/{id}?confirmImpact={confirmImpact.ToString().ToLowerInvariant()}");
        if (response.StatusCode == HttpStatusCode.Unauthorized) return Challenge();
        if (response.StatusCode == HttpStatusCode.Forbidden) return Forbid();
        if (response.StatusCode == HttpStatusCode.NotFound) return NotFound();
        if (!response.IsSuccessStatusCode)
        {
            var error = await ErrorAsync(response);
            if (response.StatusCode == HttpStatusCode.Conflict && !confirmImpact && error.StartsWith("Permanent deletion", StringComparison.Ordinal))
            {
                // Retrieve facility details before displaying confirmation.
                using var detailsResponse = await client.GetAsync($"{Endpoint}/{id}");

                if (detailsResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (detailsResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (detailsResponse.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!detailsResponse.IsSuccessStatusCode)
                {
                    TempData["FacilityError"] = await ErrorAsync(detailsResponse);
                    return RedirectToAction(nameof(Index));
                }

                var facility = await detailsResponse.Content
                    .ReadFromJsonAsync<FacilityAdminViewModel>();

                if (facility == null)
                    return NotFound();

                ViewBag.Warning = error;
                return View("ConfirmDelete", facility);
            }

            TempData["FacilityError"] = error;
            return RedirectToAction(nameof(Index));
        }

        TempData["FacilitySuccess"] = "Facility permanently deleted.";
        return RedirectToAction(nameof(Index));
    }
}
