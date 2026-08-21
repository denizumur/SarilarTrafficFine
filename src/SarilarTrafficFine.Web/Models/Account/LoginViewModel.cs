using System.ComponentModel.DataAnnotations;

namespace SarilarTrafficFine.Web.Models.Account;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatýrla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}