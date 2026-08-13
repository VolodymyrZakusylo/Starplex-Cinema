using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;
using StarPlex.Application.Features.Auth.DTOs;

namespace StarPlex.API.Controllers;

/// <summary>
/// Handles authentication and user session management including registration, login, and token refreshing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IIdentityService identityService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _identityService = identityService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    /// <summary>
    /// Registers a new customer account.
    /// </summary>
    /// <param name="request">Registration data including email, password, and personal info.</param>
    /// <returns>Authentication result with access and refresh tokens.</returns>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _identityService.RegisterAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            cancellationToken);

        if (result == null)
        {
            return BadRequest(new { message = "Registration failed. Email might already be taken." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Authenticates a user and returns a token session.
    /// </summary>
    /// <param name="request">Login credentials (email and password).</param>
    /// <returns>Authentication result with access and refresh tokens.</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _identityService.LoginAsync(request.Email, request.Password, cancellationToken);

        if (result == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="request">The active refresh token string.</param>
    /// <returns>A new pair of access and refresh tokens.</returns>
    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var result = await _identityService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (result == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Revokes the specified refresh token, logging the user out of the current session.
    /// </summary>
    /// <param name="request">The refresh token to revoke.</param>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var result = await _identityService.RevokeTokenAsync(request.RefreshToken, cancellationToken);

        if (!result)
        {
            return BadRequest(new { message = "Token not found or already revoked." });
        }

        return Ok(new { message = "Successfully logged out." });
    }
}