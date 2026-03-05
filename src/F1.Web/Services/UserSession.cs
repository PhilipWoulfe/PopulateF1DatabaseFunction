using F1.Web.Models;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace F1.Web.Services
{
    public class UserSession : IUserSession
    {
        private readonly HttpClient _httpClient;
        private const string MeEndpoint = "users/me";
        private User? _user;

        public User? User
        {
            get => _user;
            private set
            {
                _user = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(User)));
            }
        }

        public UserSession(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task InitializeAsync()
        {
            try
            {
                User = await _httpClient.GetFromJsonAsync<User>(MeEndpoint);
            }
            catch (HttpRequestException)
            {
                User = null;
            }
            catch (JsonException)
            {
                // Handles cases where the API returns HTML (e.g. 404 page) instead of JSON
                User = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
