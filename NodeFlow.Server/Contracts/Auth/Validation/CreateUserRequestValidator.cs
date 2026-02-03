using FluentValidation;
using NodeFlow.Server.Contracts.Auth.Request;

namespace NodeFlow.Server.Contracts.Auth.Validation;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.UserName).NotEmpty();
        RuleFor(request => request.Email).NotEmpty();
        RuleFor(request => request.Password).NotEmpty();
    }
}
