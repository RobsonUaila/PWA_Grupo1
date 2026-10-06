using Drought.Application.Interfaces;
using Drought.Application.Services;
using Drought.Domain.Entities;
using Drought.Infrastructure.Data;
using Drought.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Base de dados (PostgreSQL recomendado; SQL Server também suportado)
// ---------------------------------------------------------------
builder.Services.AddDbContext<DroughtDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------
// ASP.NET Core Identity — autenticação e perfis (Admin / Investigador)
// ---------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5; // mitigação de força-bruta
    })
    .AddEntityFrameworkStores<DroughtDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        // Para pedidos de API devolve 401 em vez de redireccionar para uma página de login.
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
});

// ---------------------------------------------------------------
// Serviços da aplicação
// ---------------------------------------------------------------
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<ITrainingService>(sp =>
    new TrainingService(sp.GetRequiredService<DroughtDbContext>(),
        Path.Combine(builder.Environment.ContentRootPath, "MLModels")));
builder.Services.AddSingleton<IWebHostPathProvider>(new WebHostPathProvider(builder.Environment.ContentRootPath));

builder.Services.AddControllers();

// ---------------------------------------------------------------
// Swagger / OpenAPI (requisito "API REST documentada com Swagger")
// ---------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Soil Moisture Drought Classification API",
        Version = "v1",
        Description = "Grupo 1 — Chókwè e Massinga. Programação Web Avançada (ASP.NET), USTM."
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Drought API v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Aplica migrations e garante os roles/utilizador administrador em arranque (ambiente de desenvolvimento).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DroughtDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Administrador", "Investigador" })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
}

app.Run();

/// <summary>Implementação simples de IWebHostPathProvider usada pelos serviços da Application.</summary>
public class WebHostPathProvider : IWebHostPathProvider
{
    private readonly string _contentRoot;
    public WebHostPathProvider(string contentRoot) => _contentRoot = contentRoot;
    public string MapPath(string relativePath) => Path.Combine(_contentRoot, relativePath);
}
