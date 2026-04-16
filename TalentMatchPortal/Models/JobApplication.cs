using System.ComponentModel.DataAnnotations;

namespace TalentMatchPortal.Models
{
    public class JobApplication
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int UserId { get; set; }
        public string ResumePath { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Shortlisted, Rejected

        public string? ExamLink { get; set; }

        public int? ExamScore { get; set; } = 0;
        public DateTime AppliedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Job Job { get; set; }
        public virtual User User { get; set; }
    }
}