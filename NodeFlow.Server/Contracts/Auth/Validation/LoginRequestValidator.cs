using FluentValidation;
using NodeFlow.Server.Contracts.Auth.Request;

namespace NodeFlow.Server.Contracts.Auth.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Identifier).NotEmpty();
        RuleFor(request => request.Password).NotEmpty();
    }
}
