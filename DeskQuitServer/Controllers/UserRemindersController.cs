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
public class UserRemindersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserRemindersController(ApplicationDbContext context)
    {
        _context = context;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserReminderDto>>> GetReminders()
    {
        var userId = GetUserId();
        var reminders = await _context.UserReminders
            .Where(r => r.UserId == userId)
            .Select(r => new UserReminderDto
            {
                Id = r.Id,
                IsEnabled = r.IsEnabled,
                IntervalInMinutes = r.IntervalInMinutes,
                NotificationStyle = r.NotificationStyle,
                IsCustom = r.IsCustom,
                CustomTitle = r.CustomTitle,
                CustomDescription = r.CustomDescription
            })
            .ToListAsync();

        return Ok(reminders);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateReminders([FromBody] List<UserReminderDto> reminderDtos)
    {
        var userId = GetUserId();
        var existingReminders = await _context.UserReminders
            .Where(r => r.UserId == userId)
            .ToListAsync();

        // Simple approach: remove all existing and add the new ones.
        // More complex logic could be used to update/add/delete individually.
        _context.UserReminders.RemoveRange(existingReminders);

        var newReminders = reminderDtos.Select(dto => new UserReminder
        {
            Id = dto.Id,
            UserId = userId,
            IsEnabled = dto.IsEnabled!.Value,
            IntervalInMinutes = dto.IntervalInMinutes!.Value,
            NotificationStyle = dto.NotificationStyle!.Value,
            IsCustom = dto.IsCustom!.Value,
            CustomTitle = dto.CustomTitle,
            CustomDescription = dto.CustomDescription
        });

        await _context.UserReminders.AddRangeAsync(newReminders);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
