
using Api.Database;
using Microsoft.EntityFrameworkCore;

namespace Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

			var dbConStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

			// Add services to the container.

			#region DB DbContext
			builder.Services.AddDbContext<MyDbContext>(options => options.UseSqlServer(dbConStr)
#if DEBUG
			.EnableSensitiveDataLogging()
			.EnableDetailedErrors()
#endif
			, ServiceLifetime.Transient
			, ServiceLifetime.Transient);
#if DEBUG
			builder.Services.AddDatabaseDeveloperPageExceptionFilter();
#endif
			#endregion

			builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

			#region DB Initilize
			using(var scope = app.Services.CreateScope()) {
				var services = scope.ServiceProvider;
				var context = services.GetRequiredService<MyDbContext>();

				if(context.Database.IsSqlServer()) {
					context.Database.Migrate();
				}
				await DbInitializer.Initialize(context);
			}
			#endregion

			app.Run();
        }
    }
}
