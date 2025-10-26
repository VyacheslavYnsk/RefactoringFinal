using System.ComponentModel.DataAnnotations;

public class UserLoginRequest
{
    [EmailAddress]
    public required string Email { get; set; }

    public required string Password { get; set; }





}