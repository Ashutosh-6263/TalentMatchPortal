using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentMatchPortal.Models
{
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key to the User table
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public string LeaveType { get; set; } // Sick, Casual, Earned

        [Required]
        [Display(Name = "Start Date")]
        public DateTime FromDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        public DateTime ToDate { get; set; }

        [Required]
        public string Reason { get; set; }

        // Status can be: Pending, Approved, Rejected
        public string Status { get; set; } = "Pending";

        public DateTime AppliedOn { get; set; } = DateTime.Now;
    }
}