using JiSaveSacco.API.Data;
using JiSaveSacco.API.DTOs;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

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

        // =========================
        // PUBLIC REGISTRATION
        // =========================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (existingUser != null)
                return BadRequest("Username already exists");

            // 1. Create User
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Member",
                Status = "Pending"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 2. Create Member Profile
            var member = new Member
            {
                UserId = user.UserId,
                MemberNo = dto.MemberNo,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                NationalId = dto.NationalId,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                Occupation = dto.Occupation,
                Status = "Pending"
            };

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Registration successful. Await admin approval.",
                memberId = member.MemberId
            });
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

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == user.UserId);

            var token = _jwtService.GenerateToken(user, member?.MemberId);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                MemberId = member?.MemberId
            });
        }
    }
}