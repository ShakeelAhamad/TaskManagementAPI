using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskManagementAPI.Constants;
using TaskManagementAPI.Data;
using TaskManagementAPI.DTOs;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories;

namespace TaskManagementAPI.Services
{
    public class AuthService : IAuthService
    { 
        //Repositories replace direct DbContect usage.
        //UserRepository -> all user-related DB operation
        //RefreshTokenRepository -> all token-related Db operation.
        //_context kept only for role lookup (no RoleRepository in this implementation)
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ApplicationDbContext context, 
            IConfiguration configuration
        )
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _context = context;
            _configuration = configuration;
        }

        public async Task<UserResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            return await RegisterWithRoleAsync(registerDto,RoleConstants.User);//Default to "User" role
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto,string ipAddress) {
            //Find User by Email
            //UserRepository.GetByEmailAsync()
            //Uses base.FirstOrDefaultAsync() with Role eagerly loaded
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);


            //If Check User Exists
            if(user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            //Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password,user.PasswordHash);

            //Check Password
            if (!isPasswordValid) {
                throw new UnauthorizedAccessException("Invalid email or password"); 
            }

            //Generate JWT Token
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(ipAddress);
            //Save refresh token to database
            refreshToken.UserId = user.Id;

            //Uses base generic AddAsync() from Repository<RefreshToke>
            await _refreshTokenRepository.AddAsync(refreshToken);


            //Remove old/incative refresh tokens for this users (optional cleanup)
            //RefreshTokenRepository.RemoveOldTokensAsync()
            //Uses base.RemoveRange() internally for cleanup
            await _refreshTokenRepository.RemoveOldTokensAsync(user.Id);

            //Uses base generic SaveChangeAsync() from Repository<RefreshToke>
            await _refreshTokenRepository.SaveChangesAsync();
            
            var accessTokenExpiry= DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"]!));

            //Return login Response
            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                User  = new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.Username!, 
                    Email = user.Email!,
                    CreatedAt = user.CreatedAt,
                    Role = user.Role.Name //Include the role
                },
                AccessTokenExpiresAt = accessTokenExpiry,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            }; 

        }

        public async Task<UserResponseDto> RegisterWithRoleAsync(RegisterDto registerDto,string roleName)
        {
            //Check if user already exists
            var email    = registerDto.Email.Trim().ToLower();
            var username = registerDto.Username.Trim().ToLower();

            //UserRepository.EmailExistsAsync()
            //user base.AnyAsync() internally
            if(await _userRepository.EmailExistsAsync(email))
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            //UserRepository.UsernameExistsAsync()
            //user base.AnyAsync() internally
            if(await _userRepository.UsernameExistsAsync(username))
            {
                throw new InvalidOperationException("User with this username already exists");
            }
            

            //Role lookup still user _context directly
            //We don't have a RoleRepository
             roleName = !string.IsNullOrEmpty(registerDto.Role) ? registerDto.Role : RoleConstants.User;
            //Get the role
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if(role == null)
            {
                throw new InvalidOperationException("Default user role not found. Please ensure roles are seeded");
            }

            //Has the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            //Create New User
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                RoleId = role.Id,//Assign role
                CreatedAt = DateTime.UtcNow
            };
            //Add to Database
            //Uses base generic AddAsync() from Repository<User>
            await _userRepository.AddAsync(user);
            //Uses base generic SaveChangesAsync() from Repository<User>
            await _userRepository.SaveChangesAsync();

            //UserRepository.GetUserWithRoleAsync
            //Uses base.FirstOrDefaultAsync() with Role include
            var userWithRole = await _userRepository.GetUserWithRoleAsync(user.Id);


            //Return response DTO (no password)
            return new UserResponseDto
            {
                Id = userWithRole!.Id,
                Username = userWithRole.Username,
                Email = userWithRole.Email,
                CreatedAt = userWithRole.CreatedAt,
                Role = userWithRole.Role.Name //Include the role name
            };
        }
        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(string token, string ipAddresh)
        {
            //UserRepository.GetUserWitheRefreshTokenAsync()
            //Custome Query - needs the user + Role + RefreshTokens loaded
            var user = await _userRepository.GetUserWithRefreshTokensAsync(token);
            if(user == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }
            var refreshToken = user.RefreshTokens.Single(rt => rt.Token == token);
            if (!refreshToken.IsActive)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            //Generate New tokens
            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToke = GenerateRefreshToken(ipAddresh);
            newRefreshToke.UserId = user.Id;

            //Mark old refresh token as replaced
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddresh;
            refreshToken.ReplacedByToken = newRefreshToke.Token;

            //Uses base generic  Update() from Repository<RefreshToken>
            _refreshTokenRepository.Update(refreshToken);

            //Save New Refresh Token
            //Uses base generic AddAsync() from Repository<RefreshToken>
           await _refreshTokenRepository.AddAsync(newRefreshToke);


            //Remove old refreh token
            //RefreshTokenRepository.RemoveOldTokensAsync()
            // Use base.RemoveRange() internally for cleanup
            await _refreshTokenRepository.RemoveOldTokensAsync(user.Id);
            //Uses base generic SaveChangesAsync() from Repository<RefreshToken>
            await _refreshTokenRepository.SaveChangesAsync();

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"]!));
            return new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToke.Token,
                AccessTokenExpiresAt = accessTokenExpiry,
                RefreshTokenExpiresAt = newRefreshToke.ExpiresAt
            };

        } 
        public async Task<bool> RevokeTokenAsync(string token, string ipAddresh) 
        {
            //UserRepository.GetUserWithRefreshTokensAsync()
            //Custom query - needs User + RefreshTokens loaded
            var user = await _userRepository.GetUserWithRefreshTokensAsync(token);
            if(user == null)
            {
                return false;
            }
            var refreshToken = user.RefreshTokens.Single(rt => rt.Token == token);
            if (!refreshToken.IsActive)
            {
                return false;
            }
            //Revoke token
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddresh;
            //Uses base generic Update() from Repository<RefreshToken>
             _refreshTokenRepository.Update(refreshToken);
            //Uses base generic SaveChangesAsync() from Repository<RefreshToken>
            await _refreshTokenRepository.SaveChangesAsync();
            return true;
        }

        private string GenerateAccessToken(User user)
        {
            //Get JWT Settings from configuration
            var secretKey = _configuration["JwtSettings:SecretKey"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audienc"];
            var expiryMinutes = double.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"]!);

            //Create security key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //Define claims (user information to include in token)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email,user.Email!),
                new Claim(JwtRegisteredClaimNames.UniqueName,user.Username!),
                new Claim(ClaimTypes.Role,user.Role.Name),//Add role claim
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
            };

            //Create Token
            var token = new JwtSecurityToken(
                issuer:issuer,
                audience:audience,
                claims:claims,
                expires:DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials:credentials
                );

            //Return Token as string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            var refreshTokenExpiry = double.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"]!);
            //Generate cryptographically secure random token
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var token = Convert.ToBase64String(randomBytes);
            return new RefreshToken
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiry),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

        }
        private async Task RemoveOldRefreshTokens(int userId)
        {
            //Remove refresh tokens that are expired or revoked and older than 30 days
            var tokenToRemove = await _context.RefreshTokens
                .Where(rt => 
                rt.UserId == userId &&
               // !rt.IsActive && 
                rt.CreatedAt.AddDays(30) < DateTime.UtcNow)
                .ToListAsync();
            if (tokenToRemove.Any())
            {
                _context.RefreshTokens.RemoveRange(tokenToRemove);
            }
        }
        private async Task<User> GetUserByRefreshToken(string token)
        {
            var user = await _context.Users
                                    .Include(u => u.Role)
                                    .Include(u => u.RefreshTokens)
                                    .SingleOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == token));

            if(user == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            return user;
        }

    }
}
