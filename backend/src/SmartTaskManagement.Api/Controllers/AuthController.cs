using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.Api.Models;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Auth;
using SmartTaskManagement.Domain.Constants;

namespace SmartTaskManagement.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    AuthenticationService authenticationService) : ControllerBase
{
    private const string RefreshTokenCookieName = "smart-task-refresh-token";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authenticationService.RegisterAsync(request, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponseFactory.Success(HttpContext, user, "User registered successfully."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthenticationResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(
            request,
            GetIpAddress(),
            cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);

        return Ok(ApiResponseFactory.Success(
            HttpContext,
            ToAuthenticationResponse(result),
            "Login successful."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthenticationResponse>>> Refresh(
        CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var rawRefreshToken) ||
            string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new InvalidRefreshTokenException();
        }

        var result = await authenticationService.RefreshAsync(
            rawRefreshToken,
            GetIpAddress(),
            cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);

        return Ok(ApiResponseFactory.Success(
            HttpContext,
            ToAuthenticationResponse(result),
            "Access token refreshed successfully."));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object?>>> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshTokenCookieName, out var rawRefreshToken);

        await authenticationService.LogoutAsync(
            rawRefreshToken,
            GetIpAddress(),
            cancellationToken);

        DeleteRefreshTokenCookie();

        return Ok(ApiResponseFactory.Success<object?>(
            HttpContext,
            null,
            "Logout successful."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseFactory.Failure<UserResponse>(
                HttpContext,
                "Authentication failed.",
                [new ApiError("INVALID_USER_CLAIM", "The access token does not contain a valid user ID.")]));
        }

        return Ok(ApiResponseFactory.Success(
            HttpContext,
            await authenticationService.GetCurrentUserAsync(userId, cancellationToken),
            "Current user retrieved successfully."));
    }

    [HttpGet("admin-check")]
    [Authorize(Policy = "AdminOnly")]
    public ActionResult<ApiResponse<object>> AdminCheck()
    {
        return Ok(ApiResponseFactory.Success<object>(
            HttpContext,
            new
            {
                authorized = true,
                role = RoleNames.Admin
            },
            "Authorization check successful."));
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth",
                Expires = expiresAtUtc
            });
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth"
            });
    }

    private static AuthenticationResponse ToAuthenticationResponse(AuthenticationResult result)
    {
        return new AuthenticationResponse(
            result.AccessToken,
            result.AccessTokenExpiresAtUtc,
            result.User);
    }
}
