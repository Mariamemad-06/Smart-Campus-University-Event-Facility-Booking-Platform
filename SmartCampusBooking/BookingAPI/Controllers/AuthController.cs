using BookingAPI.Data;
using BookingAPI.DTOs;
using BookingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BookingAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly BookingDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthController(
            BookingDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var universityId = string.IsNullOrWhiteSpace(request.UniversityId)
                ? null
                : request.UniversityId.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict("Email is already registered.");

            if (universityId != null &&
                await _context.Users.AnyAsync(u => u.UniversityId == universityId))
                return Conflict("University ID is already registered.");

            if (request.Role != "Student" && request.Role != "Faculty")
                return BadRequest("Only Student and Faculty registration is allowed.");

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == request.Role);

            if (role == null)
                return BadRequest("Role is not configured.");

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                UniversityId = universityId,
                RoleId = role.RoleId
            };

            user.PasswordHash =
                _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is SqlException sqlException &&
                      (sqlException.Number == 2601 || sqlException.Number == 2627))
            {
                if (await _context.Users.AnyAsync(u => u.Email == email))
                    return Conflict("Email is already registered.");

                if (universityId != null &&
                    await _context.Users.AnyAsync(u => u.UniversityId == universityId))
                    return Conflict("University ID is already registered.");

                return Conflict("The account details are already registered.");
            }

            return Ok(new
            {
                Message = "Registration successful.",
                user.UserId,
                user.Email,
                Role = role.RoleName
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return Unauthorized("Invalid email or password.");

            var result = _passwordHasher.VerifyHashedPassword(
                user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid email or password.");

            var key = Convert.FromBase64String(
                _configuration["Jwt:Key"]!);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: credentials);

            return Ok(new
            {
                Message = "Login successful.",
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = token.ValidTo,
                user.UserId,
                user.FullName,
                user.Email,
                Role = user.Role.RoleName
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            return Ok(new
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Name = User.Identity?.Name,
                Role = User.FindFirstValue(ClaimTypes.Role)
            });
        }
     
        }
    }
}