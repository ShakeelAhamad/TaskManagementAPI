namespace TaskManagementAPI.DTOs
{
    public class LoginResponseDto
    {
        public string AccessToken {  get; set; } = string.Empty;
        public string RefreshToken {  get; set; } = string.Empty;
        public UserResponseDto User { get; set; } = null!;
        public DateTime AccessTokenExpiresAt {  get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
