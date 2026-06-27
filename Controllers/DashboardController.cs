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

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            // ================= KPIs =================
            var totalMembers = await _context.Members.CountAsync();

            var activeLoans = await _context.Loans.CountAsync(l => l.Status == "Approved");

            var pendingLoans = await _context.Loans.CountAsync(l => l.Status == "Pending");

            var totalSavings = await _context.SavingsTransactions
                .SumAsync(s => (decimal?)s.Amount) ?? 0;

            var loanExposure = await _context.Loans
                .Where(l => l.Status == "Approved")
                .SumAsync(l => (decimal?)l.OutstandingBalance) ?? 0;

            // ================= RECENT LOANS =================
            var recentLoans = await _context.Loans
                .OrderByDescending(l => l.ApplicationDate)
                .Take(5)
                .Select(l => new
                {
                    l.LoanId,
                    l.MemberId,
                    l.LoanAmount,
                    l.Status,
                    l.ApplicationDate
                })
                .ToListAsync();

            // ================= RECENT ACTIVITIES (AUDIT FEED) =================
            var recentActivities = await _context.AuditLogs
                .OrderByDescending(a => a.ActionDate)
                .Take(8)
                .Select(a => new
                {
                    a.ActionDate,
                    a.Action,
                    a.TableAffected,
                    a.UserId,
                    a.RecordId
                })
                .ToListAsync();

            // ================= NOTIFICATIONS (SYSTEM FEED) =================
            var notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Message,
                    n.CreatedAt,
                    n.IsRead
                })
                .ToListAsync();

            // ================= CHART 1: SAVINGS TREND =================
            var savingsTrend = await _context.SavingsTransactions
                .GroupBy(s => s.TransactionDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    total = g.Sum(x => x.Amount)
                })
                .Take(30)
                .ToListAsync();

            // ================= CHART 2: LOANS TREND =================
            var loanTrend = await _context.Loans
                .GroupBy(l => l.ApplicationDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    total = g.Sum(x => x.LoanAmount)
                })
                .Take(30)
                .ToListAsync();

            return Ok(new
            {
                kpis = new
                {
                    totalMembers,
                    activeLoans,
                    pendingLoans,
                    totalSavings,
                    loanExposure
                },

                recentLoans,
                recentActivities,
                notifications, 

                charts = new
                {
                    savingsTrend,
                    loanTrend
                }
            });
        }

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