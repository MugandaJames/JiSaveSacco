using JiSaveSacco.API.Data;
using JiSaveSacco.API.DTOs.Loans;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly NotificationService _notify;
        private readonly IdentityService _identity;

        public LoansController(
            AppDbContext context,
            AuditService audit,
            NotificationService notify,
            IdentityService identity)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
            _identity = identity;
        }

        // =========================================================
        // MEMBER: CHECK LOAN ELIGIBILITY (DYNAMIC HYBRID FORMULA)
        // =========================================================
        [Authorize(Roles = "Member,Admin,Staff")]
        [HttpGet("eligibility/{memberId}")]
        public async Task<IActionResult> GetLoanEligibility(int memberId)
        {
            // 1. Fetch the latest savings balance from the SavingsTransactions ledger
            var currentSavings = await _context.SavingsTransactions
                .Where(s => s.MemberId == memberId)
                .OrderByDescending(s => s.SavingId)
                .Select(s => s.BalanceAfter)
                .FirstOrDefaultAsync();

            // 2. Apply hybrid rules: Base Limit of 1,000 + (Savings * 4)
            const decimal BaseLimit = 1000m;
            const decimal SaccoMultiplier = 4m;

            decimal grossEligibleAmount = BaseLimit + (currentSavings * SaccoMultiplier);

            // 3. Subtract any active outstanding loan debt exposure
            var existingLoanBalance = await _context.Loans
                .Where(l => l.MemberId == memberId && l.Status == "Approved")
                .SumAsync(l => l.OutstandingBalance);

            decimal netEligibility = grossEligibleAmount - existingLoanBalance;
            if (netEligibility < 0) netEligibility = 0m;

            return Ok(new
            {
                MemberId = memberId,
                CurrentSavings = currentSavings,
                BaseLimitProvided = BaseLimit,
                MultiplierApplied = SaccoMultiplier,
                GrossEligibility = grossEligibleAmount,
                ActiveDebtExposure = existingLoanBalance,
                NetEligibleAmount = netEligibility // Maximum currency amount they can safely apply for right now
            });
        }

        // =========================================================
        // MEMBER: APPLY FOR LOAN (WITH AUTOMATED LIMIT CHECKS)
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLoan(CreateLoanDto dto)
        {
            var memberId = _identity.GetMemberId();
            if (memberId is null) return Unauthorized("Member identity missing");

            // 1. Re-calculate rules server-side to guarantee integrity against payload tampering
            var currentSavings = await _context.SavingsTransactions
                .Where(s => s.MemberId == memberId)
                .OrderByDescending(s => s.SavingId)
                .Select(s => s.BalanceAfter)
                .FirstOrDefaultAsync();

            decimal allowedLimit = 1000m + (currentSavings * 4m);

            var activeDebt = await _context.Loans
                .Where(l => l.MemberId == memberId && l.Status == "Approved")
                .SumAsync(l => l.OutstandingBalance);

            decimal netAllowed = allowedLimit - activeDebt;

            // 2. Terminate workflow if the application exceeds bounds
            if (dto.LoanAmount > netAllowed)
            {
                return BadRequest($"Loan application denied. Your current maximum eligibility limit is Ksh {netAllowed:N2}.");
            }

            var loan = new Loan
            {
                MemberId = memberId.Value,
                LoanAmount = dto.LoanAmount,
                InterestRate = dto.InterestRate,
                EligibleAmount = allowedLimit, // Saves permanent snapshot of historical limit context
                OutstandingBalance = dto.LoanAmount + (dto.LoanAmount * dto.InterestRate / 100),
                Status = "Pending",
                ApplicationDate = DateTime.UtcNow
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            await _audit.Log(_identity.GetUserId(), "Applied for loan", "Loans", loan.LoanId);
            return Ok(new { message = "Loan application submitted successfully", loanId = loan.LoanId, status = loan.Status });
        }

        // =========================================================
        // ADMIN: GET ALL LOANS OVERVIEW (PENDING & ACTIVE)
        // =========================================================
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("admin-overview")]
        public async Task<IActionResult> GetAdminOverview()
        {
            var loans = await _context.Loans
                .Include(l => l.Member)
                .OrderByDescending(l => l.ApplicationDate)
                .Select(l => new
                {
                    l.LoanId,
                    l.MemberId,
                    MemberName = l.Member.FirstName + " " + l.Member.LastName,
                    l.LoanAmount,
                    l.InterestRate,
                    l.OutstandingBalance,
                    l.Status,
                    l.ApplicationDate,
                    l.ApprovalDate
                })
                .ToListAsync();

            return Ok(loans);
        }

        // =========================================================
        // ADMIN: GET LOANS THAT ARE DUE / OVERDUE (VIA SCHEDULES)
        // =========================================================
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("due")]
        public async Task<IActionResult> GetDueLoans()
        {
            var today = DateTime.UtcNow;

            var dueLoans = await _context.Loans
                .Include(l => l.Member)
                .Where(l => l.Status == "Approved" && l.OutstandingBalance > 0)
                .Where(l => l.LoanSchedules.Any(s => s.DueDate <= today && s.Status != "Paid"))
                .Select(l => new
                {
                    l.LoanId,
                    l.MemberId,
                    MemberName = l.Member.FirstName + " " + l.Member.LastName,
                    l.LoanAmount,
                    l.OutstandingBalance,
                    l.Status,
                    OldestUnpaidDueDate = l.LoanSchedules
                        .Where(s => s.DueDate <= today && s.Status != "Paid")
                        .OrderBy(s => s.DueDate)
                        .Select(s => s.DueDate)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = dueLoans.Select(l => new
            {
                l.LoanId,
                l.MemberId,
                l.MemberName,
                l.LoanAmount,
                l.OutstandingBalance,
                l.Status,
                DaysOverdue = l.OldestUnpaidDueDate != default
                    ? (today - l.OldestUnpaidDueDate).Days
                    : 0
            }).ToList();

            return Ok(result);
        }

        // =========================================================
        // ADMIN: APPROVE LOAN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpPut("approve/{loanId}")]
        public async Task<IActionResult> ApproveLoan(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);
            if (loan is null) return NotFound("Loan not found");
            if (loan.Status != "Pending") return BadRequest("Loan already processed");

            loan.Status = "Approved";
            loan.ApprovalDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _audit.Log(_identity.GetUserId(), "Approved loan", "Loans", loan.LoanId);
            await _notify.Send(loan.MemberId, "Loan Approved", "Your loan has been approved successfully.");

           
            return Ok(new
            {
                success = true,
                message = "Loan approved successfully"
            });
        }

        // =========================================================
        // ADMIN: REJECT LOAN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpPut("reject/{loanId}")]
        public async Task<IActionResult> RejectLoan(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);
            if (loan is null) return NotFound("Loan not found");
            if (loan.Status != "Pending") return BadRequest("Loan already processed");

            loan.Status = "Rejected";

            await _context.SaveChangesAsync();
            await _audit.Log(_identity.GetUserId(), "Rejected loan", "Loans", loan.LoanId);
            await _notify.Send(loan.MemberId, "Loan Rejected", "Your loan application was not approved.");

            return Ok(new
            {
                success = true,
                message = "Loan rejected"
            });
        }

        // =========================================================
        // MEMBER: LOAN REPAYMENT
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpPost("repay")]
        public async Task<IActionResult> RepayLoan(LoanRepaymentDto dto)
        {
            var memberId = _identity.GetMemberId();
            if (memberId is null) return Unauthorized("Member identity missing");

            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.LoanId == dto.LoanId && l.MemberId == memberId);
            if (loan is null) return NotFound("Loan not found");
            if (loan.Status != "Approved") return BadRequest("Loan is not active");
            if (dto.AmountPaid <= 0) return BadRequest("Payment must be greater than zero");
            if (dto.AmountPaid > loan.OutstandingBalance) return BadRequest("Payment exceeds balance");

            loan.OutstandingBalance -= dto.AmountPaid;

            var repayment = new LoanRepayment
            {
                LoanId = loan.LoanId,
                AmountPaid = dto.AmountPaid,
                RemainingBalance = loan.OutstandingBalance,
                PaymentDate = DateTime.UtcNow
            };

            _context.LoanRepayments.Add(repayment);
            if (loan.OutstandingBalance == 0) loan.Status = "Paid";

            await _context.SaveChangesAsync();
            await _audit.Log(_identity.GetUserId(), "Loan repayment", "LoanRepayments", repayment.RepaymentId);
            await _notify.Send(loan.MemberId, "Payment Received", $"You paid {dto.AmountPaid}. Remaining balance: {loan.OutstandingBalance}");

            return Ok(new { message = "Repayment successful", remaining = loan.OutstandingBalance });
        }

        // =========================================================
        // MEMBER: GET MY LOANS
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpGet("my-loans")]
        public async Task<IActionResult> GetMyLoans()
        {
            var memberId = _identity.GetMemberId();
            if (memberId is null) return Unauthorized("Member identity missing");

            var loans = await _context.Loans
                .Where(l => l.MemberId == memberId)
                .OrderByDescending(l => l.ApplicationDate)
                .ToListAsync();

            return Ok(loans);
        }
    }
}