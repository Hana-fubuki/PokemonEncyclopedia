using FluentValidation;

namespace PokemonEncyclopedia.Application.Features.GetPokemonSpeciesByName;

public class GetPokemonSpeciesByNameValidator : AbstractValidator<GetPokemonSpeciesByNameQuery>
{
    public GetPokemonSpeciesByNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Species name is required.")
            .MaximumLength(50)
            .WithMessage("Species name is too long.");
    }
}
