namespace OrderMgmt.Application.Notifications.Models;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? Link,
    bool IsRead,
    DateTimeOffset CreatedAt);
