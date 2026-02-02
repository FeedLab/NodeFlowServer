using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NodeFlow.Server;
using NodeFlow.Server.Contracts.Auth.Request;
using NodeFlow.Server.Contracts.Auth.Response;
using NodeFlow.Server.Data;
using NodeFlow.Server.Data.Entities;

namespace NodeFlow.Server.IntegrationTests.Auth;

public sealed class CreateUserTests
{
    [Fact]
    public async Task CreateUser_Persists_To_Database_And_Returns_Response()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new CreateUserRequest("alice", "alice@example.com", "P@ssw0rd!");
        var response = await client.PostAsJsonAsync("/auth/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        created.Should().NotBeNull();
        created!.UserName.Should().Be(request.UserName);
        created.Email.Should().Be(request.Email);
        created.Id.Should().NotBe(Guid.Empty);
        created.Location.Should().Be($"/auth/users/{created.Id}");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NodeFlowDbContext>();
        var users = await dbContext.Users.AsNoTracking().ToListAsync();

        users.Should().HaveCount(1);
        users[0].Email.Should().Be(request.Email);
        users[0].UserName.Should().Be(request.UserName);
        users[0].PasswordHash.Should().NotBeNullOrWhiteSpace();
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
