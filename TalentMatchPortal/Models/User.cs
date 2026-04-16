using System.ComponentModel.DataAnnotations;

namespace TalentMatchPortal.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; } // Username
        public string MobileNumber { get; set; }
        public string Education { get; set; }
        public int PassOutYear { get; set; }
        public string Password { get; set; }

        // Roles: "Candidate", "Employee", "HR"
        public string Role { get; set; } = "Candidate";

        // New property for Leave Management
        // Defaulting to 20 days for new employees
        public int TotalLeaveBalance { get; set; } = 20;
    }
}