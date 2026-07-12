using FluentValidation;

namespace PokemonEncyclopedia.Application.Features.GetPokemonByName;

public class GetPokemonByNameValidator : AbstractValidator<GetPokemonByNameQuery>
{
    public GetPokemonByNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pokemon name is required.")
            .MaximumLength(50)
            .WithMessage("Pokemon name is too long.");
    }
}
