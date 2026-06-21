using JiSaveSacco.API.Data;
using JiSaveSacco.API.DTOs;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;

namespace JiSaveSacco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [Authorize]
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("JWT is working. You are authenticated.");
        }

        // =========================
        // LOGIN
        // =========================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
                return Unauthorized("Invalid username or password");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash
            );

            if (!isPasswordValid)
                return Unauthorized("Invalid username or password");

            // Find linked member (if exists)
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == user.UserId);

            // Generate JWT with embedded identity
            var token = _jwtService.GenerateToken(user, member?.MemberId);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Role = user.Role
            });
        }
    }
}