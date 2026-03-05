namespace F1.Web.Models;

public class Driver
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public int? PermanentNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
}
