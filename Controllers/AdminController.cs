using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Constants;
using TaskManagementAPI.Data;
using TaskManagementAPI.DTOs;
using TaskManagementAPI.Services;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]//Enter controller requred only Admin role
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        public AdminController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        //GET : api/admin/users - GET All Users
        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Role)
                    .Select(u => new UserResponseDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        CreatedAt = u.CreatedAt,
                        Role = u.Role.Name
                    }).ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new {message="An error occurred whil retrieving user",details = ex.Message});
            }
        }

        //GET : api/admin/users/{id} - Get specific user
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Tasks)
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.CreatedAt,
                        Role = u.Role.Name,
                        TaskCount = u.Tasks.Count
                    })
                    .FirstOrDefaultAsync();

                if(user == null)
                {
                    return NotFound(new { message = "User Not Found" });
                }
                return Ok(user);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { message = "An error occurred whil retrieving user", details = ex.Message });
            }
        }

        //POST : api/admin/users - create user with specific role
        [HttpPost("users")]
        public async Task<IActionResult> CreateUserWithRole([FromBody] RegisterDto registerDto)
        {
            try
            {
                //User the role provided or default the User
                var roleName = string.IsNullOrWhiteSpace(registerDto.Role) ? RoleConstants.User : registerDto.Role;
                var user = await _authService.RegisterWithRoleAsync(registerDto, roleName);
                return CreatedAtAction(nameof(GetUserById),new { id = user.Id},user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new {message = ex.Message});
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { message = "An error occurred whil retrieving user", details = ex.Message });
            }
        }

        //PUT : admin/users/{id}/role : user role change
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleDto changeRoleDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == id);
                if(user == null)
                {
                    return NotFound(new { message = "User Not Found" });
                }
                var newRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == changeRoleDto.RoleName);
                if(newRole == null)
                {
                    return BadRequest(new { message = $" '{changeRoleDto.RoleName}' dose not exist" });
                }
                user.RoleId = newRole.Id;
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = $"User '{user.Username}' role change to '{changeRoleDto.RoleName}'",
                    users = new UserResponseDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt,
                        Role = newRole.Name
                    }
                });
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { message = "An error occurred whil retrieving user", details = ex.Message });
            }
        }

        //DELETE : api/admin/user/{id} - Delete User
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users
                   .Include(u => u.Role)
                   .FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                {
                    return NotFound(new { message = "User Not Found" });
                }

                //Delete All User's Task first (cascade shoudl handle this , but being explict)
                _context.Tasks.RemoveRange(user.Tasks);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred whil retrieving user", details = ex.Message });
            }
        }

        //GET : api/admin/stats - Get system statistics
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            try
            {
                var totalUser = await _context.Users.CountAsync();
                var totalTaks = await _context.Tasks.CountAsync();
                var completedTasks = await _context.Tasks.CountAsync(t => t.IsCompleted);
                var pendingTasks = totalTaks - completedTasks;
                var usersByRole = await _context.Users
                                                .Include(u => u.Role)
                                                .GroupBy(u => u.Role.Name)
                                                .Select(g => new { Role = g.Key, Count = g.Count() })
                                                .ToListAsync();
                return Ok(new
                {
                    totalUser,
                    totalTaks,
                    completedTasks,
                    pendingTasks,
                    usersByRole
                });
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { message = "An error occurred whil retrieving user", details = ex.Message });
            }
        }
    }
}
