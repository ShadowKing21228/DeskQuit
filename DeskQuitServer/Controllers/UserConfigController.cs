using System.Security.Claims;
using DeskQuitServer.Data;
using DeskQuitServer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskQuitServer.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserConfigController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserConfigController(ApplicationDbContext context)
    {
        _context = context;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }

    [HttpGet]
    public async Task<ActionResult<UserConfigDto>> GetConfig()
    {
        var userId = GetUserId();
        var config = await _context.UserConfigs.FindAsync(userId);

        if (config == null)
        {
            return NotFound("Configuration not found.");
        }

        return new UserConfigDto
        {
            AfkThresholdMinutes = config.AfkThresholdMinutes,
            TimerWidth = config.TimerWidth,
            TimerHeight = config.TimerHeight,
            RunOnStartup = config.RunOnStartup
        };
    }

    [HttpPut]
    public async Task<IActionResult> UpdateConfig(UserConfigDto configDto)
    {
        var userId = GetUserId();
        var config = await _context.UserConfigs.FindAsync(userId);

        if (config == null)
        {
            return NotFound("Configuration not found.");
        }

        config.AfkThresholdMinutes = configDto.AfkThresholdMinutes!.Value;
        config.TimerWidth = configDto.TimerWidth!.Value;
        config.TimerHeight = configDto.TimerHeight!.Value;
        config.RunOnStartup = configDto.RunOnStartup!.Value;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
