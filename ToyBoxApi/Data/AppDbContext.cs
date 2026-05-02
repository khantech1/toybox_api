using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Toy> Toys => Set<Toy>();
    public DbSet<ToyImage> ToyImages => Set<ToyImage>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<SharedToy> SharedToys => Set<SharedToy>();
    public DbSet<ExchangeRequest> ExchangeRequests => Set<ExchangeRequest>();
    public DbSet<ExchangeRequestToy> ExchangeRequestToys => Set<ExchangeRequestToy>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        // ── Toy ──────────────────────────────────────────────────────────────
        // SQL Server error 1785: a table cannot have two FKs to the same parent
        // table that both use CASCADE or SET NULL, because that creates multiple
        // cascade paths.  Rule: only ONE of the two Category FKs may have an
        // automatic action; the other must be NO ACTION (client-side null clear).
        modelBuilder.Entity<Toy>(e =>
        {
            e.HasOne(t => t.Category)
             .WithMany(c => c.ToysInCategory)
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);       // OK – first path

            e.HasOne(t => t.DesiredCategory)
             .WithMany(c => c.ToysDesiredCategory)
             .HasForeignKey(t => t.DesiredCategoryId)
             .OnDelete(DeleteBehavior.ClientSetNull); // NO ACTION in DB; EF clears in memory

            e.HasOne(t => t.Owner)
             .WithMany(u => u.Toys)
             .HasForeignKey(t => t.OwnerUserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Contact (composite PK) ────────────────────────────────────────────
        modelBuilder.Entity<Contact>(e =>
        {
            e.HasOne(c => c.User)
             .WithMany(u => u.ContactsAsUser)
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.ContactUser)
             .WithMany(u => u.ContactsAsContact)
             .HasForeignKey(c => c.ContactId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SharedToy (composite PK) ──────────────────────────────────────────
        modelBuilder.Entity<SharedToy>(e =>
        {
            e.HasOne(s => s.SharedWithUser)
             .WithMany(u => u.SharedToys)
             .HasForeignKey(s => s.SharedWithUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Toy)
             .WithMany(t => t.SharedWith)
             .HasForeignKey(s => s.ToyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ExchangeRequest ───────────────────────────────────────────────────
        modelBuilder.Entity<ExchangeRequest>(e =>
        {
            e.HasOne(r => r.Initiator)
             .WithMany(u => u.InitiatedRequests)
             .HasForeignKey(r => r.InitiatorUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ExchangeRequestToy (composite PK) ─────────────────────────────────
        modelBuilder.Entity<ExchangeRequestToy>(e =>
        {
            e.HasOne(rt => rt.ExchangeRequest)
             .WithMany(r => r.Toys)
             .HasForeignKey(rt => rt.RequestId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(rt => rt.Toy)
             .WithMany(t => t.ExchangeRequestToys)
             .HasForeignKey(rt => rt.ToyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Review ────────────────────────────────────────────────────────────
        // Reviews has three FKs to Users (reviewer, reviewee) and one to
        // ExchangeRequest which cascades.  Both user FKs must be RESTRICT to
        // avoid another cascade-cycle through ExchangeRequest → Review.
        modelBuilder.Entity<Review>(e =>
        {
            e.HasOne(r => r.Reviewer)
             .WithMany(u => u.ReviewsGiven)
             .HasForeignKey(r => r.ReviewerUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Reviewee)
             .WithMany(u => u.ReviewsReceived)
             .HasForeignKey(r => r.RevieweeUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.ExchangeRequest)
             .WithMany(req => req.Reviews)
             .HasForeignKey(r => r.RequestId)
             .OnDelete(DeleteBehavior.Cascade);

            // One reviewer can only submit one review per request
            e.HasIndex(r => new { r.RequestId, r.ReviewerUserId }).IsUnique();
        });

        // ── Seed categories ───────────────────────────────────────────────────
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1,  CategoryName = "Educational"     },
            new Category { CategoryId = 2,  CategoryName = "Creative"        },
            new Category { CategoryId = 3,  CategoryName = "Vehicles"        },
            new Category { CategoryId = 4,  CategoryName = "Dolls & Figures" },
            new Category { CategoryId = 5,  CategoryName = "Outdoor & Sports"},
            new Category { CategoryId = 6,  CategoryName = "Puzzles & Games" },
            new Category { CategoryId = 7,  CategoryName = "Building Blocks" },
            new Category { CategoryId = 8,  CategoryName = "Electronic Toys" },
            new Category { CategoryId = 9,  CategoryName = "Stuffed Animals" },
            new Category { CategoryId = 10, CategoryName = "Arts & Crafts"   }
        );
    }
}
