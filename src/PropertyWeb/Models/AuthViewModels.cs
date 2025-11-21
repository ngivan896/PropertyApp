using System.ComponentModel.DataAnnotations;

namespace PropertyWeb.Models;

public class RegisterViewModel
{
    [Required]
    [MaxLength(80)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "owner";

    [MaxLength(25)]
    public string? Phone { get; set; }
}

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AdminCreateUserViewModel
{
    [Required]
    [MaxLength(80)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "owner";

    [MaxLength(25)]
    public string? Phone { get; set; }
}

public class AdminPropertyFormViewModel
{
    [Required]
    public Guid OwnerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Address_line { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Unit_label { get; set; }

    [MaxLength(40)]
    public string? Property_type { get; set; }

    public IEnumerable<User_account> OwnerOptions { get; set; } = Enumerable.Empty<User_account>();
}
