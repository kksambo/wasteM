using Microsoft.EntityFrameworkCore;
using WasteManagement.Models;
using System.Security.Cryptography;
using System.Text;

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ApplicationName = typeof(Program).Assembly.FullName,
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = "wwwroot",
    // Removed Urls property as it is not valid for WebApplicationOptions
});

builder.Services.AddDbContext<WasteManagementContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();


var corsPolicy = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

builder.WebHost.UseUrls($"http://*:{port}"); // Configure the URLs

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WasteManagementContext>();
    dbContext.Database.Migrate(); // Apply pending migrations
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors(corsPolicy);

app.MapGet("/", () => "Hello World!");

app.MapPost("/api/smartbins", async (WasteManagementContext db, SmartBin smartBin) =>
{
    db.SmartBins.Add(smartBin);
    await db.SaveChangesAsync();
    return Results.Created($"/api/smartbins/{smartBin.Id}", smartBin);
});

app.MapGet("/api/smartbins", async (WasteManagementContext db) =>
{
    return await db.SmartBins.ToListAsync();
});

app.MapPost("/api/AppUsers", async (WasteManagementContext db, AppUser appUser) =>
{
    db.AppUsers.Add(appUser);
    await db.SaveChangesAsync();
    return Results.Created($"/api/AppUsers/{appUser.Id}", appUser);
});

app.MapGet("/api/AppUsers", async (WasteManagementContext db) =>
{
    return await db.AppUsers.ToListAsync();
});

app.MapPost("/api/Rewards", async (WasteManagementContext db, Reward reward) =>
{
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == reward.UserEmail);
    if (user != null)
    {
        user.Points -= reward.PointsRequired;
        user.amount += reward.Amount;
        if (user.Points < 0)
        {
            return Results.BadRequest("Not enough points");
        }
        await db.SaveChangesAsync();
    }
    else
    {
        return Results.NotFound("User not found");
    }
    db.Rewards.Add(reward);
    await db.SaveChangesAsync();
    return Results.Created($"/api/Rewards/{reward.Id}", reward);
});

app.MapGet("/api/Rewards", async (WasteManagementContext db) =>
{
    return await db.Rewards.ToListAsync();
});

app.MapPost("/api/getPoints", async (WasteManagementContext db, string email) =>
{
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
    if (user != null)
    {
        return Results.Ok(user.Points);
    }
    return Results.NotFound("User not found");
});

app.MapPost("/api/givePoints", async (WasteManagementContext db, Point point) =>
{
    // Find the user by email directly in the database
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == point.UserEmail);

    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    
    user.Points += point.Points;

 
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Points allocated successfully", UpdatedPoints = user.Points });
});




app.MapPost("/api/login", async (WasteManagementContext db, AppUser loginRequest) =>
{

    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

    if (user == null || user.Password != loginRequest.Password)
    {
        return Results.Unauthorized();
    }

   
    var token = GenerateToken(user.Email);

    return Results.Ok(new { Token = token, Message = "Login successful" });
});


string GenerateToken(string email)
{
    using var hmac = new HMACSHA256();
    var tokenBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(email + DateTime.UtcNow));
    return Convert.ToBase64String(tokenBytes);
}

app.Run();
