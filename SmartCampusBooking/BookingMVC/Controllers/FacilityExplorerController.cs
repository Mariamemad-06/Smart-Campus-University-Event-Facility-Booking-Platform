using BookingMVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookingMVC.Controllers;

[Authorize(Roles = "Student,Faculty")]
public sealed class FacilityExplorerController(IHttpClientFactory factory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FacilityExplorerViewModel model, CancellationToken ct)
    {
        // Never use caller-supplied results or errors from the query string.
        model.Facilities = new();
        model.Error = null;
        model.SearchCompleted = false;
        var query = new Dictionary<string, string?>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) query[key] = value.Trim();
        }
        Add("Keyword", model.Keyword);
        Add("Type", model.Type);
        Add("Location", model.Location);
        Add("Capacity", model.Capacity?.ToString(CultureInfo.InvariantCulture));
        var hasDate = !string.IsNullOrWhiteSpace(model.Date);
        var hasStart = !string.IsNullOrWhiteSpace(model.StartTime);
        var hasEnd = !string.IsNullOrWhiteSpace(model.EndTime);
        DateOnly date = default;
        if (hasDate && !DateOnly.TryParseExact(model.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            ModelState.AddModelError(nameof(model.Date), "Enter a valid date.");
        if (hasStart || hasEnd)
        {
            if (!hasDate || !hasStart || !hasEnd)
                ModelState.AddModelError(string.Empty, "Select a date and both times to search a specific interval.");
            else if (!TimeOnly.TryParseExact(model.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                     !TimeOnly.TryParseExact(model.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                ModelState.AddModelError(string.Empty, "Enter valid start and end times.");
            else if (start >= end)
                ModelState.AddModelError(string.Empty, "End time must be after start time.");
            else
            {
                // Send the API's time-window contract, not Date alongside StartTime/EndTime.
                Add("StartTime", date.ToDateTime(start).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                Add("EndTime", date.ToDateTime(end).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
            }
        }
        else if (hasDate) Add("Date", model.Date);
        if (!ModelState.IsValid) return View(model);

        var token = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token)) return Challenge();
        using var client = factory.CreateClient("BookingApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var response = await client.GetAsync(QueryHelpers.AddQueryString("api/facilities/search", query), ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return Challenge();
            if (response.StatusCode == HttpStatusCode.Forbidden) return Forbid();
            if (!response.IsSuccessStatusCode)
            {
                model.Error = await ReadErrorAsync(response, ct);
                return View(model);
            }
            model.Facilities = await response.Content.ReadFromJsonAsync<List<FacilityExplorerItem>>(cancellationToken: ct) ?? new();
            model.SearchCompleted = true;
        }
        catch (HttpRequestException)
        {
            model.Error = "The facility service is unavailable. Please try again.";
        }
        catch (JsonException)
        {
            model.Error = "The facility service returned an unexpected response.";
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            model.Error = "The facility service did not respond in time. Please try again.";
        }
        return View(model);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject().Where(e => e.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(e => e.Value.EnumerateArray()).Where(v => v.ValueKind == JsonValueKind.String)
                        .Select(v => v.GetString()).Distinct();
                    var message = string.Join(" ", messages);
                    if (!string.IsNullOrWhiteSpace(message)) return message;
                }
                if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String) return m.GetString() ?? "Search failed.";
                if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String) return t.GetString() ?? "Search failed.";
            }
        }
        catch (JsonException) { }
        return $"Facility search failed (HTTP {(int)response.StatusCode}).";
    }
}
