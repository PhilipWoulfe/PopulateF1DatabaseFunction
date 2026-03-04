using F1.Web.Models;
using System.ComponentModel;

namespace F1.Web.Services
{
    public interface IUserSession : INotifyPropertyChanged
    {
        User? User { get; }
        Task InitializeAsync();
    }
}
