using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Api.Database;

namespace Api.Services.Data;
public class MyDbContextFactory : IDesignTimeDbContextFactory<MyDbContext> {
	public MyDbContext CreateDbContext(string[] args) {
		var config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json")
				.Build();

		var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();
		var connectionString = config.GetConnectionString("DefaultConnection");

		optionsBuilder.UseSqlServer(connectionString);

		return new MyDbContext(optionsBuilder.Options);
	}
}