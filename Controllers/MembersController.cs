using JiSaveSacco.API.Data;
using JiSaveSacco.API.DTOs.Members;
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
    public class MembersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IdentityService _identity;

        public MembersController(AppDbContext context, IdentityService identity)
        {
            _context = context;
            _identity = identity;
        }

        // =========================
        // CREATE MEMBER (ADMIN ONLY)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateMember(CreateMemberDto dto)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (existingUser != null)
                return BadRequest("Username already exists");

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Member",
                Status = "Active"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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
                Status = "Active"
            };

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Member created successfully",
                memberId = member.MemberId
            });
        }

        // =========================
        // GET ALL MEMBERS (ADMIN)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetMembers()
        {
            var members = await _context.Members
                .Select(m => new MemberResponseDto
                {
                    MemberId = m.MemberId,
                    MemberNo = m.MemberNo,
                    FullName = m.FirstName + " " + m.LastName,
                    Phone = m.Phone,
                    Email = m.Email,
                    Status = m.Status
                })
                .ToListAsync();

            return Ok(members);
        }

        // =========================
        // GET MY PROFILE (JWT SAFE)
        // =========================
        [Authorize(Roles = "Member")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized("Member not linked to account");

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null)
                return NotFound("Member profile not found");

            return Ok(new MemberResponseDto
            {
                MemberId = member.MemberId,
                MemberNo = member.MemberNo,
                FullName = member.FirstName + " " + member.LastName,
                Phone = member.Phone,
                Email = member.Email,
                Status = member.Status
            });
        }
    }
}