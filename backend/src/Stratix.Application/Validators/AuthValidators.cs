using FluentValidation;
using Stratix.Application.DTOs.Auth;

namespace Stratix.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public class RegisterOrganizationRequestValidator : AbstractValidator<RegisterOrganizationRequest>
{
    public RegisterOrganizationRequestValidator()
    {
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdminName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Slug));
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}

public class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Department).MaximumLength(200);
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}
