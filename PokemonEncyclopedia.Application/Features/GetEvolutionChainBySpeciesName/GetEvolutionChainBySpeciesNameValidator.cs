using FluentValidation;

namespace PokemonEncyclopedia.Application.Features.GetEvolutionChainBySpeciesName;

public class GetEvolutionChainBySpeciesNameValidator : AbstractValidator<GetEvolutionChainBySpeciesNameQuery>
{
    public GetEvolutionChainBySpeciesNameValidator()
    {
        RuleFor(x => x.SpeciesName)
            .NotEmpty()
            .WithMessage("Species name is required.")
            .MaximumLength(50)
            .WithMessage("Species name is too long.");
    }
}
