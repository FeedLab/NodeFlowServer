using FluentAssertions;
using NodeFlow.Server.Contracts.Auth.Request;
using NodeFlow.Server.Contracts.Auth.Validation;

namespace NodeFlow.Server.IntegrationTests.Auth;

public sealed class RequestValidatorTests
{
    [Fact]
    public void CreateUserRequestValidator_Fails_For_Empty_Fields()
    {
        var validator = new CreateUserRequestValidator();

        var result = validator.Validate(new CreateUserRequest(string.Empty, string.Empty, string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateUserRequest.UserName));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateUserRequest.Email));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateUserRequest.Password));
    }

    [Fact]
    public void CreateUserRequestValidator_Passes_For_Valid_Request()
    {
        var validator = new CreateUserRequestValidator();

        var result = validator.Validate(new CreateUserRequest("alice", "alice@example.com", "P@ssw0rd!"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginRequestValidator_Fails_For_Empty_Fields()
    {
        var validator = new LoginRequestValidator();

        var result = validator.Validate(new LoginRequest(string.Empty, string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(LoginRequest.Identifier));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void LoginRequestValidator_Passes_For_Valid_Request()
    {
        var validator = new LoginRequestValidator();

        var result = validator.Validate(new LoginRequest("alice@example.com", "P@ssw0rd!"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RefreshTokenRequestValidator_Fails_For_Empty_Token()
    {
        var validator = new RefreshTokenRequestValidator();

        var result = validator.Validate(new RefreshTokenRequest(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RefreshTokenRequest.RefreshToken));
    }

    [Fact]
    public void RefreshTokenRequestValidator_Passes_For_Valid_Request()
    {
        var validator = new RefreshTokenRequestValidator();

        var result = validator.Validate(new RefreshTokenRequest("refresh-token"));

        result.IsValid.Should().BeTrue();
    }
}
