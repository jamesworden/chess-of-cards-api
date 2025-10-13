using ChessOfCards.GameActionHandler.Requests;
using FluentValidation;

namespace ChessOfCards.GameActionHandler.Validators;

public class JoinGameRequestValidator : AbstractValidator<JoinGameRequest>
{
    public JoinGameRequestValidator()
    {
        RuleFor(x => x.GameCode)
            .NotEmpty()
            .WithMessage("GameCode is required")
            .Length(4, 6)
            .WithMessage("GameCode must be between 4 and 6 characters")
            .Matches("^[A-Z0-9]+$")
            .WithMessage("GameCode must contain only uppercase letters and numbers");

        RuleFor(x => x.GuestName)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.GuestName))
            .WithMessage("GuestName must not exceed 50 characters");
    }
}
