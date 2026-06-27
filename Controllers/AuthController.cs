using JiSaveSacco.API.Data;
using JiSaveSacco.API.DTOs;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IdentityService _identity; // Injected to cleanly pull user identity context from claims

        public AuthController(
            AppDbContext context,
            JwtService jwtService,
            IdentityService identity)
        {
            _context = context;
            _jwtService = jwtService;
            _identity = identity;
        }

        // =========================
        // PUBLIC REGISTRATION
        // =========================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Check username
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username already exists"
                });
            }

            // Check National ID
            var existingNationalId = await _context.Members
                .FirstOrDefaultAsync(m => m.NationalId == dto.NationalId);

            if (existingNationalId != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "National ID already registered"
                });
            }

            // Check Email
            var existingEmail = await _context.Members
                .FirstOrDefaultAsync(m => m.Email == dto.Email);

            if (existingEmail != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email already registered"
                });
            }

            // Generate Member Number
            var lastMember = await _context.Members
                .OrderByDescending(m => m.MemberId)
                .FirstOrDefaultAsync();

            string memberNo = lastMember == null
                ? "M001"
                : $"M{lastMember.MemberId + 1:D3}";

            // Create User
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Member",
                Status = "Pending"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create Member
            var member = new Member
            {
                UserId = user.UserId,
                MemberNo = memberNo,
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
                success = true,
                message = "Registration successful. Await admin approval.",
                memberId = member.MemberId,
                memberNo = member.MemberNo
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
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password"
                });
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password"
                });
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == user.UserId);

            var token = _jwtService.GenerateToken(user, member?.MemberId);

            return Ok(new
            {
                success = true,
                message = "Login successful.",
                token,
                username = user.Username,
                role = user.Role,
                memberId = member?.MemberId
            });
        }

        // =========================================================
        // SECURE: CHANGE PASSWORD (MEMBERS, ADMINS, & STAFF)
        // =========================================================
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = _identity.GetUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "User context authentication missing." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound(new { success = false, message = "User account could not be found." });

            // Verify current operational password validity
            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!isOldPasswordValid)
            {
                return BadRequest(new { success = false, message = "Your current password entry is incorrect." });
            }

            // Write out fresh password hash metrics
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Password updated successfully!" });
        }
    }
}