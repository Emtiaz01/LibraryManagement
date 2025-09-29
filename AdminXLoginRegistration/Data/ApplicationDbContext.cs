using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdminXLoginRegistration.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Category> Category { get; set; }
        public DbSet<Product> Product { get; set; } 
        public DbSet<BookLoan> BookLoan { get; set; }
        public DbSet<Payment> Payment { get; set; }


        public object Books { get; internal set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // If Product is deleted, all BookLoan records referencing it will also be deleted!
            modelBuilder.Entity<BookLoan>()
                .HasOne(bl => bl.Product)
                .WithMany()
                .HasForeignKey(bl => bl.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
