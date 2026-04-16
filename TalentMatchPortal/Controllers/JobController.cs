using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TalentMatchPortal.Data;
using TalentMatchPortal.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;

namespace TalentMatchPortal.Controllers
{
    [Authorize]
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- CANDIDATE SECTION ---

        [HttpGet]
        public async Task<IActionResult> List()
        {
            // Only show jobs that haven't expired (if you have an expiry date)
            var jobs = await _context.Jobs.ToListAsync();
            return View(jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int jobId, IFormFile resumeFile)
        {
            // 1. Verify User Session (Critical for HR to see WHO applied)
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                // If session expired, redirect to login
                return RedirectToAction("Login", "Account");
            }

            if (resumeFile != null && resumeFile.Length > 0)
            {
                try
                {
                    // 2. Handle File Upload
                    string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(resumeFile.FileName);
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/resume");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await resumeFile.CopyToAsync(stream);
                    }

                    // 3. Create and Save Application
                    var application = new JobApplication
                    {
                        JobId = jobId,
                        UserId = userId.Value,
                        ResumePath = fileName,
                        Status = "Pending",
                        AppliedDate = DateTime.Now
                    };

                    _context.JobApplications.Add(application);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = "Application sent successfully! HR can now review your profile.";
                    return RedirectToAction(nameof(List));
                }
                catch (Exception ex)
                {
                    // Log the error if the database fails
                    TempData["Error"] = "Database Error: Could not save application. " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Please upload a valid resume file (PDF or DOCX).";
            }

            return RedirectToAction(nameof(List));
        }


        // --- HR SECTION ---

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Jobs.ToListAsync());
        }

        [Authorize(Roles = "HR")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Create(Job job)
        {
            if (ModelState.IsValid)
            {
                _context.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(job);
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Edit(int id, Job job)
        {
            if (id != job.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(job);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Jobs.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(job);
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}