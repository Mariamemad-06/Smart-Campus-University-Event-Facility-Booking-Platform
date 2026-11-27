using BookingAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BookingAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Existing SQL Server database

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSeeding((context, _) =>
    {
        string[] roleNames = { "Student", "Faculty", "Admin" };

        foreach (var roleName in roleNames)
        {
            if (!context.Set<BookingAPI.Models.Role>()
                .Any(r => r.RoleName == roleName))
            {
                context.Set<BookingAPI.Models.Role>().Add(
                    new BookingAPI.Models.Role
                    {
                        RoleName = roleName
                    });
            }
        }

        context.SaveChanges();
    })
    .UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
        string[] roleNames = { "Student", "Faculty", "Admin" };

        foreach (var roleName in roleNames)
        {
            if (!await context.Set<BookingAPI.Models.Role>()
                .AnyAsync(r => r.RoleName == roleName,
                          cancellationToken))
            {
                context.Set<BookingAPI.Models.Role>().Add(
                    new BookingAPI.Models.Role
                    {
                        RoleName = roleName
                    });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }));


// JWT authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is missing.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is missing.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();

// Existing OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Authentication must run before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();