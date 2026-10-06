using Microsoft.AspNetCore.Identity;

namespace Drought.Domain.Entities;

/// <summary>
/// Utilizador da aplicação (ASP.NET Core Identity). Perfis: "Administrador" e
/// "Investigador", conforme secção 5.1 do enunciado.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = default!;
    public string? Organization { get; set; }
}
