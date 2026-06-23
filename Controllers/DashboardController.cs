using JiSaveSacco.API.Data;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IdentityService _identity;

        public DashboardController(AppDbContext context, IdentityService identity)
        {
            _context = context;
            _identity = identity;
        }

        // =========================
        // ADMIN DASHBOARD
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var totalMembers = await _context.Members.CountAsync();

            var activeLoans = await _context.Loans
                .CountAsync(l => l.Status == "Approved");

            var pendingLoans = await _context.Loans
                .CountAsync(l => l.Status == "Pending");

            var totalSavings = await _context.SavingsTransactions
                .Select(s => (decimal?)s.Amount)
                .SumAsync() ?? 0;

            var loanExposure = await _context.Loans
                .Where(l => l.Status == "Approved")
                .Select(l => (decimal?)l.OutstandingBalance)
                .SumAsync() ?? 0;

            var recentLoans = await _context.Loans
                .OrderByDescending(l => l.ApplicationDate)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                totalMembers,
                activeLoans,
                pendingLoans,
                totalSavings,
                loanExposure,
                recentLoans
            });
        }

        // =========================
        // MEMBER DASHBOARD (JWT SECURE)
        // =========================
        [Authorize(Roles = "Member")]
        [HttpGet("member")]
        public async Task<IActionResult> MemberDashboard()
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized("Member not linked to this account");

            var savings = await _context.SavingsTransactions
                .Where(s => s.MemberId == memberId)
                .Select(s => (decimal?)s.Amount)
                .SumAsync() ?? 0;

            var loans = await _context.Loans
                .Where(l => l.MemberId == memberId)
                .CountAsync();

            var loanBalance = await _context.Loans
                .Where(l => l.MemberId == memberId && l.Status == "Approved")
                .Select(l => (decimal?)l.OutstandingBalance)
                .SumAsync() ?? 0;

            var notifications = await _context.Notifications
                .CountAsync(n => n.MemberId == memberId && !n.IsRead);

            return Ok(new
            {
                savings,
                loans,
                loanBalance,
                notifications
            });
        }
    }
}