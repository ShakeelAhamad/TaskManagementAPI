using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.DTOs
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 100 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        [Range(1,3, ErrorMessage = "Priority must be 1 (Low), 2 (Medium), or 3 (High)")]
        public int Priority { get; set; } = 1;
        public DateTime? DueDate { get; set; }

    }
}
