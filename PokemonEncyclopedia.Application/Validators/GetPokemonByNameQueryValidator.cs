using FluentValidation;
using PokemonEncyclopedia.Application.Models;

namespace PokemonEncyclopedia.Application.Validators;

public class GetPokemonByNameQueryValidator : AbstractValidator<GetPokemonByNameQuery>
{
    public GetPokemonByNameQueryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pokemon name is required.")
            .MaximumLength(50)
            .WithMessage("Pokemon name is too long.");
    }
}
