namespace F1.Web.Models
{
    public class DevSettings
    {
        public bool SimulateCloudflare { get; set; }
        public string? MockEmail { get; set; }
        public string[] MockGroups { get; set; } = [];
        public bool InjectMockJwt { get; set; }
    }
}
