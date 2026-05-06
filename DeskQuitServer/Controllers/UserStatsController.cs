using System.Security.Claims;
using DeskQuitServer.Data;
using DeskQuitServer.DTOs;
using DeskQuitServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskQuitServer.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserStatsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserStatsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }

    // --- Daily Stats ---

    [HttpGet("daily")]
    public async Task<ActionResult<IEnumerable<UserDailyStatsDto>>> GetDailyStats([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
    {
        var userId = GetUserId();
        var query = _context.UserDailyStats.Where(s => s.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.StatDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.StatDate <= endDate.Value);
        }

        var stats = await query
            .OrderByDescending(s => s.StatDate)
            .Select(s => new UserDailyStatsDto
            {
                StatDate = s.StatDate,
                ActiveSeconds = s.ActiveSeconds,
                AfkSeconds = s.AfkSeconds,
                NotificationsTotal = s.NotificationsTotal,
                NotificationsCustom = s.NotificationsCustom
            })
            .ToListAsync();

        return Ok(stats);
    }

    [HttpPost("daily")]
    public async Task<IActionResult> UpdateDailyStats([FromBody] List<UserDailyStatsDto> statsDtos)
    {
        var userId = GetUserId();

        var aggregatedByDate = statsDtos
            .GroupBy(dto => dto.StatDate!.Value)
            .Select(group => new
            {
                StatDate = group.Key,
                ActiveSeconds = group.Sum(x => x.ActiveSeconds!.Value),
                AfkSeconds = group.Sum(x => x.AfkSeconds!.Value),
                NotificationsTotal = group.Sum(x => x.NotificationsTotal!.Value),
                NotificationsCustom = group.Sum(x => x.NotificationsCustom!.Value)
            });

        foreach (var item in aggregatedByDate)
        {
            var existingStat = await _context.UserDailyStats
                .FirstOrDefaultAsync(s => s.UserId == userId && s.StatDate == item.StatDate);

            if (existingStat != null)
            {
                existingStat.ActiveSeconds += item.ActiveSeconds;
                existingStat.AfkSeconds += item.AfkSeconds;
                existingStat.NotificationsTotal += item.NotificationsTotal;
                existingStat.NotificationsCustom += item.NotificationsCustom;
            }
            else
            {
                _context.UserDailyStats.Add(new UserDailyStats
                {
                    UserId = userId,
                    StatDate = item.StatDate,
                    ActiveSeconds = item.ActiveSeconds,
                    AfkSeconds = item.AfkSeconds,
                    NotificationsTotal = item.NotificationsTotal,
                    NotificationsCustom = item.NotificationsCustom
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("all-time")]
    public async Task<ActionResult<UserAllTimeStatsDto>> GetAllTimeStats()
    {
        var userId = GetUserId();

        var dailyStatsQuery = _context.UserDailyStats.Where(s => s.UserId == userId);
        var reminderStatsQuery = _context.UserDailyReminderStats.Where(s => s.UserId == userId);

        var activeSeconds = await dailyStatsQuery.SumAsync(s => (long?)s.ActiveSeconds) ?? 0;
        var afkSeconds = await dailyStatsQuery.SumAsync(s => (long?)s.AfkSeconds) ?? 0;
        var notificationsTotal = await dailyStatsQuery.SumAsync(s => (int?)s.NotificationsTotal) ?? 0;
        var notificationsCustom = await dailyStatsQuery.SumAsync(s => (int?)s.NotificationsCustom) ?? 0;
        var daysTracked = await dailyStatsQuery.CountAsync();
        var firstStatDate = await dailyStatsQuery.MinAsync(s => (DateOnly?)s.StatDate);
        var lastStatDate = await dailyStatsQuery.MaxAsync(s => (DateOnly?)s.StatDate);
        var reminderNotificationsTotal = await reminderStatsQuery.SumAsync(s => (int?)s.NotificationsCount) ?? 0;
        var distinctReminders = await reminderStatsQuery.Select(s => s.ReminderId).Distinct().CountAsync();

        return Ok(new UserAllTimeStatsDto
        {
            ActiveSeconds = activeSeconds,
            AfkSeconds = afkSeconds,
            TotalSeconds = activeSeconds + afkSeconds,
            DaysTracked = daysTracked,
            NotificationsTotal = notificationsTotal,
            NotificationsCustom = notificationsCustom,
            ReminderNotificationsTotal = reminderNotificationsTotal,
            DistinctReminders = distinctReminders,
            FirstStatDate = firstStatDate,
            LastStatDate = lastStatDate
        });
    }


    // --- Daily Reminder Stats ---

    [HttpGet("reminders")]
    public async Task<ActionResult<IEnumerable<UserDailyReminderStatsDto>>> GetDailyReminderStats([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
    {
        var userId = GetUserId();
        var query = _context.UserDailyReminderStats.Where(s => s.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.StatDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.StatDate <= endDate.Value);
        }

        var stats = await query
            .OrderByDescending(s => s.StatDate)
            .Select(s => new UserDailyReminderStatsDto
            {
                StatDate = s.StatDate,
                ReminderId = s.ReminderId,
                NotificationsCount = s.NotificationsCount
            })
            .ToListAsync();

        return Ok(stats);
    }

    [HttpPost("reminders")]
    public async Task<IActionResult> UpdateDailyReminderStats([FromBody] List<UserDailyReminderStatsDto> statsDtos)
    {
        var userId = GetUserId();

        var aggregatedByDateAndReminder = statsDtos
            .GroupBy(dto => new { StatDate = dto.StatDate!.Value, dto.ReminderId })
            .Select(group => new
            {
                group.Key.StatDate,
                group.Key.ReminderId,
                NotificationsCount = group.Sum(x => x.NotificationsCount!.Value)
            });

        foreach (var item in aggregatedByDateAndReminder)
        {
            var existingStat = await _context.UserDailyReminderStats
                .FirstOrDefaultAsync(s => s.UserId == userId && s.StatDate == item.StatDate && s.ReminderId == item.ReminderId);

            if (existingStat != null)
            {
                existingStat.NotificationsCount += item.NotificationsCount;
            }
            else
            {
                _context.UserDailyReminderStats.Add(new UserDailyReminderStats
                {
                    UserId = userId,
                    StatDate = item.StatDate,
                    ReminderId = item.ReminderId,
                    NotificationsCount = item.NotificationsCount
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}
