namespace Backend.Models;

public class UserSession
{
    // Cookie に入るセッションキー（推測困難なランダム値）
    public required string Id { get; set; }
    public int UserId { get; set; }
    // TicketSerializer でシリアライズした AuthenticationTicket
    public required byte[] Ticket { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
