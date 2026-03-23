using Microsoft.EntityFrameworkCore;

namespace AutoRpaTool.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Scenario> Scenarios { get; set; } = null!;
        public DbSet<ActionNode> ActionNodes { get; set; } = null!;
        public DbSet<BranchRule> BranchRules { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = "Server=192.168.165.69;Port=3306;User ID=measurement_user;Password=12345678;Database=testauto;SslMode=None;AllowPublicKeyRetrieval=True;CharSet=utf8mb4;";
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // NodeType lưu dưới dạng string cho dễ đọc trong DB
            modelBuilder.Entity<ActionNode>()
                .Property(n => n.NodeType)
                .HasConversion<string>();
        }
    }
}
