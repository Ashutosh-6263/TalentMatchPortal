using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for .Include and .FirstOrDefaultAsync
using System.Net;                    // Required for NetworkCredential
using System.Net.Mail;               // Required for SmtpClient
using System.Threading.Tasks;
using TalentMatchPortal.Data;
using TalentMatchPortal.Models;

namespace TalentMatchPortal.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HRDashboardController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            // 1. Get all jobs posted by HR
            var jobs = await _context.Jobs.ToListAsync();

            // 2. Get all applications with User and Job details
            var applications = await _context.JobApplications
                .Include(a => a.User)
                .Include(a => a.Job)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            // 3. Create a ViewModel or use a Tuple to send both lists to the View
            var dashboardData = new Tuple<IEnumerable<Job>, IEnumerable<JobApplication>>(jobs, applications);

            return View(dashboardData);
        }
        // GET: HRDashboard/Edit/5
        [HttpGet]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            // EXPLICIT PATH: Points exactly to the file we just created
            return View("~/Views/Jobs/Edit.cshtml", job);
        }

        // POST: HRDashboard/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Location,SalaryRange,PostedDate")] Job job)
        {
            if (id != job.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // This updates the existing record in the database
                    _context.Update(job);
                    await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateJob @p0, @p1, @p2, @p3, @p4",
                    job.Id, job.Title, job.Description, job.Location, job.SalaryRange);

                    TempData["Message"] = "Job details updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Jobs.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
            }

            // If validation fails (e.g. Title is empty), return to the same view
            return View("~/Views/Jobs/Edit.cshtml", job);
        }

        // POST: HRDashboard/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Delete(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job != null)
            {
                _context.Jobs.Remove(job);
                
                await _context.Database.ExecuteSqlRawAsync("EXEC sp_DeleteJobAndApplications @p0", id);
                TempData["Message"] = "Job deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shortlist(int appId)
        {
            var app = await _context.JobApplications
                .Include(a => a.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(x => x.Id == appId);

            if (app != null)
            {
                try
                {
                    // 1. Update Database
                    app.Status = "Shortlisted";
                    _context.Update(app);
                  
                    // For Shortlist
                    await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateApplicationStatus @p0, @p1", appId, "Shortlisted");

                    // 2. Email Details
                    string candidateName = app.User?.FirstName ?? "Candidate"; // Uses the Name property we fixed earlier
                    string jobTitle = app.Job?.Title ?? "Software Developer";

                    // PLACEHOLDER: Replace this with your real Google Form link
                    string assessmentLink = "https://docs.google.com/forms/d/e/1FAIpQLSfl-EUsehUP2KLu9UEs_FhTVuG93NJ306Y5lLQSBm2qlyG6Og/viewform?usp=dialog";

                    using (var smtp = new SmtpClient("smtp.gmail.com"))
                    {
                        smtp.Port = 587;
                        smtp.Credentials = new NetworkCredential("aashutoshchoudhary966@gmail.com", "lpmu crcl zlly cbym");
                        smtp.EnableSsl = true;

                        string subject = $"Interview Invitation: Technical Assessment for {jobTitle} | TalentMatch";

                        string body = $@"
Dear {candidateName},

Thank you for your interest in the {jobTitle} position at our organization.

After reviewing your application and resume, we are pleased to inform you that you have been shortlisted for the next stage of our recruitment process. We were impressed with your background and would like to invite you to complete a preliminary technical assessment.

Assessment Details:
        • Topics: Core Java, SQL, and Logic
        • Estimated Time: 15–20 minutes

Please click the link below to begin the assessment:
{assessmentLink}

Kindly complete this assessment within the next 48 hours to ensure your application remains active. Should you have any technical difficulties, please reply directly to this email.

We look forward to reviewing your results.

Best Regards,

Human Resources Team
TalentMatch Portal
Pune, Maharashtra";

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress("your-email@gmail.com", "TalentMatch HR"),
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = false
                        };
                        mailMessage.To.Add(app.User.Email);

                        await smtp.SendMailAsync(mailMessage);
                    }

                    TempData["Message"] = "Candidate successfully shortlisted and professional invitation sent.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Application status updated, but invitation email failed: " + ex.Message;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Reject(int appId)
        {
            var app = await _context.JobApplications
                .Include(a => a.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(x => x.Id == appId);

            if (app != null)
            {
                try
                {
                    // 1. Update Status in Database
                    app.Status = "Rejected";
                    _context.Update(app);
                    // For Reject
                    await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateApplicationStatus @p0, @p1", appId, "Rejected");

                    // 2. Send Rejection Email
                    string myEmail = "aashutoshchoudhary966@gmail.com";
                    string appPassword = "lpmu crcl zlly cbym";

                    string candidateName = app.User?.FirstName ?? "Candidate";
                    string jobTitle = app.Job?.Title ?? "Software Developer";

                    using (var smtp = new SmtpClient("smtp.gmail.com"))
                    {
                        smtp.Port = 587;
                        smtp.Credentials = new NetworkCredential(myEmail, appPassword);
                        smtp.EnableSsl = true;

                        using (var mailMessage = new MailMessage())
                        {
                            mailMessage.From = new MailAddress(myEmail, "TalentMatch HR");
                            mailMessage.To.Add(app.User.Email);
                            mailMessage.Subject = $"Application Update: {jobTitle} Role | TalentMatch";

                            mailMessage.Body = $@"
Dear {candidateName},

Thank you for giving us the opportunity to review your application for the {jobTitle} position.

After careful consideration, we regret to inform you that we will not be moving forward with your application at this time. Our team received a high volume of qualified applicants, and we have decided to pursue other candidates whose experience more closely aligns with our current needs.

We appreciate the time and effort you put into your application. We will keep your resume in our talent pool for future opportunities that may be a better fit.

We wish you the very best in your job search and future professional endeavors.

Best Regards,

Human Resources Team
TalentMatch Portal";

                            await smtp.SendMailAsync(mailMessage);
                        }
                    }
                    TempData["Message"] = "Candidate rejected and notification email sent.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Status updated to Rejected, but email failed: " + ex.Message;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken] // Added for security
        public async Task<IActionResult> SendOfferLetter(int appId)
        {
            var app = await _context.JobApplications
                .Include(a => a.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(x => x.Id == appId);

            // 1. Check if candidate exists AND meets the 60% (6/10) criteria
            if (app != null)
            {
                if (app.ExamScore < 6)
                {
                    TempData["Error"] = "Cannot send offer. Candidate scored below 60%.";
                    return RedirectToAction(nameof(Index));
                }

                try
                {
                    // 2. Update Database Status
                    app.Status = "Offered";
                    _context.Update(app);
                    await _context.SaveChangesAsync();

                    // 3. Configure SMTP (Using your existing credentials)
                    using (var smtp = new SmtpClient("smtp.gmail.com"))
                    {
                        smtp.Port = 587;
                        smtp.Credentials = new NetworkCredential("aashutoshchoudhary966@gmail.com", "lpmu crcl zlly cbym");
                        smtp.EnableSsl = true;

                        using (var mailMessage = new MailMessage())
                        {
                            mailMessage.From = new MailAddress("aashutoshchoudhary966@gmail.com", "TalentMatch HR");
                            mailMessage.To.Add(app.User.Email);
                            mailMessage.Subject = $"Official Job Offer: {app.Job.Title} | TalentMatch Corp";
                            mailMessage.IsBodyHtml = true; // Set to true for the HTML design below

                            mailMessage.Body = $@"
                    <div style='font-family: Arial, sans-serif; border: 2px solid #198754; padding: 25px; border-radius: 10px;'>
                        <h2 style='color: #198754;'>Congratulations {app.User.FirstName}!</h2>
                        <p>Based on your excellent performance in the technical assessment (Score: {app.ExamScore}/10), 
                           we are thrilled to offer you the position of <strong>{app.Job.Title}</strong>.</p>
                        
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px;'>
                            <p><b>Salary Package:</b> {app.Job.SalaryRange}</p>
                            <p><b>Location:</b> {app.Job.Location}</p>
                            <p><b>Joining Date:</b> Within 15 days of acceptance</p>
                        </div>

                        <p>Please reply to this email to confirm your acceptance of this offer.</p>
                        <hr/>
                        <p>Welcome to the team!<br/><b>Human Resources Team</b><br/>TalentMatch Portal</p>
                    </div>";

                            await smtp.SendMailAsync(mailMessage);
                        }
                    }

                    TempData["Message"] = $"Offer Letter successfully sent to {app.User.FirstName}!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Status updated, but Offer Email failed: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Index));
        }
        [AllowAnonymous] //Open ngrok : ngrok http 7237
        [HttpPost("/HRDashboard/UpdateScoreFromGoogle")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateScoreFromGoogle([FromForm] string email, [FromForm] double score) // Changed int to double
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("Email missing");

            // Now Math.Round will work perfectly
            int finalScore = (int)Math.Round(score);

            System.Diagnostics.Debug.WriteLine($"---> Received Score: {finalScore} for Email: {email}");

            var app = await _context.JobApplications
                .Include(u => u.User)
                .OrderByDescending(a => a.AppliedDate)
                .FirstOrDefaultAsync(x => x.User.Email.Trim().ToLower() == email.Trim().ToLower()
                                       && x.Status == "Shortlisted");

            if (app != null)
            {
                app.ExamScore = finalScore;
                _context.Update(app);
                await _context.SaveChangesAsync();
                return Ok($"Successfully updated {email} with score {finalScore}"); // Added a success message
            }

            return NotFound("No matching shortlisted candidate found.");
        }
        [HttpPost]
        public async Task<IActionResult> HireCandidate(int candidateId)
        {
            // 1. Find the user in the database
            var user = await _context.Users.FindAsync(candidateId);

            if (user != null)
            {
                // 2. Change the Role to Employee
                user.Role = "Employee";

                // 3. Ensure they have their starting leave balance
                user.TotalLeaveBalance = 20;

                _context.Update(user);
                await _context.SaveChangesAsync();

                // Optional: You might also want to update the JobApplication status to "Hired"
                var application = await _context.JobApplications
                    .FirstOrDefaultAsync(a => a.UserId == candidateId);
                if (application != null)
                {
                    application.Status = "Hired";
                    _context.Update(application);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }

    }
}
