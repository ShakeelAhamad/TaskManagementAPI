using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.DTOs
{
    public class RefreshTokenRequestDto
    {
        [Required(ErrorMessage ="Refresh token is requred")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
