using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Digital_Wallet_System.Data;
using Digital_Wallet_System.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        // Entry point of the application
        var builder = WebApplication.CreateBuilder(args);

        // Adding services to the controller
        builder.Services.AddControllers();

        // Add logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Services.AddEndpointsApiExplorer();

        // Database Services for the tables
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Registering the services due to dependency injection
        builder.Services.AddDataProtection();
        builder.Services.AddScoped<WalletService>();
        builder.Services.AddSingleton<RedisService>();
        builder.Services.AddScoped<PaystackService>();
        builder.Services.AddSingleton<ReferenceProtector>();

        // Configuring JWT Authentication
        var jwtKey = builder.Configuration["Jwt:Key"]; // JWT Key
        var jwtIssuer = builder.Configuration["Jwt:Issuer"]; // JWT Issuer
        var jwtAudience = builder.Configuration["Jwt:Audience"]; // JWT Audience

        // Check if the JWT config values are missing
        if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
        {
            throw new InvalidOperationException("JWT configuration values are missing");
        }

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
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

        builder.Services.AddAuthorization();

        // Build the web application
        var app = builder.Build();

        // Configure the HTTP Pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // app.UseHttpsRedirection(); // Not necessary for now

        // Initialize Authentication and Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Start the application
        app.Run();
    }
}