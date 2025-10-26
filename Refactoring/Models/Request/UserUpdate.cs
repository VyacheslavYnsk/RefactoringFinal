using System.ComponentModel.DataAnnotations;


public class UserUpdate
{
    
    [EmailAddress]

    public string? Email { get; set; }

    public  string? FirstName { get; set; }

    public string? LastName { get; set; }

    public int? Age { get; set; }

    public Gender? Gender { get; set; }

}