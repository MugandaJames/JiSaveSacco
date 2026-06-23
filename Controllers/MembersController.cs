using JiSaveSacco.API.Data;
using JiSaveSacco.API.DTOs.Members;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IdentityService _identity;
        private readonly AuditService _audit;

        public MembersController(
            AppDbContext context,
            IdentityService identity,
            AuditService audit)
        {
            _context = context;
            _identity = identity;
            _audit = audit;
        }

        // =========================
        // GET ALL MEMBERS
        // =========================
        [Authorize(Roles = "Admin,Staff")]
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
        // GET MEMBER BY ID
        // =========================
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMember(int id)
        {
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member is null)
                return NotFound("Member not found");

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

        // =========================
        // MY PROFILE (SECURE)
        // =========================
        [Authorize(Roles = "Member")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var memberId = _identity.GetMemberId();

            if (memberId is null)
                return Unauthorized("Member identity missing");

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member is null)
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

        // =========================
        // PENDING MEMBERS
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingMembers()
        {
            var pending = await _context.Members
                .Where(m => m.Status == "Pending")
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

            return Ok(pending);
        }

        // =========================
        // APPROVE MEMBER
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveMember(int id)
        {
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member is null)
                return NotFound("Member not found");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == member.UserId);

            if (user is null)
                return NotFound("User account missing");

            member.Status = "Active";
            user.Status = "Active";

            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                "Approved member",
                "Members",
                member.MemberId
            );

            return Ok("Member approved successfully");
        }

        // =========================
        // REJECT MEMBER
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectMember(int id)
        {
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member is null)
                return NotFound("Member not found");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == member.UserId);

            member.Status = "Rejected";

            if (user != null)
                user.Status = "Inactive";

            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                "Rejected member",
                "Members",
                member.MemberId
            );

            return Ok("Member rejected successfully");
        }

        // =========================
        // DEACTIVATE MEMBER
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateMember(int id)
        {
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member is null)
                return NotFound("Member not found");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == member.UserId);

            member.Status = "Inactive";

            if (user != null)
                user.Status = "Inactive";

            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                "Deactivated member",
                "Members",
                member.MemberId
            );

            return Ok("Member deactivated successfully");
        }
    }
}