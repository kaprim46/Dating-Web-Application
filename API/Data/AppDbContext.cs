using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class AppDbContext: IdentityDbContext<AppUser, AppRole, int, 
            IdentityUserClaim<int>, AppUserRole, IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        public AppDbContext(DbContextOptions options): base(options)
        {
            
        }

        public DbSet<UserLike> Likes { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Connection> Connections { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppUser>()
              .HasMany(q => q.UserRoles)
              .WithOne(q => q.User)
              .HasForeignKey(q => q.UserId)
              .IsRequired();

            builder.Entity<AppRole>()
              .HasMany(q => q.UserRoles)
              .WithOne(q => q.Role)
              .HasForeignKey(q => q.RoleId)
              .IsRequired();

            builder.Entity<UserLike>().HasKey(k => new {k.SourceUserId, k.TargetUserId});

            builder.Entity<UserLike>().HasOne(s => s.SourceUser)
            .WithMany(l => l.LikedUsers).HasForeignKey(s => s.SourceUserId)
            .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserLike>().HasOne(s => s.TargetUser)
            .WithMany(l => l.LikedByUsers).HasForeignKey(s => s.TargetUserId)
            .OnDelete(DeleteBehavior.Cascade);


           builder.Entity<Message>().HasOne(s => s.Sender)
           .WithMany(s => s.MessagesSent)
           .OnDelete(DeleteBehavior.Restrict);

           builder.Entity<Message>().HasOne(r => r.Recepient)
           .WithMany(r => r.MessagesReceived)
           .OnDelete(DeleteBehavior.Restrict);
        }
    }
}