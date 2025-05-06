using Api.Database.Models;

using Api.Environments;

using Microsoft.EntityFrameworkCore;

namespace Api.Database;

public static class DbInitializer {
	public static async Task Initialize(MyDbContext context) {
		string superAdminEmail = ProductAppInfo.AdminEmail;
		// SuperAdmin 이 없는 경우 추가
		if(!await context.Members.AnyAsync(o => o.UserEmail == superAdminEmail)) {
			var member = new Member()
				{
				UserEmail = superAdminEmail
			};

			await context.Members.AddAsync(member);
			await context.SaveChangesAsync();
		}
	}
}
