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
        // Phase 1 — FAQ
        services.AddScoped<IFAQService, FAQService>();
        services.AddScoped<IValidator<FAQRequestDto>, FAQRequestValidator>();
        services.AddScoped<IValidator<FAQResponseDto>, FAQResponseValidator>();

        // Phase 2 — Quotation
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<IValidator<QuotationRequestDto>, QuotationRequestValidator>();
        services.AddScoped<IValidator<QuotationResponseDto>, QuotationResponseValidator>();

        // Phase 3 — Tracking
        services.AddScoped<ITrackingService, TrackingService>();
        services.AddScoped<IValidator<TrackingRequestDto>, TrackingRequestValidator>();
        services.AddScoped<IValidator<TrackingResponseDto>, TrackingResponseValidator>();

        return services;
    }
}
