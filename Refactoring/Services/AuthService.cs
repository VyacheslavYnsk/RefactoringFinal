using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; 
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Model.Register;


namespace Auth.Service;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordService _passwordService;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, IPasswordService passwordService)
    {
        _context = context;
        _configuration = configuration;
        _passwordService = passwordService;
    }

    public async Task<AuthResponse> RegisterAsync(UserRegisterRequest request)
    {

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Пользователь с таким email уже существует"
            };
        }


        var user = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Gender = request.Gender,
            Role = Role.User,
            Password = _passwordService.HashPassword(request.Password)
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Message = "Регистрация успешна",
                AccesToken = token
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                Success = false,
                Message = $"Ошибка при регистрации: {ex.Message}"
            };
        }
    }



    public async Task<AuthResponse> LoginAsync(UserLoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null )
        {
            return new AuthResponse { Success = false, Message = "Пользователь не найден" };
        }

        if (!_passwordService.VerifyPassword(request.Password, user.Password))
        {
            return new AuthResponse { Success = false, Message = "Неверный пароль" };
        }

        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Success = true,
            AccesToken = token,
            Message = "Аутентификация успешна"
        };
    }





    private string GenerateJwtToken(UserDto user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var jwtKey = "G7@!f4#Zq8&lN9^kP2*eR1$hW3%tX6@zB5";

        var key = Encoding.ASCII.GetBytes(jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
            }),


            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return tokenString;
    }




}