using Drought.Application.Interfaces;
using Drought.Application.Services;
using Drought.Domain.Entities;
using Drought.Infrastructure.Data;
using Drought.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DroughtDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<DroughtDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<ITrainingService>(sp =>
    new TrainingService(sp.GetRequiredService<DroughtDbContext>(),
        Path.Combine(builder.Environment.ContentRootPath, "MLModels")));
builder.Services.AddSingleton<IWebHostPathProvider>(
    new WebHostPathProvider(builder.Environment.ContentRootPath));

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DroughtDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Administrador", "Investigador" })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Seed Administrador
    var adminUser = await userManager.FindByEmailAsync("admin@drought.mz");
    if (adminUser is null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "admin@drought.mz",
            Email = "admin@drought.mz",
            FullName = "Administrador (Grupo 1)",
            Organization = "USTM",
            EmailConfirmed = true
        };
        var res = await userManager.CreateAsync(adminUser, "Admin123!");
        if (res.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Administrador");
        }
    }

    // Seed Investigador
    var investUser = await userManager.FindByEmailAsync("investigador@drought.mz");
    if (investUser is null)
    {
        investUser = new ApplicationUser
        {
            UserName = "investigador@drought.mz",
            Email = "investigador@drought.mz",
            FullName = "Investigador USTM",
            Organization = "USTM / AI4Drought-MZ",
            EmailConfirmed = true
        };
        var res = await userManager.CreateAsync(investUser, "Invest123!");
        if (res.Succeeded)
        {
            await userManager.AddToRoleAsync(investUser, "Investigador");
        }
    }

    // Seed Observações caso a tabela esteja vazia
    if (!await db.SoilObservations.AnyAsync())
    {
        var csvPath = Path.Combine(app.Environment.ContentRootPath, "..", "data", "soil_moisture_chokwe_massinga.csv");
        if (File.Exists(csvPath))
        {
            var importService = scope.ServiceProvider.GetRequiredService<CsvImportService>();
            using var stream = File.OpenRead(csvPath);
            await importService.ImportAsync(stream, "soil_moisture_chokwe_massinga.csv", adminUser?.Id ?? "system");
        }
    }

    // Treinar e activar o primeiro modelo caso não exista
    if (!await db.ModelVersions.AnyAsync() && await db.SoilObservations.CountAsync() >= 20)
    {
        var trainingService = scope.ServiceProvider.GetRequiredService<ITrainingService>();
        await trainingService.TrainAndCompareAsync("Sistema (Seed Inicial)");
    }
}

app.Run();

public class WebHostPathProvider : IWebHostPathProvider
{
    private readonly string _contentRoot;
    public WebHostPathProvider(string contentRoot) => _contentRoot = contentRoot;
    public string MapPath(string relativePath) => Path.Combine(_contentRoot, relativePath);
}
