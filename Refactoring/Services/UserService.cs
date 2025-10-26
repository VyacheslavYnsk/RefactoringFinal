using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; 
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;


namespace Auth.Service;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public UserService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Invalid user id", nameof(id));

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new User
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Gender = u.Gender,
                Role = u.Role
            })
            .FirstOrDefaultAsync();

        return user;
    }


    public async Task<User?> EditAsync(UserUpdate request, Guid id)
    {

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.Email))
            user.Email = request.Email;

        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;

        if (request.Age.HasValue)
            user.Age = request.Age.Value;

        if (request.Gender.HasValue)
            user.Gender = request.Gender.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var userR = new User
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Gender = user.Gender,
            Role = user.Role
        };

        return userR;
    }
    

    public async Task<Role?> GetRoleAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Invalid user id", nameof(id));

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .FirstOrDefaultAsync();
        
        return user.Role;
    }
}
          

    
