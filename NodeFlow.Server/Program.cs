using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using System.Text;
using NodeFlow.Server.Contracts.Auth.Validation;
using NodeFlow.Server.Data;
using NodeFlow.Server.Data.Repositories;
using NodeFlow.Server.Domain.Repositories;
using NodeFlow.Server.Endpoints;
using NodeFlow.Server.Endpoints.Auth;

namespace NodeFlow.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"[DEBUG_LOG] Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("[DEBUG_LOG] Token validated successfully");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"[DEBUG_LOG] Authentication challenge: {context.Error}, {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = signingKey
                };
            });

        builder.Services.AddDbContext<NodeFlowDbContext>(options =>
        {
            var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
            if (useInMemory)
            {
                var databaseName = builder.Configuration.GetValue<string>("InMemoryDatabaseName") ?? "NodeFlow";
                options.UseInMemoryDatabase(databaseName);
            }
            else
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.EnableRetryOnFailure();
                    sqlServerOptions.MigrationsAssembly("NodeFlow.Server.Data");
                });
            }
        });
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISessionRepository, SessionRepository>();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithOpenApiRoutePattern("/openapi/v1.json")
                    .WithTitle("NodeFlow API")
                    .WithTheme(ScalarTheme.DeepSpace)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    ;
            });
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapAuthEndpoints();
        app.MapWeatherForecast();

        app.Run();
    }
}
