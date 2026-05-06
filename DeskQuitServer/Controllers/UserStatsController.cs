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

        foreach (var dto in statsDtos)
        {
            var existingStat = await _context.UserDailyStats
                .FirstOrDefaultAsync(s => s.UserId == userId && s.StatDate == dto.StatDate!.Value);

            if (existingStat != null)
            {
                existingStat.ActiveSeconds = dto.ActiveSeconds!.Value;
                existingStat.AfkSeconds = dto.AfkSeconds!.Value;
                existingStat.NotificationsTotal = dto.NotificationsTotal!.Value;
                existingStat.NotificationsCustom = dto.NotificationsCustom!.Value;
            }
            else
            {
                _context.UserDailyStats.Add(new UserDailyStats
                {
                    UserId = userId,
                    StatDate = dto.StatDate!.Value,
                    ActiveSeconds = dto.ActiveSeconds!.Value,
                    AfkSeconds = dto.AfkSeconds!.Value,
                    NotificationsTotal = dto.NotificationsTotal!.Value,
                    NotificationsCustom = dto.NotificationsCustom!.Value
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
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

        foreach (var dto in statsDtos)
        {
            var existingStat = await _context.UserDailyReminderStats
                .FirstOrDefaultAsync(s => s.UserId == userId && s.StatDate == dto.StatDate!.Value && s.ReminderId == dto.ReminderId);

            if (existingStat != null)
            {
                existingStat.NotificationsCount = dto.NotificationsCount!.Value;
            }
            else
            {
                _context.UserDailyReminderStats.Add(new UserDailyReminderStats
                {
                    UserId = userId,
                    StatDate = dto.StatDate!.Value,
                    ReminderId = dto.ReminderId,
                    NotificationsCount = dto.NotificationsCount!.Value
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}
