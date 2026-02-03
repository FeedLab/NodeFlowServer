using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodeFlow.Server;
using NodeFlow.Server.Contracts.Auth.Request;
using NodeFlow.Server.Contracts.Auth.Response;
using NodeFlow.Server.Data;

namespace NodeFlow.Server.IntegrationTests.Auth;

public sealed class LogoutTests
{
    [Fact]
    public async Task Logout_Deletes_All_Sessions_From_Database()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // Create user and login
        var createRequest = new CreateUserRequest("frank", "frank@example.com", "P@ssw0rd!");
        await client.PostAsJsonAsync("/auth/users", createRequest);

        var loginRequest = new LoginRequest("frank@example.com", "P@ssw0rd!");
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Verify session exists in database
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NodeFlowDbContext>();
            var user = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Email == createRequest.Email);
            var sessions = await dbContext.Sessions.AsNoTracking().Where(s => s.UserId == user.Id).ToListAsync();
            sessions.Should().HaveCount(1);
            sessions[0].RefreshToken.Should().NotBeNullOrWhiteSpace();
        }

        // Logout with bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);
        var logoutResponse = await client.PostAsync("/auth/logout", null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify all sessions were deleted from database
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NodeFlowDbContext>();
            var user = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Email == createRequest.Email);
            var sessions = await dbContext.Sessions.AsNoTracking().Where(s => s.UserId == user.Id).ToListAsync();
            sessions.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Logout_Returns_Unauthorized_Without_Bearer_Token()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var logoutResponse = await client.PostAsync("/auth/logout", null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Returns_Unauthorized_With_Invalid_Token()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var logoutResponse = await client.PostAsync("/auth/logout", null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Prevents_RefreshToken_From_Being_Used()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // Create user and login
        var createRequest = new CreateUserRequest("grace", "grace@example.com", "P@ssw0rd!");
        await client.PostAsJsonAsync("/auth/users", createRequest);

        var loginRequest = new LoginRequest("grace@example.com", "P@ssw0rd!");
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var refreshToken = loginResult!.RefreshToken;

        // Logout
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);
        var logoutResponse = await client.PostAsync("/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to use refresh token after logout (should fail)
        client.DefaultRequestHeaders.Authorization = null;
        var refreshRequest = new RefreshTokenRequest(refreshToken);
        var refreshResponse = await client.PostAsJsonAsync("/auth/refresh", refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTests");
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDatabaseName"] = $"NodeFlowTests-{Guid.NewGuid():N}",
                    [$"{JwtOptions.SectionName}:{nameof(JwtOptions.Issuer)}"] = "NodeFlow.Tests",
                    [$"{JwtOptions.SectionName}:{nameof(JwtOptions.Audience)}"] = "NodeFlow.Tests",
                    [$"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}"] = "NodeFlowTestsSigningKey1234567890",
                    [$"{JwtOptions.SectionName}:{nameof(JwtOptions.AccessTokenMinutes)}"] = "5",
                    [$"{JwtOptions.SectionName}:{nameof(JwtOptions.RefreshTokenMinutes)}"] = "10080"
                });
            });
        }
    }
}
