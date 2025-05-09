
using Api.Database;
using Api.Environments;
using Api.Services;
using Api.Services.Database;
using Api.Services.Hosted;

using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerUI;

namespace Api {
	public class Program {
		const string CorsPolicyName = "CorsPolicyName";
		const string Title = $"{ProductAppInfo.AppName} API";
		const string Version = "v1";
		const string Description = $"{ProductAppInfo.AppName} solution api server";
		public static async Task Main(string[] args) {
			var builder = WebApplication.CreateBuilder(args);

			var dbConStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

			// Add services to the container.

			#region AddHttpClient
			builder.Services.AddHttpClient();
			#endregion

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

			#region Config
			builder.Services.AddConfig(builder.Configuration);
			#endregion

			#region DB Service

			builder.Services.AddTransient<IDbMemberService, DbMemberService>();

			#endregion

			#region Cors

			builder.Services.AddCors(options => {
				options.AddPolicy("CorsPolicyName",
				builder => {
					builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
				});
			});

			#endregion

			builder.Services.AddSingleton<ISmartThingsService, SmartThingsService>();
			builder.Services.AddSingleton<IInfluxDbService, InfluxDbService>();

			#region Hosted Service
			builder.Services.AddHostedService<TokenRefreshWorker>();
			builder.Services.AddHostedService<SmartThingsDataWorker>();
			#endregion

			builder.Services.AddControllers();
			builder.Services.AddOpenApi();

			#region Swagger

			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(c => {
				c.SwaggerDoc(Version,
					new OpenApiInfo {
						Title = Title,
						Version = Version,
						Description = Description,
						Contact =
							new OpenApiContact {
								Name = ProductAppInfo.DeveloperName,
								Email = ProductAppInfo.AdminEmail
							}
					}
				);
				// include all project's xml comments
				var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
				foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
					if(!assembly.IsDynamic) {
						var xmlFile = $"{assembly.GetName().Name}.xml";
						var xmlPath = Path.Combine(baseDirectory, xmlFile);
						if(File.Exists(xmlPath)) {
							c.IncludeXmlComments(xmlPath);
						}
					}
				}

				c.UseInlineDefinitionsForEnums();

				c.UseAllOfToExtendReferenceSchemas();
			});
			#endregion


			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if(app.Environment.IsDevelopment()) {
				app.MapOpenApi();
			}

			#region Swagger + UI

			app.UseSwagger();
			app.UseSwaggerUI(c => {
				c.SwaggerEndpoint($"{ProductAppInfo.SwaggerRoute}/{Version}/swagger.json", $"{Title} {Version}");
				c.DisplayRequestDuration();
				c.DocExpansion(DocExpansion.List);
				c.EnableDeepLinking();
				c.EnableFilter();
				c.ShowExtensions();
				c.ShowCommonExtensions();
				c.EnableValidator();
			});
			#endregion

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

			#region SmartThingsService Init

			var smartThingsService = app.Services.GetRequiredService<ISmartThingsService>();
			await smartThingsService.Initialize();

			#endregion

			app.Run();
		}
	}
}
