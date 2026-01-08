
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Employee_Assignment.DTOs.Auth;
using Xunit;

namespace Employee_Assignment.IntegrationTests.Setup
{
    // Define a collection to ensure tests don't run in parallel
    [CollectionDefinition("Integration Tests")]
    public class IntegrationTestCollection : ICollectionFixture<TestWebApplicationFactory<Program>>
    {
    }

    [Collection("Integration Tests")]
    public class IntegrationTestBase : IDisposable
    {
        protected readonly HttpClient Client;
        protected readonly TestWebApplicationFactory<Program> Factory;

        public IntegrationTestBase(TestWebApplicationFactory<Program> factory)
        {
            Factory = factory;
            Factory.ResetDatabase(); // Reset before each test
            Client = factory.CreateClient();
        }

        protected async Task<string> GetAuthTokenAsync(string email = "admin@test.com", string password = "Admin123!")
        {
            var loginDto = new LoginDto
            {
                Email = email,
                Password = password
            };

            var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            response.EnsureSuccessStatusCode();

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return authResponse!.Token;
        }

        protected void SetAuthToken(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        protected async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await Client.PostAsync(url, content);
        }

        protected async Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await Client.PutAsync(url, content);
        }

        // Helper to generate unique emails for tests
        protected string GetUniqueEmail(string prefix = "test")
        {
            return $"{prefix}{Guid.NewGuid():N}@test.com";
        }

        public void Dispose()
        {
            Client?.Dispose();
        }
    }
}