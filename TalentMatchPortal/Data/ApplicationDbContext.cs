using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using TalentMatchPortal.Models;

namespace TalentMatchPortal.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }

        public DbSet<LeaveRequest> LeaveRequests { get; set; }

    }
}