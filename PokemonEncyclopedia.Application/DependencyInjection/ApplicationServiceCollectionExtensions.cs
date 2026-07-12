using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PokemonEncyclopedia.Application.Models;
using PokemonEncyclopedia.Application.Validators;
using PokemonEncyclopedia.Application.Validators.Behavior;

namespace PokemonEncyclopedia.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(typeof(GetPokemonByGenerationQuery).Assembly);
        services.AddValidatorsFromAssemblyContaining<GetPokemonByGenerationQueryValidator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
