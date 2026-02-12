using Microsoft.EntityFrameworkCore;


public class AppDbContext : DbContext
{
    //public DbSet<Student> Students { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var connectionString =
            "Server=localhost;Port=3306;Database=vetpet;User=root;Password=;";

        options.UseMySql(connectionString,
            ServerVersion.AutoDetect(connectionString));
    }
}
