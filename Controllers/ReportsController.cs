using JiSaveSacco.API.Data;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Controllers
{
    // Unified binding DTO container to align inbound parameter streams
    public class ReportGenerationRequest
    {
        public string ReportType { get; set; } = string.Empty;
    }

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

        // =========================================================
        // ADMIN: VIEW DYNAMIC REPORT DATA CONTENT LIVE
        // =========================================================
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("view")]
        public async Task<IActionResult> ViewReportContent([FromBody] ReportGenerationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ReportType))
                return BadRequest("Invalid report type specified.");

            switch (request.ReportType)
            {

                case "Monthly Summary":
                    var currentMonth = DateTime.UtcNow.Month;
                    var currentYear = DateTime.UtcNow.Year;

                    // Fetch aggregate metrics for the current calendar month
                    var savingsThisMonth = await _context.SavingsTransactions
                        .Where(s => s.TransactionDate.Month == currentMonth && s.TransactionDate.Year == currentYear)
                        .SumAsync(s => (decimal?)s.Amount) ?? 0;

                    var loansThisMonth = await _context.Loans
                        .Where(l => l.ApplicationDate.Month == currentMonth && l.ApplicationDate.Year == currentYear)
                        .SumAsync(l => (decimal?)l.LoanAmount) ?? 0;

                    var newMembersThisMonth = await _context.Members
                        .Where(m => m.CreatedAt.Month == currentMonth && m.CreatedAt.Year == currentYear)
                        .CountAsync();

                    var monthlyData = new List<object>
                    {
                        new {
                            MonthName = DateTime.UtcNow.ToString("MMMM yyyy"),
                            NewMembers = newMembersThisMonth,
                            SavingsVolume = savingsThisMonth,
                            LoansIssued = loansThisMonth
                        }
                    };
                    return Ok(new { Type = request.ReportType, Data = monthlyData });

                case "Loan Report":
                    var loanData = await _context.Loans
                        .Include(l => l.Member)
                        .OrderByDescending(l => l.ApplicationDate)
                        .Select(l => new {
                            l.LoanId,
                            MemberName = l.Member.FirstName + " " + l.Member.LastName,
                            l.LoanAmount,
                            l.OutstandingBalance,
                            l.Status
                        }).ToListAsync();
                    return Ok(new { Type = request.ReportType, Data = loanData });

                case "Savings Report":
                    var savingsData = await _context.SavingsTransactions
                        .Include(s => s.Member)
                        .OrderByDescending(s => s.TransactionDate)
                        .Take(100) // Caps row extraction length to prioritize API network pipeline performance
                        .Select(s => new {
                            s.SavingId,
                            MemberName = s.Member.FirstName + " " + s.Member.LastName,
                            s.Amount,
                            Type = s.TransactionType,
                            Date = s.TransactionDate
                        }).ToListAsync();
                    return Ok(new { Type = request.ReportType, Data = savingsData });

                case "Member Report":
                    var memberData = await _context.Members
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => new {
                            m.MemberId,
                            m.MemberNo,
                            Name = m.FirstName + " " + m.LastName,
                            m.Email,
                            m.Status
                        }).ToListAsync();
                    return Ok(new { Type = request.ReportType, Data = memberData });

                default:
                    return BadRequest("The selected report profiling layout classification is not configured yet.");
            }
        }

        // =========================================================
        // ADMIN: GET REPORT GENERATION TRAIL HISTORY
        // =========================================================
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("log")]
        public async Task<IActionResult> GetReportsHistoryLog()
        {
            var logs = await _context.Reports
                .Include(r => r.User)
                .OrderByDescending(r => r.GeneratedAt)
                .Select(r => new
                {
                    r.ReportId,
                    r.ReportType,
                    GeneratedBy = r.User != null ? r.User.Username : $"ID: {r.GeneratedBy}",
                    r.GeneratedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        // =========================================================
        // ADMIN: LOG / INITIATE NEW REPORT RUN RECORD
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpPost("log")]
        public async Task<IActionResult> LogReport([FromBody] ReportGenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ReportType))
                return BadRequest("Invalid or empty report selection type parameters.");

            var currentUserId = _identity.GetUserId();

            var report = new Report
            {
                ReportType = request.ReportType,
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = currentUserId
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            await _audit.Log(
                currentUserId,
                $"Generated {request.ReportType} report",
                "Reports",
                report.ReportId);

            return Ok(new
            {
                success = true,
                message = "Report logged successfully",
                reportId = report.ReportId
            });
        }

        // =========================================================
        // ADMIN: MEMBER SUMMARY AGGREGATE STATS
        // =========================================================
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("member-summary")]
        public async Task<IActionResult> MemberSummary()
        {
            var members = await _context.Members.CountAsync();
            var activeLoans = await _context.Loans.CountAsync(l => l.Status == "Approved");

            var totalSavings = await _context.SavingsTransactions
                .OrderByDescending(s => s.SavingId)
                .GroupBy(s => s.MemberId)
                .Select(g => g.FirstOrDefault().BalanceAfter)
                .SumAsync();

            var loanExposure = await _context.Loans
                .Where(l => l.Status == "Approved")
                .SumAsync(l => l.OutstandingBalance);

            return Ok(new
            {
                totalMembers = members,
                activeLoans,
                totalSavings,
                loanExposure
            });
        }

        [Authorize(Roles = "Member")]
        [HttpGet("my-report")]
        public async Task<IActionResult> MyReport()
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized("Member not linked to account");

            var savings = await _context.SavingsTransactions
                .Where(s => s.MemberId == memberId)
                .OrderByDescending(s => s.SavingId)
                .Select(s => (decimal?)s.BalanceAfter)
                .FirstOrDefaultAsync() ?? 0;

            var totalLoans = await _context.Loans
                .Where(l => l.MemberId == memberId && l.Status == "Approved")
                .SumAsync(l => (decimal?)l.OutstandingBalance) ?? 0;

            var activeLoans = await _context.Loans
                .CountAsync(l => l.MemberId == memberId && l.Status == "Approved");

            return Ok(new
            {
                totalSavings = savings,
                totalLoans,
                activeLoans,
                netPosition = savings - totalLoans
            });
        }
    }
}