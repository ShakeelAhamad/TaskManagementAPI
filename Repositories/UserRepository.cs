using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Repositories
{
    /// <summary>
    /// User-specific repository implementation.
    ///
    /// FROM GENERIC REPOSITORY (inherited, no need to rewrite):
    ///   AddAsync()         → adds new user in AuthService.RegisterAsync()
    ///   SaveChangesAsync() → saves user in AuthService.RegisterAsync()
    ///   AnyAsync()         → building block for EmailExistsAsync and UsernameExistsAsync
    ///   FirstOrDefaultAsync() → building block for GetByEmailAsync, GetUserWithRoleAsync
    ///
    /// SPECIFIC TO THIS REPOSITORY (added here):
    ///   GetByEmailAsync()           → needs Role eager loading for JWT generation
    ///   EmailExistsAsync()          → user-friendly existence check
    ///   UsernameExistsAsync()       → user-friendly existence check
    ///   GetUserWithRoleAsync()      → needs Role eager loading
    ///   GetUserWithRefreshTokensAsync() → needs both Role and RefreshTokens loaded
    /// </summary>
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
            
        }

        //--------------------------------------------------
        //USER-SPECIFIC IMPLEMENTATIONS
        //These use base class methods as building blocks
        //--------------------------------------------------

        ///<summary>
        ///Gets user by email with Role eagerly loaded.
        ///Role is always needed in AuthService for JWT token generation.
        ///Uses base : FirstOrDefaultAsync() with case-insensitive comparison.
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            //Include role because AuthService needs user.Role.Name for JWT claims
            return await _dbSet
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        ///<summary>
        ///Checks email uniqueness during registration
        ///More efficient than GetByEmail - stop as soon as match is found.
        ///Uses base : AnyAsync() as building block. 
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email)
        {
            //Uses base generic AnyAsync - no need to rewrite the DbSet logic
            return await AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        ///<summary>
        ///Checks username uniqueness during registration
        ///More efficient than GetByUsername - stop as soon as match is found.
        ///Uses base : AnyAsync() as building block. 
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            //Uses base generic AnyAsync - no need to rewrite the DbSet logic
            return await AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }

        ///<summary>
        ///Gets user withe role loaded after registration
        ///Role is needed to include in the registration response.
        ///Uses in : FirstOrDefaultAsync() with role included.
        /// </summary>
        public async Task<User?> GetUserWithRoleAsync(int userId) 
        {
            //Include Role because we need user.Role.Name in UserResponseDto
            return await _dbSet
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId);
        }

        ///<summary>
        ///Get user withe both role and RefreshTokens loded
        ///Needed for token refresh and revocation operations.
        /// Cannot use base.FirstOrDefaultAsync() because we need multiple Includes.
        /// </summary>
        public async Task<User?> GetUserWithRefreshTokensAsync(string refreshToken)
        {
            //Must write custome query here because we need TWO levels of eager loading.
            //1. Include Role -> needed for Jwt generation when refreshing.
            //2. Include RefreshToken -> needed to finde and validate the specific token.
            return await _dbSet
                         .Include(u => u.Role)
                         .Include(u => u.RefreshTokens)
                         .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken));
        }
    }
}
