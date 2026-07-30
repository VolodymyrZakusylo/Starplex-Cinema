using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using StarPlex.Application.Common.Exceptions;

namespace StarPlex.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request processing.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                validationEx.Errors.Select(e => e.ErrorMessage).ToArray()
            ),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Resource not found.",
                new[] { notFoundEx.Message }
            ),

            KeyNotFoundException keyNotFoundEx => (
                StatusCodes.Status404NotFound,
                "Not found.",
                new[] { keyNotFoundEx.Message }
            ),

            InvalidOperationException invalidOpEx => (
                StatusCodes.Status409Conflict,
                "Conflict occurred.",
                new[] { invalidOpEx.Message }
            ),

            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Database conflict.",
                new[] { "Duplicate or invalid data detected in the database." }
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An internal server error occurred.",
                new[] { exception.Message }
            )
        };

        context.Response.StatusCode = statusCode;

        var responseObject = new
        {
            StatusCode = statusCode,
            Message = message,
            Errors = errors
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(responseObject));
    }
}