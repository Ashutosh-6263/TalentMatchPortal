using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentMatchPortal.Data;

namespace TalentMatchPortal.Controllers
{
    [Authorize(Roles = "Lead")] // Ensure only Leads can access this
    public class LeadDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LeadDashboardController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            // Leads see all leave requests to approve/reject
            var leaves = await _context.LeaveRequests
                .Include(l => l.User)
                .OrderByDescending(l => l.AppliedOn)
                .ToListAsync();
            return View(leaves);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            var leave = await _context.LeaveRequests.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == id);
            if (leave != null && leave.Status == "Pending")
            {
                int days = (leave.ToDate - leave.FromDate).Days + 1;
                if (leave.User.TotalLeaveBalance >= days)
                {
                    leave.Status = "Approved";
                    leave.User.TotalLeaveBalance -= days;
                    _context.Update(leave);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectLeave(int id)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "Rejected";
                _context.Update(leave);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
