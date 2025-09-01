using Cherry.AuthApi.Models;
using Microsoft.AspNetCore.Identity;

namespace Cherry.AuthApi.Data
{
	public static class SeedData
	{
		private static readonly string[] Roles = ["User", "Gynecologist", "Pharmacy", "Admin"];

		public static async Task SeedAsync(RoleManager<IdentityRole> roleMgr, UserManager<ApplicationUser> userMgr)
		{
			foreach (var r in Roles)
				if (!await roleMgr.RoleExistsAsync(r))
					await roleMgr.CreateAsync(new IdentityRole(r));

			var adminEmail = "admin@cherry.local";
			var admin = await userMgr.FindByEmailAsync(adminEmail);
			if (admin == null)
			{
				admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
				await userMgr.CreateAsync(admin, "Admin#123");
				await userMgr.AddToRoleAsync(admin, "Admin");
			}
		}
	}
}


