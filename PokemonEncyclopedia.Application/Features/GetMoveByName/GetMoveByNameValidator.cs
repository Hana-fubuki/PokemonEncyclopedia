using FluentValidation;

namespace PokemonEncyclopedia.Application.Features.GetMoveByName;

public class GetMoveByNameValidator : AbstractValidator<GetMoveByNameQuery>
{
    public GetMoveByNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Move name is required.")
            .MaximumLength(50)
            .WithMessage("Move name is too long.");
    }
}
