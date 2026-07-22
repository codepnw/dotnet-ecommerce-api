using EcommerceAPI.Application.DTOs.Requests;
using FluentValidation;

namespace EcommerceAPI.Application.Validators;

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token is required")
            .Must(token => token.Split('.').Length == 3)
            .WithMessage("Invalid JWT format");
    }
}