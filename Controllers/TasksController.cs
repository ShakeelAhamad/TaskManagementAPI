using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagementAPI.Constants;
using TaskManagementAPI.DTOs;
using TaskManagementAPI.Extensions;
using TaskManagementAPI.Services;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] //This entire controller requires authentication
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        //Regular users , managers, and admins can create task
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            try
            {
                //Get the current user`s ID from the JWT token
                var userId = User.GetUserId();

                //Create the task
                var task = await _taskService.CreateTaskAsync(createTaskDto, userId);

                //return response
                return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
            }
            catch (UnauthorizedAccessException ex)
            {

                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500,new { message = "An error occurred while creating the task", details = ex.Message });
            }
        }

        //Regular users see only their tasks
        //Manager and Admins can see all tasks

        [HttpGet]
        public async Task<IActionResult> GetUserTask()
        {
            try
            {
                //Get the current user`s ID from the JWT token
                var userId = User.GetUserId();

                //Get the current user`s Role from the JWT token
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                List<TaskResponseDto> tasks;
                //Managers and Admin can see all tasks
                if(userRole == RoleConstants.Manager || userRole == RoleConstants.Admin)
                {
                    tasks = await _taskService.GetAllTasksAsync();//we'll add this methode
                }
                else
                {
                    //Regular users see only their own task
                    tasks = await _taskService.GetUserTaskAsync(userId);
                }
                return Ok(tasks);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the task", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        
        public async Task<IActionResult> GetTaskById(int id)
        {
            try
            {
                //Get the current user`s Id from the JWT toke
                var userId = User.GetUserId();
                //Get the current user`s Role from the JWT token
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                //Get the task (includes authorization check)
                var task = await _taskService.GetTaskByIdAsync(id, userId);
                if(task == null)
                {
                    //Mamagers and Admins can view any task
                    if (userRole == RoleConstants.Manager || userRole == RoleConstants.Admin)
                    {
                        task = await _taskService.GetAnyTaskByIdAsync(id);
                    }
                    if(task == null)
                    {
                        return NotFound(new { message = "Task not found or you don`t have permission to acccess it" });
                    }
                }
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {

                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the task", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto updateTaskDto) {
            try
            {
                //Get the current user`s Id from the JWT toke
                var userId = User.GetUserId();
                //Get the current user`s Role from the JWT token
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                //Only Admin can update other user tasks 
                TaskResponseDto? task;

                if(userRole == RoleConstants.Admin)
                {
                    task = await _taskService.UpdateAnyTaskAsync(id, updateTaskDto);//we'll add this
                }
                else
                {
                    task = await _taskService.UpdateTaskAsync(id, updateTaskDto, userId);
                }
                if(task == null)
                {
                    return NotFound(new { message = "Task not found or you don`t have permission to update it" });
                }
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {

                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the task", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")] //Explicit admin-only authorization
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                //Get the current user`s Id from the JWT toke
                var userId = User.GetUserId();
                var success = await _taskService.DeleteAnyTaskAsync(id);
                if (!success) 
                {
                    return NotFound(new { message = "Task not found or you don`t have permission to delete" });
                }
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the task", details = ex.Message });
            }

        }

        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleTaskCompletion(int id)
        {
            try
            {
                //Get the current user`s Id from the JWT toke
                var userId = User.GetUserId();
                var task   = await _taskService.ToggleTaskCompletionAsync(id, userId);
                if (task == null)
                {
                    return NotFound(new { message = "Task not found or you don`t have permission to modify it" });
                }
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {

                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while toggle the task", details = ex.Message });
            }
        }
    }
}
