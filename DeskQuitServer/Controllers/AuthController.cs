using DeskQuitServer.Data;
using DeskQuitServer.DTOs;
using DeskQuitServer.Models;
using DeskQuitServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskQuitServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(
        ApplicationDbContext context,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email.ToLower()))
        {
            return BadRequest("Email is already taken.");
        }

        var (hash, salt) = _passwordHasher.HashPassword(registerDto.Password);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email.ToLower(),
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        
        // Create default config for the new user
        var defaultConfig = new UserConfig
        {
            UserId = user.Id,
            AfkThresholdMinutes = 15,
            TimerWidth = 200,
            TimerHeight = 100,
            RunOnStartup = false
        };
        _context.UserConfigs.Add(defaultConfig);

        await _context.SaveChangesAsync();

        var token = _tokenService.CreateToken(user);

        return new AuthResponseDto
        {
            Email = user.Email,
            Token = token
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == loginDto.Email.ToLower());

        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        if (!_passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized("Invalid email or password.");
        }

        var token = _tokenService.CreateToken(user);

        return new AuthResponseDto
        {
            Email = user.Email,
            Token = token
        };
    }
}
