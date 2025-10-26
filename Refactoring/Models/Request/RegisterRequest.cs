using System.ComponentModel.DataAnnotations;
namespace Model.Register;

public class UserRegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }


    [Required]
    [StringLength(50, MinimumLength = 6)]

    public required string Password { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public int? Age { get; set; }

    public required Gender Gender { get; set; }


}