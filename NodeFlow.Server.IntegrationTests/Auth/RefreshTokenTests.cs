using System.Net;
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

public sealed class RefreshTokenTests
{
    [Fact]
    public async Task RefreshToken_Returns_New_Tokens_When_Valid()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // Create user and login
        var createRequest = new CreateUserRequest("charlie", "charlie@example.com", "P@ssw0rd!");
        await client.PostAsJsonAsync("/auth/users", createRequest);

        var loginRequest = new LoginRequest("charlie@example.com", "P@ssw0rd!");
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Wait a bit to ensure different timestamps in JWT
        await Task.Delay(1100);

        // Use refresh token to get new tokens
        var refreshRequest = new RefreshTokenRequest(loginResult!.RefreshToken);
        var refreshResponse = await client.PostAsJsonAsync("/auth/refresh", refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        refreshResult.Should().NotBeNull();
        refreshResult!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshResult.AccessToken.Should().NotBe(loginResult.AccessToken);
        refreshResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
        refreshResult.RefreshToken.Should().NotBe(loginResult.RefreshToken);
        refreshResult.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task RefreshToken_Returns_Unauthorized_When_Token_Invalid()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var refreshRequest = new RefreshTokenRequest("invalid-refresh-token");
        var refreshResponse = await client.PostAsJsonAsync("/auth/refresh", refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_Returns_BadRequest_When_Token_Empty()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var refreshRequest = new RefreshTokenRequest("");
        var refreshResponse = await client.PostAsJsonAsync("/auth/refresh", refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_Updates_RefreshToken_In_Database()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // Create user and login
        var createRequest = new CreateUserRequest("dave", "dave@example.com", "P@ssw0rd!");
        await client.PostAsJsonAsync("/auth/users", createRequest);

        var loginRequest = new LoginRequest("dave@example.com", "P@ssw0rd!");
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var oldRefreshToken = loginResult!.RefreshToken;

        // Use refresh token
        var refreshRequest = new RefreshTokenRequest(oldRefreshToken);
        var refreshResponse = await client.PostAsJsonAsync("/auth/refresh", refreshRequest);
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Verify database was updated
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NodeFlowDbContext>();
        var user = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Email == createRequest.Email);
        user.RefreshToken.Should().Be(refreshResult!.RefreshToken);
        user.RefreshToken.Should().NotBe(oldRefreshToken);
    }

    [Fact]
    public async Task RefreshToken_Returns_Unauthorized_When_Used_Twice()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // Create user and login
        var createRequest = new CreateUserRequest("eve", "eve@example.com", "P@ssw0rd!");
        await client.PostAsJsonAsync("/auth/users", createRequest);

        var loginRequest = new LoginRequest("eve@example.com", "P@ssw0rd!");
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var oldRefreshToken = loginResult!.RefreshToken;

        // Use refresh token first time (should succeed)
        var firstRefreshRequest = new RefreshTokenRequest(oldRefreshToken);
        var firstRefreshResponse = await client.PostAsJsonAsync("/auth/refresh", firstRefreshRequest);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to use the same refresh token again (should fail)
        var secondRefreshRequest = new RefreshTokenRequest(oldRefreshToken);
        var secondRefreshResponse = await client.PostAsJsonAsync("/auth/refresh", secondRefreshRequest);
        secondRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
