namespace ToyBoxApi.DTOs.Notifications;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public int? ToyId { get; set; }
    public int? RequestId { get; set; }
    public int? GiftId { get; set; }
    public int? ActorUserId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
