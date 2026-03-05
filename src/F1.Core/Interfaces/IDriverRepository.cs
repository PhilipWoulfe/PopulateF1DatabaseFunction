using F1.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace F1.Core.Interfaces
{
    public interface IDriverRepository
    {
        Task<List<Driver>> GetDriversAsync();
    }
}
