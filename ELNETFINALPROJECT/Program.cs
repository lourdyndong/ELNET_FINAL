using Microsoft.EntityFrameworkCore;
using ELNETFINALPROJECT.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=accounts.db"));

builder.Services.AddHostedService<ELNETFINALPROJECT.Services.StationTimerService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Ensure db is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Transactions (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT, Type TEXT, Amount DECIMAL, CreatedAt DATETIME, StationInfo TEXT);");
    db.Database.ExecuteSqlRaw("UPDATE Accounts SET SessionHourlyRate = 20, DisplayName = Username;");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
