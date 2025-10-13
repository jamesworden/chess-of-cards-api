using ChessOfCards.GameActionHandler.Requests;
using FluentValidation;

namespace ChessOfCards.GameActionHandler.Validators;

public class CreatePendingGameRequestValidator : AbstractValidator<CreatePendingGameRequest>
{
    public CreatePendingGameRequestValidator()
    {
        RuleFor(x => x.DurationOption)
            .Must(BeValidDurationOption)
            .When(x => !string.IsNullOrWhiteSpace(x.DurationOption))
            .WithMessage("DurationOption must be one of: SHORT, MEDIUM, LONG");

        RuleFor(x => x.HostName)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.HostName))
            .WithMessage("HostName must not exceed 50 characters");
    }

    private static bool BeValidDurationOption(string? durationOption)
    {
        if (string.IsNullOrWhiteSpace(durationOption))
            return true;

        var validOptions = new[] { "SHORT", "MEDIUM", "LONG" };
        return validOptions.Contains(durationOption.ToUpper());
    }
}
