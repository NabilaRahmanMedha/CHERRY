using System.Text;
using System.IO;
using Cherry.AuthApi; // for TokenService
using Cherry.AuthApi.Data;
using Cherry.AuthApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

builder.Services.AddDbContext<AppDbContext>(opt =>
{
	var cs = config.GetConnectionString("Default");
	var serverVersion = ServerVersion.AutoDetect(cs);
	opt.UseMySql(cs, serverVersion);
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.Password.RequiredLength = 6;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireDigit = false;
	options.Password.RequireLowercase = false;
	options.Password.RequireUppercase = false;
	options.Password.RequiredUniqueChars = 0;
	options.User.RequireUniqueEmail = true;
})
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders();

var jwtSection = config.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtSection);
var jwt = jwtSection.Get<JwtOptions>()!;

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = jwt.Issuer,
		ValidAudience = jwt.Audience,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
	};
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<TokenService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var db = services.GetRequiredService<AppDbContext>();
	await db.Database.MigrateAsync();

	var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
	var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
	await SeedData.SeedAsync(roleMgr, userMgr);
}

// Enable Swagger in all environments so it works even when ASPNETCORE_ENVIRONMENT is Production
app.UseSwagger();
app.UseSwaggerUI();

// Serve static files for uploaded images
app.UseStaticFiles();

// Ensure upload directories exist
var webRoot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsDir = Path.Combine(webRoot, "uploads", "profiles");
Directory.CreateDirectory(uploadsDir);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();


