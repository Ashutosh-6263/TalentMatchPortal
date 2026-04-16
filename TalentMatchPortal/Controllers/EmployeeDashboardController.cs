using Microsoft.AspNetCore.Mvc;
using TalentMatchPortal.Data;
using TalentMatchPortal.Models;
using Microsoft.EntityFrameworkCore;

public class EmployeeDashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public EmployeeDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Use GetInt32 because we saved it as an Int in the Login method
        int? userId = HttpContext.Session.GetInt32("UserId");

        // If session is empty, send them back to login
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Now 'userId' is a safe number (like 1, 2, or 10)
        var user = await _context.Users.FindAsync(userId);

        var history = await _context.LeaveRequests
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.AppliedOn)
            .ToListAsync();

        ViewBag.TotalLeaves = user?.TotalLeaveBalance ?? 0;
        ViewBag.UserName = user?.FirstName;

        return View(history);
    }
    [HttpPost]
    public async Task<IActionResult> ApplyLeave(LeaveRequest leave)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId != null)
        {
            // VALIDATION: Prevent past dates
            if (leave.FromDate.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("", "You cannot apply for leave in the past.");
                // You might want to return to Index with an error message
                TempData["ErrorMessage"] = "Invalid Date: From Date cannot be in the past.";
                return RedirectToAction("Index");
            }

            // VALIDATION: Ensure ToDate is after FromDate
            if (leave.ToDate < leave.FromDate)
            {
                TempData["ErrorMessage"] = "Invalid Date: To Date must be after From Date.";
                return RedirectToAction("Index");
            }

            leave.UserId = userId.Value;
            leave.Status = "Pending";
            leave.AppliedOn = DateTime.Now;

            _context.LeaveRequests.Add(leave);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Leave application submitted successfully!";
            return RedirectToAction("Index");
        }

        return RedirectToAction("Login", "Account");
    }
}