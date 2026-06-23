using JiSaveSacco.API.Data;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IdentityService _identity;
        private readonly AuditService _audit;

        public ReportsController(
            AppDbContext context,
            IdentityService identity,
            AuditService audit)
        {
            _context = context;
            _identity = identity;
            _audit = audit;
        }

        // =========================
        // TOTAL SAVINGS
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet("total-savings")]
        public async Task<IActionResult> TotalSavings()
        {
            var total = await _context.SavingsTransactions
                .SumAsync(s => s.Amount);

            return Ok(new
            {
                totalSavings = total
            });
        }

        // =========================
        // ACTIVE LOANS
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet("active-loans")]
        public async Task<IActionResult> ActiveLoans()
        {
            var loans = await _context.Loans
                .CountAsync(l => l.Status == "Approved");

            return Ok(new
            {
                activeLoans = loans
            });
        }

        // =========================
        // LOAN EXPOSURE
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet("loan-exposure")]
        public async Task<IActionResult> LoanExposure()
        {
            var total = await _context.Loans
                .Where(l => l.Status == "Approved")
                .SumAsync(l => l.OutstandingBalance);

            return Ok(new
            {
                totalLoanExposure = total
            });
        }

        // =========================
        // MEMBER SUMMARY
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet("member-summary")]
        public async Task<IActionResult> MemberSummary()
        {
            var members = await _context.Members.CountAsync();

            var activeLoans = await _context.Loans
                .CountAsync(l => l.Status == "Approved");

            var totalSavings = await _context.SavingsTransactions
                .SumAsync(s => s.Amount);

            return Ok(new
            {
                totalMembers = members,
                activeLoans,
                totalSavings
            });
        }

        // =========================
        // MY REPORT
        // =========================
        [Authorize(Roles = "Member")]
        [HttpGet("my-report")]
        public async Task<IActionResult> MyReport()
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized("Member not linked to account");

            var savings = await _context.SavingsTransactions
                .Where(s => s.MemberId == memberId)
                .SumAsync(s => s.Amount);

            var loans = await _context.Loans
                .Where(l => l.MemberId == memberId)
                .SumAsync(l => l.LoanAmount);

            var repayments = await _context.LoanRepayments
                .Where(r => r.Loan.MemberId == memberId)
                .SumAsync(r => r.AmountPaid);

            return Ok(new
            {
                totalSavings = savings,
                totalLoans = loans,
                totalRepayments = repayments,
                netPosition = savings - loans
            });
        }

        // =========================
        // LOG REPORT GENERATION
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPost("log")]
        public async Task<IActionResult> LogReport([FromBody] string reportType)
        {
            var report = new Report
            {
                ReportType = reportType,
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = _identity.GetUserId()
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                $"Generated {reportType} report",
                "Reports",
                report.ReportId);

            return Ok(new
            {
                message = "Report logged successfully",
                reportId = report.ReportId
            });
        }
    }
}