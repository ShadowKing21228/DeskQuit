using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DeskQuit.Models;
using DeskQuit.Services.Logging;

namespace DeskQuit.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public ApiService()
    {
        _httpClient = new HttpClient();
    }

    public void UpdateBaseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        
        try 
        {
            if (!url.EndsWith("/"))
            {
                url += "/";
            }
            _httpClient.BaseAddress = new Uri(url);
            AppLogger.Info($"Updated BaseAddress to: {url}", nameof(ApiService));
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to update BaseAddress to {url}: {ex.Message}", nameof(ApiService));
        }
    }

    public void SetToken(string? token)
    {
        _token = token;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public async Task<(bool Success, string? Email, string? Token, string? ErrorMessage)> LoginAsync(string email, string password)
    {
        return await AuthenticateAsync("Auth/login", email, password);
    }

    public async Task<(bool Success, string? Email, string? Token, string? ErrorMessage)> RegisterAsync(string email, string password)
    {
        return await AuthenticateAsync("Auth/register", email, password);
    }

    private async Task<(bool Success, string? Email, string? Token, string? ErrorMessage)> AuthenticateAsync(string endpoint, string email, string password)
    {
        try
        {
            var payload = new { email, password };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    SetToken(result.Token);
                    return (true, result.Email, result.Token, null);
                }
            }

            return (false, null, null, $"Server returned {response.StatusCode}: {responseContent}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Auth error ({endpoint}): {ex.Message}", nameof(ApiService));
            return (false, null, null, ex.Message);
        }
    }

    public async Task<GlobalConfig?> GetUserConfigAsync()
    {
        if (!IsAuthenticated) return null;

        try
        {
            var response = await _httpClient.GetAsync("UserConfig");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GlobalConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            AppLogger.Warning($"Failed to get user config: {response.StatusCode}", nameof(ApiService));
            return null;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"GetUserConfig error: {ex.Message}", nameof(ApiService));
            return null;
        }
    }

    public async Task<bool> SyncConfigAsync(GlobalConfig config)
    {
        if (!IsAuthenticated) return false;

        try
        {
            var payload = new
            {
                afkThresholdMinutes = config.AfkThresholdMinutes,
                timerWidth = config.TimerWidth,
                timerHeight = config.TimerHeight,
                runOnStartup = config.RunOnStartup
            };
            
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("UserConfig", content);
            if (!response.IsSuccessStatusCode)
            {
                AppLogger.Warning($"Failed to sync config: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}", nameof(ApiService));
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"SyncConfig error: {ex.Message}", nameof(ApiService));
            return false;
        }
    }

    public async Task<List<ReminderConfig>?> GetUserRemindersAsync()
    {
        if (!IsAuthenticated) return null;
        try
        {
            var response = await _httpClient.GetAsync("UserReminders");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ReminderConfig>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            AppLogger.Warning($"Failed to get user reminders: {response.StatusCode}", nameof(ApiService));
            return null;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"GetUserReminders error: {ex.Message}", nameof(ApiService));
            return null;
        }
    }

    public async Task<bool> SyncRemindersAsync(List<ReminderConfig> reminders)
    {
        if (!IsAuthenticated) return false;

        try
        {
            var json = JsonSerializer.Serialize(reminders, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("UserReminders", content);
            if (!response.IsSuccessStatusCode)
            {
                AppLogger.Warning($"Failed to sync reminders: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}", nameof(ApiService));
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"SyncReminders error: {ex.Message}", nameof(ApiService));
            return false;
        }
    }

    public async Task<bool> SendDailyStatsAsync(string dateStr, long activeSeconds, long afkSeconds, int notifsTotal, int notifsCustom)
    {
        if (!IsAuthenticated) return false;

        try
        {
            var payload = new[]
            {
                new
                {
                    statDate = dateStr,
                    activeSeconds = activeSeconds,
                    afkSeconds = afkSeconds,
                    notificationsTotal = notifsTotal,
                    notificationsCustom = notifsCustom
                }
            };
            
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("UserStats/daily", content);
            if (!response.IsSuccessStatusCode)
            {
                AppLogger.Warning($"Failed to send daily stats: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}", nameof(ApiService));
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"SendDailyStats error: {ex.Message}", nameof(ApiService));
            return false;
        }
    }

    public async Task<AllTimeStatsResponse?> GetAllTimeStatsAsync()
    {
        AppLogger.Info($"GetAllTimeStatsAsync called. IsAuthenticated={IsAuthenticated}, BaseAddress={_httpClient.BaseAddress}", nameof(ApiService));
        
        if (!IsAuthenticated)
        {
            AppLogger.Warning("GetAllTimeStatsAsync called but not authenticated", nameof(ApiService));
            return null;
        }

        try
        {
            AppLogger.Info("GetAllTimeStatsAsync: starting request to userstats/all-time", nameof(ApiService));
            AppLogger.Info($"Authorization header present: {_httpClient.DefaultRequestHeaders.Authorization != null}", nameof(ApiService));
            
            var response = await _httpClient.GetAsync("userstats/all-time");
            AppLogger.Info($"GetAllTimeStats response code: {response.StatusCode}", nameof(ApiService));
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                AppLogger.Info($"GetAllTimeStats response body: {json}", nameof(ApiService));
                
                var result = JsonSerializer.Deserialize<AllTimeStatsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null)
                {
                    AppLogger.Info($"Parsed all-time stats: active={result.ActiveSeconds}s, afk={result.AfkSeconds}s, days={result.DaysTracked}, notifs={result.NotificationsTotal}", nameof(ApiService));
                    return result;
                }
                else
                {
                    AppLogger.Warning("Failed to parse AllTimeStatsResponse (null result)", nameof(ApiService));
                }
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                AppLogger.Warning($"Failed to get all-time stats: {response.StatusCode} - {errorBody}", nameof(ApiService));
            }
            return null;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"GetAllTimeStats error: {ex.Message}", nameof(ApiService));
            return null;
        }
    }

    public async Task<(long activeSeconds, long afkSeconds, int notifsTotal, int notifsCustom)?> GetTotalStatsAsync()
    {
        var stats = await GetAllTimeStatsAsync();
        if (stats != null)
        {
            return (stats.ActiveSeconds, stats.AfkSeconds, stats.NotificationsTotal, stats.NotificationsCustom);
        }
        return null;
    }

    public class AllTimeStatsResponse
    {
        public long ActiveSeconds { get; set; }
        public long AfkSeconds { get; set; }
        public long TotalSeconds { get; set; }
        public int DaysTracked { get; set; }
        public int NotificationsTotal { get; set; }
        public int NotificationsCustom { get; set; }
        public int ReminderNotificationsTotal { get; set; }
        public int DistinctReminders { get; set; }
        public string? FirstStatDate { get; set; }
        public string? LastStatDate { get; set; }
    }

    private class AuthResponse
    {
        public string? Token { get; set; }
        public string? Email { get; set; }
    }
}
