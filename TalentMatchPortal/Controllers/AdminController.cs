using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentMatchPortal.Data;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    public AdminController(ApplicationDbContext context) => _context = context;

    public IActionResult Dashboard()
    {
        var apps = _context.JobApplications.Include(a => a.User).Include(a => a.Job).ToList();
        return View(apps);
    }

    [HttpPost]
    public IActionResult Shortlist(int appId, string examLink)
    {
        var app = _context.JobApplications.Find(appId);
        if (app != null)
        {
            app.Status = "Shortlisted";
            app.ExamLink = examLink;
            _context.SaveChanges();
            // In a real app, send email here
        }
        return RedirectToAction("Dashboard");
    }
}