namespace F1.Web.Models
{
    public class User
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? Id { get; set; }
    }
}
