using Cherry.AuthApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cherry.AuthApi.Data
{
	public class AppDbContext : IdentityDbContext<ApplicationUser>
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

		public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
		public DbSet<CycleEntry> CycleEntries => Set<CycleEntry>();

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			builder.Entity<UserProfile>()
				.HasOne(p => p.User)
				.WithOne()
				.HasForeignKey<UserProfile>(p => p.UserId)
				.IsRequired();
		}
	}
}


