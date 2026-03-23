using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRpaTool.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Scenario> Scenarios { get; set; } = null!;
        public DbSet<ActionNode> ActionNodes { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
            string connectionString = "Server=192.168.165.69;Port=3306;User ID=measurement_user;Password=12345678;Database=testauto;SslMode=None;AllowPublicKeyRetrieval=True;CharSet=utf8mb4;";

      
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
}
