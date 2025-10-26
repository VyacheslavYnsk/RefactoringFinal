public class UserDto : Entity
{
    public required string Email { get; set; }

    public required string Password { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public int? Age { get; set; }

    public required Gender Gender { get; set; }
    public required Role Role { get; set; }

    public required DateTime CreatedAt { get; set; }

    public required DateTime UpdatedAt { get; set; }





}