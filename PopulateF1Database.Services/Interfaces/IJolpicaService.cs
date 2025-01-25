using System.Threading.Tasks;

namespace PopulateF1Database.Services.Interfaces
{
    public interface IJolpicaService
    {
        Task<string> GetDataAsync(string endpoint);
    }
}
