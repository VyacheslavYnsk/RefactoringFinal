public class AuthResponse
{
    public  string? AccesToken { get; set; }

    public required string Message { get; set; }


    public required bool Success { get; set; }
}