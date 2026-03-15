using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;

namespace CalendarManager.API.Services.Implementations;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly HttpClient _httpClient;
    private readonly IOAuthService _oauthService;
    private const string BaseUrl = "https://www.googleapis.com/calendar/v3";

    public GoogleCalendarService(HttpClient httpClient, IOAuthService oauthService)
    {
        _httpClient = httpClient;
        _oauthService = oauthService;
    }

    public async Task<List<CalendarEventDto>> GetEventsAsync(Guid userId, DateTime start, DateTime end)
    {
        try
        {
            // Format dates as ISO 8601 UTC 
            var timeMin = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var timeMax = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            // Build request URL with query parameters
            var url = $"{BaseUrl}/calendars/primary/events" +
                     $"?timeMin={Uri.EscapeDataString(timeMin)}" +
                     $"&timeMax={Uri.EscapeDataString(timeMax)}" +
                     $"&singleEvents=true" +
                     $"&orderBy=startTime" +
                     $"&maxResults=250";
            
            // Create authorized HTTP request
            var request = await CreateAuthorizedRequestAsync(userId, HttpMethod.Get, url);
            
            // Make HTTP request
            var response = await _httpClient.SendAsync(request);
            
            // Handle HTTP errors
            if (!response.IsSuccessStatusCode)
            {
                await HandleHttpError(response, "Failed to get calendar events");
            }
            
            // Parse JSON response
            var jsonContent = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(jsonContent);
            var root = jsonDocument.RootElement;
            
            var events = new List<CalendarEventDto>();
            
            // Extract events from response
            if (root.TryGetProperty("items", out var itemsElement))
            {
                foreach (var eventElement in itemsElement.EnumerateArray())
                {
                    try
                    {
                        var eventDto = ConvertGoogleEventToDto(eventElement);
                        events.Add(eventDto);
                    }
                    catch (Exception ex)
                    {
                        // Log individual event parsing error but continue
                        Console.WriteLine($"Error parsing event: {ex.Message}");
                        continue;
                    }
                }
            }
            
            return events;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not UnauthorizedAccessException)
        {
            // Wrap other exceptions
            throw new InvalidOperationException($"Failed to retrieve calendar events: {ex.Message}", ex);
        }
    }
    
    private async Task HandleHttpError(HttpResponseMessage response, string operation)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        
        var errorMessage = response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Authentication failed. Please re-authenticate with Google.",
            System.Net.HttpStatusCode.Forbidden => "Insufficient permissions. Please ensure calendar access is granted.",
            System.Net.HttpStatusCode.NotFound => "Calendar not found.",
            System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again later.",
            _ => $"{operation} failed with status {response.StatusCode}"
        };
        
        throw new HttpRequestException($"{errorMessage} Details: {errorContent}", null, response.StatusCode);
    }

    public async Task<CalendarEventDto> CreateEventAsync(Guid userId, CreateEventDto request)
    {
        try
        {
            // Build Google Calendar event object
            var googleEvent = ConvertDtoToGoogleEvent(request);
            
            // Serialize to JSON
            var jsonContent = JsonSerializer.Serialize(googleEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            // Build request URL
            var url = $"{BaseUrl}/calendars/primary/events";
            
            // Create authorized HTTP request
            var httpRequest = await CreateAuthorizedRequestAsync(userId, HttpMethod.Post, url);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Make HTTP request
            var response = await _httpClient.SendAsync(httpRequest);
            
            // Handle HTTP errors
            if (!response.IsSuccessStatusCode)
            {
                await HandleHttpError(response, "Failed to create calendar event");
            }
            
            // Parse response and convert to DTO
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseContent);
            var createdEvent = ConvertGoogleEventToDto(responseJson.RootElement);
            
            return createdEvent;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Failed to create calendar event: {ex.Message}", ex);
        }
    }

    public async Task<CalendarEventDto> UpdateEventAsync(Guid userId, string eventId, UpdateEventDto request)
    {
        try
        {
            // Build update object with only provided fields
            var updateObject = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(request.Title))
                updateObject["summary"] = request.Title;
            
            if (request.Description != null)
                updateObject["description"] = request.Description;
            
            if (!string.IsNullOrEmpty(request.Location))
                updateObject["location"] = request.Location;
            
            if (request.Start.HasValue)
            {
                updateObject["start"] = new Dictionary<string, object>
                {
                    ["dateTime"] = request.Start.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ["timeZone"] = "UTC"
                };
            }
            
            if (request.End.HasValue)
            {
                updateObject["end"] = new Dictionary<string, object>
                {
                    ["dateTime"] = request.End.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ["timeZone"] = "UTC"
                };
            }
            
            // Serialize to JSON
            var jsonContent = JsonSerializer.Serialize(updateObject);
            
            // Build request URL
            var url = $"{BaseUrl}/calendars/primary/events/{Uri.EscapeDataString(eventId)}";
            
            // Create authorized HTTP request
            var httpRequest = await CreateAuthorizedRequestAsync(userId, HttpMethod.Put, url);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Make HTTP request
            var response = await _httpClient.SendAsync(httpRequest);
            
            // Handle HTTP errors
            if (!response.IsSuccessStatusCode)
            {
                await HandleHttpError(response, "Failed to update calendar event");
            }
            
            // Parse response and convert to DTO
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseContent);
            var updatedEvent = ConvertGoogleEventToDto(responseJson.RootElement);
            
            return updatedEvent;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Failed to update calendar event: {ex.Message}", ex);
        }
    }

    public async Task DeleteEventAsync(Guid userId, string eventId)
    {
        try
        {
            // Build request URL
            var url = $"{BaseUrl}/calendars/primary/events/{Uri.EscapeDataString(eventId)}";
            
            // Create authorized HTTP request
            var request = await CreateAuthorizedRequestAsync(userId, HttpMethod.Delete, url);
            
            // Make HTTP request
            var response = await _httpClient.SendAsync(request);
            
            // Handle HTTP errors (404 is acceptable for delete operations)
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                await HandleHttpError(response, "Failed to delete calendar event");
            }
        }
        catch (Exception ex) when (ex is not HttpRequestException and not UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Failed to delete calendar event: {ex.Message}", ex);
        }
    }

    public async Task<FreeBusyDto> GetFreeBusyAsync(Guid userId, string email, DateTime start, DateTime end)
    {
        try
        {
            // Build free/busy request
            var freeBusyRequest = new Dictionary<string, object>
            {
                ["timeMin"] = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["timeMax"] = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["items"] = new[]
                {
                    new Dictionary<string, object> { ["id"] = email }
                }
            };
            
            // Serialize to JSON
            var jsonContent = JsonSerializer.Serialize(freeBusyRequest);
            
            // Build request URL
            var url = $"{BaseUrl}/freeBusy";
            
            // Create authorized HTTP request
            var httpRequest = await CreateAuthorizedRequestAsync(userId, HttpMethod.Post, url);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Make HTTP request
            var response = await _httpClient.SendAsync(httpRequest);
            
            // Handle HTTP errors
            if (!response.IsSuccessStatusCode)
            {
                await HandleHttpError(response, "Failed to get free/busy information");
            }
            
            // Parse response
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseContent);
            var root = responseJson.RootElement;
            
            var busyPeriods = new List<BusyPeriod>();
            
            // Extract busy periods
            if (root.TryGetProperty("calendars", out var calendarsElement) &&
                calendarsElement.TryGetProperty(email, out var calendarElement) &&
                calendarElement.TryGetProperty("busy", out var busyElement))
            {
                foreach (var busyPeriod in busyElement.EnumerateArray())
                {
                    var busyStart = DateTime.Parse(busyPeriod.GetProperty("start").GetString()!).ToUniversalTime();
                    var busyEnd = DateTime.Parse(busyPeriod.GetProperty("end").GetString()!).ToUniversalTime();
                    busyPeriods.Add(new BusyPeriod 
                    { 
                        Start = busyStart, 
                        End = busyEnd 
                    });
                }
            }
            
            return new FreeBusyDto
            {
                Email = email ?? string.Empty,
                BusyPeriods = busyPeriods
            };
        }
        catch (Exception ex) when (ex is not HttpRequestException and not UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Failed to get free/busy information: {ex.Message}", ex);
        }
    }

    public async Task<bool> ValidateTokenAsync(Guid userId)
    {
        try
        {
            // Try a simple API call to validate token
            var url = $"{BaseUrl}/users/me/calendarList/primary";
            var request = await CreateAuthorizedRequestAsync(userId, HttpMethod.Get, url);
            
            var response = await _httpClient.SendAsync(request);
            
            // Return true if successful, false for auth errors
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Don't throw exceptions, just return false for any error
            return false;
        }
    }

    // HELPER METHODS TO IMPLEMENT:

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(Guid userId, HttpMethod method, string url)
    {
        // Get valid access token (will refresh if expired)
        var accessToken = await _oauthService.GetValidAccessTokenAsync(userId);
        
        // Create HTTP request
        var request = new HttpRequestMessage(method, url);
        
        // Set Authorization header
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        // Set Accept header
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        return request;
    }

    private CalendarEventDto ConvertGoogleEventToDto(JsonElement googleEvent)
    {
        // Extract basic event properties
        var id = googleEvent.GetProperty("id").GetString() ?? "";
        var summary = googleEvent.TryGetProperty("summary", out var summaryElement) ? summaryElement.GetString() : "";
        var description = googleEvent.TryGetProperty("description", out var descElement) ? descElement.GetString() : null;
        var location = googleEvent.TryGetProperty("location", out var locElement) ? locElement.GetString() : null;
        
        // Parse start and end times
        var (startDateTime, endDateTime) = ParseEventDateTime(googleEvent);
        
        // Parse attendees
        var attendees = new List<string>();
        if (googleEvent.TryGetProperty("attendees", out var attendeesElement))
        {
            foreach (var attendee in attendeesElement.EnumerateArray())
            {
                if (attendee.TryGetProperty("email", out var emailElement))
                {
                    var email = emailElement.GetString();
                    if (!string.IsNullOrEmpty(email))
                    {
                        attendees.Add(email);
                    }
                }
            }
        }
        
        return new CalendarEventDto
        {
            Id = id,
            Title = summary ?? "Untitled Event",
            Description = description,
            Location = location,
            Start = startDateTime,
            End = endDateTime,
            Attendees = attendees.Any() ? attendees : null
        };
    }
    
    private (DateTime start, DateTime end) ParseEventDateTime(JsonElement googleEvent)
    {
        var startElement = googleEvent.GetProperty("start");
        var endElement = googleEvent.GetProperty("end");
        
        // Try dateTime first (timed events), then date (all-day events)
        DateTime startDateTime;
        DateTime endDateTime;
        
        if (startElement.TryGetProperty("dateTime", out var startDateTimeElement))
        {
            startDateTime = DateTime.Parse(startDateTimeElement.GetString()!).ToUniversalTime();
        }
        else if (startElement.TryGetProperty("date", out var startDateElement))
        {
            // All-day event - parse as date only
            startDateTime = DateTime.Parse(startDateElement.GetString()!).ToUniversalTime();
        }
        else
        {
            startDateTime = DateTime.UtcNow; // Fallback
        }
        
        if (endElement.TryGetProperty("dateTime", out var endDateTimeElement))
        {
            endDateTime = DateTime.Parse(endDateTimeElement.GetString()!).ToUniversalTime();
        }
        else if (endElement.TryGetProperty("date", out var endDateElement))
        {
            // All-day event - parse as date only
            endDateTime = DateTime.Parse(endDateElement.GetString()!).ToUniversalTime();
        }
        else
        {
            endDateTime = startDateTime.AddHours(1); // Fallback to 1 hour
        }
        
        return (startDateTime, endDateTime);
    }

    private object ConvertDtoToGoogleEvent(CreateEventDto dto)
    {
        var googleEvent = new Dictionary<string, object>
        {
            ["summary"] = dto.Title ?? "Untitled Event",
            ["start"] = new Dictionary<string, object>
            {
                ["dateTime"] = dto.Start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["timeZone"] = "UTC"
            },
            ["end"] = new Dictionary<string, object>
            {
                ["dateTime"] = dto.End.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["timeZone"] = "UTC"
            }
        };
        
        // Add optional fields
        if (!string.IsNullOrEmpty(dto.Description))
        {
            googleEvent["description"] = dto.Description;
        }
        
        if (!string.IsNullOrEmpty(dto.Location))
        {
            googleEvent["location"] = dto.Location;
        }
        
        // Add attendees if provided
        if (dto.Attendees != null && dto.Attendees.Any())
        {
            googleEvent["attendees"] = dto.Attendees.Select(email => new Dictionary<string, object>
            {
                ["email"] = email
            }).ToList();
        }
        
        // Add default reminders
        googleEvent["reminders"] = new Dictionary<string, object>
        {
            ["useDefault"] = true
        };
        
        return googleEvent;
    }
}

// NOTES:
// - Google Calendar API v3 documentation: https://developers.google.com/calendar/api/v3/reference
// - Always use UTC times for API calls
// - Handle rate limiting (exponential backoff)
// - Different date formats for all-day vs timed events
// - Attendees require valid email addresses
// - Some operations require specific OAuth scopes
// - Handle timezone conversions properly