using System.Net;
using System.Text.Json;
using CreditCase.Application.Exceptions;
using FluentValidation;

#pragma warning disable CA1812 // instantiated via DI

namespace CreditCase.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (NotFoundException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.NotFound, "NotFound", ex.Message);
        }
        catch (LoanApplicationDeniedException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.UnprocessableEntity, "LoanDenied", ex.Message);
        }
        catch (InsufficientCreditScoreException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.UnprocessableEntity, "InsufficientCreditScore", ex.Message);
        }
        catch (ExcessiveDebtRatioException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.UnprocessableEntity, "ExcessiveDebtRatio", ex.Message);
        }
        catch (InvalidCustomerProfileException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.UnprocessableEntity, "InvalidCustomerProfile", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.UnprocessableEntity, "BusinessRuleViolation", ex.Message);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(
                new { type = "ValidationError", message = "One or more validation errors occurred.", errors },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "InternalServerError", "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string type, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(
            new { type, message },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        ));
    }
}
