// filepath: c:\Users\POL_1_G1B-01\wasteM\Program.cs
using Microsoft.EntityFrameworkCore;
using WasteManagement.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<WasteManagementContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
    return Results.Created($"/api/Reards/{reward.Id}", reward);
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
    var points = point;

    var users = await db.AppUsers.ToListAsync();

    foreach (var user in users)
    {
        if (user.Email == points.UserEmail)
        {
            user.Points += points.Points;
            await db.SaveChangesAsync();
            return Results.Ok(user.Points);
        }
    }
    
    return Results.NotFound("User not found");
});

app.Run();