using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
    public DbSet<ToyOwnershipHistory> ToyOwnershipHistory => Set<ToyOwnershipHistory>();
    public DbSet<ToyGift> ToyGifts => Set<ToyGift>();
    public DbSet<ToyPriority> ToyPriorities => Set<ToyPriority>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Child> Children => Set<Child>();

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

            e.HasOne(t => t.CurrentHolder)
             .WithMany()
             .HasForeignKey(t => t.CurrentHolderUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Ownership history ────────────────────────────────────────────────
        modelBuilder.Entity<ToyOwnershipHistory>(e =>
        {
            e.HasOne(h => h.Toy)
             .WithMany(t => t.History)
             .HasForeignKey(h => h.ToyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(h => h.User)
             .WithMany()
             .HasForeignKey(h => h.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(h => new { h.ToyId, h.EndedAt });
        });

        // ── Gifts ────────────────────────────────────────────────────────────
        modelBuilder.Entity<ToyGift>(e =>
        {
            e.HasOne(g => g.Toy)
             .WithMany(t => t.Gifts)
             .HasForeignKey(g => g.ToyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(g => g.FromUser)
             .WithMany()
             .HasForeignKey(g => g.FromUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(g => g.ToUser)
             .WithMany()
             .HasForeignKey(g => g.ToUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(g => new { g.ToUserId, g.Status });
        });

        // ── Priorities (composite PK) ────────────────────────────────────────
        modelBuilder.Entity<ToyPriority>(e =>
        {
            e.HasOne(p => p.Toy)
             .WithMany(t => t.Priorities)
             .HasForeignKey(p => p.ToyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.User)
             .WithMany()
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Children ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Child>(e =>
        {
            e.HasOne(c => c.Parent)
             .WithMany()
             .HasForeignKey(c => c.ParentUserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(c => c.ParentUserId);
        });

        // ── Notifications ────────────────────────────────────────────────────
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
             .WithMany()
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
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

            e.HasOne(r => r.Receiver)
             .WithMany()
             .HasForeignKey(r => r.ReceiverUserId)
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

        // ── All timestamps are stored as UTC ─────────────────────────────────
        // SQL Server drops DateTimeKind; restore it on read so JSON carries a "Z".
        var utc = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcNullable = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var prop in entity.GetProperties())
        {
            if (prop.ClrType == typeof(DateTime)) prop.SetValueConverter(utc);
            else if (prop.ClrType == typeof(DateTime?)) prop.SetValueConverter(utcNullable);
        }

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
