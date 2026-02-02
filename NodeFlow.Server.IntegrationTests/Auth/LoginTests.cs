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

public sealed class LoginTests
{
    [Fact]
    public async Task Login_Returns_Bearer_Token_And_Updates_LastLogin()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var createRequest = new CreateUserRequest("bob", "bob@example.com", "P@ssw0rd!");
        var createResponse = await client.PostAsJsonAsync("/auth/users", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginRequest = new LoginRequest("bob@example.com", "P@ssw0rd!");
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        login.Should().NotBeNull();
        login!.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.TokenType.Should().Be("Bearer");
        login.ExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NodeFlowDbContext>();
        var user = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Email == createRequest.Email);
        user.LastLoginUtc.Should().NotBeNull();
        user.LastLoginUtc.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
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
                    [$"{JwtOptions.SectionName}:{nameof(JwtOptions.AccessTokenMinutes)}"] = "5"
                });
            });
        }
    }
}
