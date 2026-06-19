using FluentValidation;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Interfaces;
using LogisticsFlow.Application.Services;
using LogisticsFlow.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFAQService, FAQService>();
        services.AddScoped<IValidator<FAQRequestDto>, FAQRequestValidator>();
        services.AddScoped<IValidator<FAQResponseDto>, FAQResponseValidator>();
        return services;
    }
}