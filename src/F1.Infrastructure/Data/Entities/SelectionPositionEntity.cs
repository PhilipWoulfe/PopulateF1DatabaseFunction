namespace F1.Infrastructure.Data.Entities;

public class SelectionPositionEntity
{
    public int Id { get; set; }
    public Guid SelectionId { get; set; }
    public int Position { get; set; }
    public string DriverId { get; set; } = string.Empty;
}
