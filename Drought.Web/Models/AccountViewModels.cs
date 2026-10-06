using System.ComponentModel.DataAnnotations;

namespace Drought.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A palavra-passe é obrigatória.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Lembrar-me")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [Display(Name = "Nome Completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Organização / Instituição")]
    public string? Organization { get; set; }

    [Required(ErrorMessage = "A palavra-passe é obrigatória.")]
    [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} caracteres.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Palavra-passe")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Palavra-passe")]
    [Compare("Password", ErrorMessage = "As palavras-passe não coincidem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
