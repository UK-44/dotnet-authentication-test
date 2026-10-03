using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public record RegisterRequest(
    [Required, StringLength(32, MinimumLength = 3)] string UserName,
    [Required, StringLength(100, MinimumLength = 8)] string Password);

public record LoginRequest(
    [Required] string UserName,
    [Required] string Password);

public record UserResponse(int Id, string UserName);
