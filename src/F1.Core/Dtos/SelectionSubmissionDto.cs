using F1.Core.Models;

namespace F1.Core.Dtos;

public class SelectionSubmissionDto
{
    public List<SelectionPosition> OrderedSelections { get; set; } = [];
    
    public BetType BetType { get; set; }
}
