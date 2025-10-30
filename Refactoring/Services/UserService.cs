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
    private readonly IPasswordService _passwordService; 
    private static Dictionary<Guid, User> _memoryCache = new(); 

    
    public UserService(ApplicationDbContext context, IConfiguration configuration, 
                      IPasswordService passwordService = null, ILogger<UserService> logger = null)
    {
        _context = context;
        _configuration = configuration;
        _passwordService = passwordService;
    }

    
    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Invalid user id", nameof(id));
        }

        
        if (_memoryCache.ContainsKey(id) && 
            (DateTime.UtcNow - _memoryCache[id].UpdatedAt).TotalMinutes < 10) 
        {
            return _memoryCache[id];
        }

        try
        {
            
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
                    Role = u.Role,
                    Age = u.Age, 
                })
                .FirstOrDefaultAsync();

            
            if (user != null)
            {
                _memoryCache[id] = user;
            }
            else
            {
                _memoryCache.Remove(id);
            }

            return user;
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Error getting user {id}: {ex.Message}");
            return null;
        }
    }

    
    public async Task<User?> EditAsync(UserUpdate request, Guid id)
    {
        
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (id == Guid.Empty)
            throw new ArgumentException("Invalid user id", nameof(id));

        
        List<string> changedFields = new List<string>();
        DateTime operationStart = DateTime.UtcNow;

        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return null;
            }

            
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == request.Email && u.Id != id);
                
                if (emailExists)
                {
                    throw new InvalidOperationException("Email already in use");
                }

                user.Email = request.Email;
                changedFields.Add("Email");
            }

            if (!string.IsNullOrEmpty(request.FirstName) && request.FirstName != user.FirstName)
            {
                user.FirstName = request.FirstName;
                changedFields.Add("FirstName");
            }

            if (!string.IsNullOrEmpty(request.LastName) && request.LastName != user.LastName)
            {
                user.LastName = request.LastName;
                changedFields.Add("LastName");
            }

            if (request.Age.HasValue && request.Age.Value != user.Age)
            {
                
                if (request.Age.Value < 0 || request.Age.Value > 150)
                {
                    throw new ArgumentOutOfRangeException("Age must be between 0 and 150");
                }
                user.Age = request.Age.Value;
                changedFields.Add("Age");
            }

            if (request.Gender.HasValue && request.Gender.Value != user.Gender)
            {
                user.Gender = request.Gender.Value;
                changedFields.Add("Gender");
            }

            
            user.UpdatedAt = DateTime.UtcNow;
            changedFields.Add("UpdatedAt");

            
            if (changedFields.Any())
            {
                await LogUserUpdate(id, changedFields, operationStart);
            }

            await _context.SaveChangesAsync();

            
            _memoryCache[id] = CreateUserResponse(user);

            return CreateUserResponse(user);
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Error updating user {id}: {ex.Message}");
            throw;
        }
    }

    
    public async Task<Role?> GetRoleAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Invalid user id", nameof(id));

        try
        {
            
            var userExists = await _context.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
            {
                return null;
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new { u.Role, u.Email }) 
                .FirstOrDefaultAsync();
            
            
            if (user != null && user.Role == Role.Admin)
            {
                
                await LogAdminAccess(id, user.Email);
            }

            return user?.Role;
        }
        catch (Exception ex)
        {
            
            return null;
        }
    }

    
    private User CreateUserResponse(UserDto userEntity)
    {
        return new User
        {
            Id = userEntity.Id,
            Email = userEntity.Email,
            FirstName = userEntity.FirstName,
            LastName = userEntity.LastName,
            CreatedAt = userEntity.CreatedAt,
            UpdatedAt = userEntity.UpdatedAt,
            Gender = userEntity.Gender,
            Role = userEntity.Role,
            Age = userEntity.Age 
        };
    }

    
    private async Task LogUserUpdate(Guid userId, List<string> changedFields, DateTime startTime)
    {
        var duration = DateTime.UtcNow - startTime;
        
        Console.WriteLine($"User {userId} updated fields: {string.Join(", ", changedFields)} in {duration.TotalMilliseconds}ms");
    }

    private async Task LogAdminAccess(Guid userId, string email)
    {
        
        Console.WriteLine($"Admin access: {email} ({userId}) at {DateTime.UtcNow}");
    }

    
    public async Task<bool> ValidateUserAccessAsync(Guid userId, string resource)
    {
        
        await Task.Delay(1); 
        return true;
    }

    public async Task<List<User>> GetUsersByRoleAsync(Role role)
    {
        
        return new List<User>();
    }
}