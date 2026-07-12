using FluentValidation;
using PokemonEncyclopedia.Application.Models;

namespace PokemonEncyclopedia.Application.Validators;

/// <summary>
///     Validates <see cref="GetPokemonByGenerationQuery" /> instances.
///     Ensures the generation number is present and within the supported range (1–9).
/// </summary>
public class GetPokemonByGenerationQueryValidator : AbstractValidator<GetPokemonByGenerationQuery>
{
    public GetPokemonByGenerationQueryValidator()
    {
        RuleFor(x => x.Generation)
            .NotEmpty()
            .WithMessage("Generation is required.")
            .InclusiveBetween(1, 9)
            .WithMessage("Generation must be between 1 and 9.");
    }
}
