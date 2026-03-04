using F1.Core.Models;

namespace F1.Core.Interfaces
{
    public interface IUserContext
    {
        User? GetCurrentUser();
    }
}
