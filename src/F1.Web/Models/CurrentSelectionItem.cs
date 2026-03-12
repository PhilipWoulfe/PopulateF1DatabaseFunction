namespace F1.Web.Models;

public class CurrentSelectionItem
{
    public int Position { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string SelectionType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}