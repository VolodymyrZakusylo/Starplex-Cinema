using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Exceptions;

namespace StarPlex.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        ProblemDetails problem;

        switch (exception)
        {
            case FluentValidation.ValidationException validationException:

                _logger.LogWarning("Validation failed: {Message}", validationException.Message);

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed.",
                    Detail = "One or more validation errors occurred."
                };

                problem.Extensions["errors"] = validationException.Errors
                    .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                    .ToDictionary(g => g.Key, g => g.ToArray());

                break;

            case NotFoundException notFound:

                _logger.LogWarning("Resource not found: {Message}", notFound.Message);

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found.",
                    Detail = notFound.Message
                };

                break;

            case ConflictException conflict:

                _logger.LogWarning("Conflict: {Message}", conflict.Message);

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict.",
                    Detail = conflict.Message
                };

                break;

            case BusinessRuleException business:

                _logger.LogWarning("Business rule violation: {Message}", business.Message);

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Business rule violation.",
                    Detail = business.Message
                };

                break;

            case UnauthorizedException unauthorized:

                _logger.LogWarning("Unauthorized: {Message}", unauthorized.Message);

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized.",
                    Detail = unauthorized.Message
                };

                break;

            case ForbiddenException forbidden:

                _logger.LogWarning("Forbidden: {Message}", forbidden.Message);

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden.",
                    Detail = forbidden.Message
                };

                break;

            default:

                _logger.LogError(exception, "Unhandled exception occurred while processing request.");

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while processing your request."
                };

                break;
        }

        context.Response.StatusCode = problem.Status!.Value;

        await context.Response.WriteAsJsonAsync(problem);
    }
}