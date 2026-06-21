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

        // =========================
        // APPLY FOR LOAN (SECURE)
        // =========================
        [Authorize(Roles = "Member")]
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLoan(CreateLoanDto dto)
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized("No member linked to account");

            var loan = new Loan
            {
                MemberId = memberId.Value,
                LoanAmount = dto.LoanAmount,
                InterestRate = dto.InterestRate,
                OutstandingBalance =
                    dto.LoanAmount + (dto.LoanAmount * dto.InterestRate / 100),
                Status = "Pending",
                ApplicationDate = DateTime.UtcNow
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            await _audit.Log(null, "Applied for loan", "Loans", loan.LoanId);

            return Ok(new
            {
                message = "Loan application submitted",
                loanId = loan.LoanId,
                status = loan.Status
            });
        }

        // =========================
        // APPROVE LOAN (ADMIN)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPut("approve/{loanId}")]
        public async Task<IActionResult> ApproveLoan(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);

            if (loan == null)
                return NotFound("Loan not found");

            if (loan.Status != "Pending")
                return BadRequest("Loan already processed");

            loan.Status = "Approved";
            loan.ApprovalDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.Log(null, "Approved loan", "Loans", loanId);

            await _notify.Send(
                loan.MemberId,
                "Loan Approved",
                "Your loan has been approved successfully."
            );

            return Ok("Loan approved");
        }

        // =========================
        // REJECT LOAN (ADMIN)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPut("reject/{loanId}")]
        public async Task<IActionResult> RejectLoan(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);

            if (loan == null)
                return NotFound("Loan not found");

            loan.Status = "Rejected";

            await _context.SaveChangesAsync();

            await _audit.Log(null, "Rejected loan", "Loans", loanId);

            await _notify.Send(
                loan.MemberId,
                "Loan Rejected",
                "Your loan application was not approved."
            );

            return Ok("Loan rejected");
        }

        // =========================
        // LOAN REPAYMENT (SECURE)
        // =========================
        [Authorize(Roles = "Member")]
        [HttpPost("repay")]
        public async Task<IActionResult> RepayLoan(LoanRepaymentDto dto)
        {
            var memberId = _identity.GetMemberId();

            var loan = await _context.Loans
                .FirstOrDefaultAsync(l => l.LoanId == dto.LoanId
                                       && l.MemberId == memberId);

            if (loan == null)
                return NotFound("Loan not found");

            if (loan.Status != "Approved")
                return BadRequest("Loan is not active");

            loan.OutstandingBalance -= dto.AmountPaid;

            var repayment = new LoanRepayment
            {
                LoanId = dto.LoanId,
                AmountPaid = dto.AmountPaid,
                RemainingBalance = loan.OutstandingBalance,
                PaymentDate = DateTime.UtcNow
            };

            _context.LoanRepayments.Add(repayment);

            if (loan.OutstandingBalance <= 0)
            {
                loan.Status = "Paid";
                loan.OutstandingBalance = 0;
            }

            await _context.SaveChangesAsync();

            await _audit.Log(null, "Loan repayment", "LoanRepayments", dto.LoanId);

            await _notify.Send(
                loan.MemberId,
                "Payment Received",
                $"You paid {dto.AmountPaid}. Remaining balance: {loan.OutstandingBalance}"
            );

            return Ok(new
            {
                message = "Repayment successful",
                remaining = loan.OutstandingBalance
            });
        }

        // =========================
        // GET MY LOANS (SECURE)
        // =========================
        [Authorize(Roles = "Member")]
        [HttpGet("my-loans")]
        public async Task<IActionResult> GetMyLoans()
        {
            var memberId = _identity.GetMemberId();

            var loans = await _context.Loans
                .Where(l => l.MemberId == memberId)
                .ToListAsync();

            return Ok(loans);
        }
    }
}