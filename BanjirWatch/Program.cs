using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using BanjirWatch.Data;
using BanjirWatch.Repositories;
using BanjirWatch.Repositories.Interfaces;
using BanjirWatch.Services;
using BanjirWatch.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Railway provides PORT environment variable - use it if available
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure SQLite database (use persistent path for Railway volume)
var dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? "BanjirWatch.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Configure Authorization
builder.Services.AddAuthorization();

// Register HttpClient for Weather API
builder.Services.AddHttpClient<IWeatherApiService, WeatherApiService>();

// Register Repositories
builder.Services.AddScoped<IFloodDataRepository, FloodDataRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();

// Register Services
builder.Services.AddScoped<IWeatherApiService, WeatherApiService>();
builder.Services.AddScoped<IFloodDataService, FloodDataService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMapService, MapService>();

// Register Background Service for flood data fetching
builder.Services.AddHostedService<FloodDataBackgroundService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Skip HTTPS redirection in Railway (handled by Railway's proxy)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
