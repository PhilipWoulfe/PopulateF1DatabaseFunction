using F1.Core.Models;

namespace F1.Core.Interfaces
{
    public interface IDriverRepository
    {
        Task<List<Driver>> GetDriversAsync();
    }
}
