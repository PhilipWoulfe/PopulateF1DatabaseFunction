using F1.Core.Models;

namespace F1.Core.Dtos;

public class SelectionSubmissionDto
{
    public List<string> Selections { get; set; } = [];
    public BetType BetType { get; set; }
}
