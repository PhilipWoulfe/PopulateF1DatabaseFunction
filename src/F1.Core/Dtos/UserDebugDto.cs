namespace F1.Core.Dtos
{
    public class UserDebugDto
    {
        public bool IsAuthenticated { get; set; }
        public bool IsAdmin { get; set; }
        public string AuthenticationType { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string AdminGroupClaimType { get; set; } = string.Empty;
        public IReadOnlyList<string> ConfiguredAdminGroups { get; set; } = [];
        public IReadOnlyList<string> Roles { get; set; } = [];
        public IReadOnlyList<string> Groups { get; set; } = [];
        public IReadOnlyList<UserDebugClaimDto> Claims { get; set; } = [];
    }

    public class UserDebugClaimDto
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}