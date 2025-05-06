
using Api.Database.Models;

using Microsoft.EntityFrameworkCore;

namespace Api.Database;
public class MyDbContext : DbContext {
	public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

	public DbSet<Member> Members { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<Member>(entity =>
		{
			entity.ToTable(nameof(Member));
		});
	}
}