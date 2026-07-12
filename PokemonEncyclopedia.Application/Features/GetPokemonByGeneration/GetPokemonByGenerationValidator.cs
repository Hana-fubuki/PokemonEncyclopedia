using FluentValidation;

namespace PokemonEncyclopedia.Application.Features.GetPokemonByGeneration;

/// <summary>
///     Validates <see cref="GetPokemonByGenerationQuery" /> instances.
///     Ensures the generation number is present and within the supported range (1–9).
/// </summary>
public class GetPokemonByGenerationValidator : AbstractValidator<GetPokemonByGenerationQuery>
{
    public GetPokemonByGenerationValidator()
    {
        RuleFor(x => x.Generation)
            .NotEmpty()
            .WithMessage("Generation is required.")
            .InclusiveBetween(1, 9)
            .WithMessage("Generation must be between 1 and 9.");
    }
}
