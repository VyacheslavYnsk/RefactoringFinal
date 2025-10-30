using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Model.Register;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;


namespace Refactoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        private readonly ITokenRevocationService _tokenRevocationService;


        public AuthController(ApplicationDbContext context, IConfiguration configuration, ITokenRevocationService tokenRevocationService)
        {
            _context = context;
            _configuration = configuration;
            _tokenRevocationService = tokenRevocationService;
        }

        
        [HttpPost("register")]
        public async Task<IActionResult> Register(string email, string password,
            string firstName, string lastName, Gender gender)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || email.Length < 5 || !email.Contains("@"))
                    return BadRequest(new { success = false, message = "Email invalid" });

                if (string.IsNullOrEmpty(password) || password.Length < 6)
                    return BadRequest(new { success = false, message = "Password invalid" });


                if (string.IsNullOrEmpty(firstName) || firstName.Length < 2)
                    return BadRequest(new { success = false, message = "First name invalid" });

                if (string.IsNullOrEmpty(lastName) || lastName.Length < 2)
                    return BadRequest(new { success = false, message = "Last name invalid" });


                
                var userExists = await _context.Users.AnyAsync(u => u.Email == email);
                if (userExists)
                {
                    return BadRequest(new { success = false, message = "User already exists" });
                }

                
                var userId = Guid.NewGuid();
                var now = DateTime.UtcNow;

                var user = new UserDto
                {
                    Id = userId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Role = Role.User,
                    Gender = gender,
                    Password = BCrypt.Net.BCrypt.HashPassword(password)

                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("G7@!f4#Zq8&lN9^kP2*eR1$hW3%tX6@zB5");
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                };


                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(24), 
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                
                
                
                

                
                return StatusCode(201, new
                {
                    success = true,
                    message = "User registered successfully",
                    token = tokenString,
                    user = new User
                    {
                        Id = userId,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Role = user.Role,
                        Gender = gender,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                });
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid data",
                        errors = ModelState.Values.SelectMany(v => v.Errors)
                    });
                }

                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return BadRequest(new { success = false, message = "User not found" });
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return BadRequest(new { success = false, message = "Invalid password" });
                }

                
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("G7@!f4#Zq8&lN9^kP2*eR1$hW3%tX6@zB5");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                };
                

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(24),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    token = tokenString,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> LogoutUser()
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { success = false, message = "Token is required" });
                }
                
                var result = await _tokenRevocationService.RevokeTokenAsync(token);
                
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Token has already been revoked" });
                }

                return Ok(new { success = true, message = "Logout successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

    }
}