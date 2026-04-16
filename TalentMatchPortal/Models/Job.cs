using System.ComponentModel.DataAnnotations;

namespace TalentMatchPortal.Models
{
    public class Job
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Job Title")]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        [Display(Name = "Salary Range")]
        public string SalaryRange { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.Now;
    }
}