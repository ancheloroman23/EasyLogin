using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Datax
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region Add a default user if it doesn't exist

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "string",
                    SurName = "string",
                    Username = "string",
                    Password = "admin123",
                    Email = "string@default.com",
                    AuthToken = Guid.NewGuid().ToString() // Autogenerate a token
                });

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 2,
                    Name = "Admin",
                    SurName = "Default",
                    Username = "admin",
                    Password = "admin123",
                    Email = "admin@default.com",
                    AuthToken = Guid.NewGuid().ToString() // Autogenerate a token
                });

            #endregion

        }
    }
}