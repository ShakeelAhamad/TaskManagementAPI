using TaskManagementAPI.DTOs;

namespace TaskManagementAPI.Services
{
    public interface ITaskService
    {
        Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto createTask, int userId);
        Task<List<TaskResponseDto>> GetUserTaskAsync(int userId);
        Task<TaskResponseDto?> GetTaskByIdAsync(int taskId, int userId);
        Task<TaskResponseDto?> UpdateTaskAsync(int taskId,UpdateTaskDto updateTaskDto,int userId);
        Task<bool> DeleteTaskAsync(int taskId,int userId);
        Task<TaskResponseDto?> ToggleTaskCompletionAsync(int taskId,int userId);
 
        //New Admin/manager  methode
        Task<List<TaskResponseDto>> GetAllTasksAsync();//For Manager /Admin
        Task<TaskResponseDto?> GetAnyTaskByIdAsync(int taskId);//get any task regardless of owner
        Task<TaskResponseDto?> UpdateAnyTaskAsync(int taskId,UpdateTaskDto updateTaskDto);//Update any task
        Task<bool> DeleteAnyTaskAsync(int taskId);//Delete any task
    }
}
 