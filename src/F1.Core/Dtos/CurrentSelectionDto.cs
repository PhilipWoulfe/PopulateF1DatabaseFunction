namespace F1.Core.Dtos;

public class CurrentSelectionDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string SelectionType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}