using FluentValidation;
using NodeFlow.Server.Contracts.Auth.Request;

namespace NodeFlow.Server.Contracts.Auth.Validation;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty();
    }
}
