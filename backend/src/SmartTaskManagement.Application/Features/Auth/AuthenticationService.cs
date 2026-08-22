using FluentValidation;
using FluentValidation.Results;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Features.Auth;

public sealed class AuthenticationService(
    IAuthStore authStore,
    IPasswordService passwordService,
    ITokenService tokenService,
    ISystemClock systemClock,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator)
{
    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(registerValidator, request, cancellationToken);

        var normalizedEmail = NormalizeEmail(request.Email);
        var existingUser = await authStore.FindUserByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (existingUser is not null)
        {
            throw new DuplicateEmailException();
        }

        var teamMemberRole = await authStore.FindRoleByNameAsync(
            RoleNames.TeamMember,
            cancellationToken);

        if (teamMemberRole is null)
        {
            throw new InvalidOperationException("The TeamMember role has not been seeded.");
        }

        var user = new User(
            request.Email,
            request.FirstName,
            request.LastName);

        user.SetPasswordHash(passwordService.HashPassword(user, request.Password));
        user.AssignRole(teamMemberRole);

        await authStore.AddUserAsync(user, cancellationToken);
        await authStore.SaveChangesAsync(cancellationToken);

        return ToUserResponse(user);
    }

    public async Task<AuthenticationResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(loginValidator, request, cancellationToken);

        var user = await authStore.FindUserByNormalizedEmailAsync(
            NormalizeEmail(request.Email),
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            !passwordService.VerifyPassword(user, request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var now = systemClock.UtcNow;
        var roleNames = GetRoleNames(user);
        var accessToken = tokenService.CreateAccessToken(user, roleNames);
        var rawRefreshToken = tokenService.CreateRefreshToken();
        var refreshToken = RefreshToken.Create(
            user.UserId,
            tokenService.HashRefreshToken(rawRefreshToken),
            Guid.NewGuid(),
            now,
            tokenService.GetRefreshTokenExpiry(now),
            ipAddress);

        user.RecordSuccessfulLogin();
        await authStore.AddRefreshTokenAsync(refreshToken, cancellationToken);
        await authStore.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            refreshToken.ExpiresAtUtc,
            ToUserResponse(user, roleNames));
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashRefreshToken(rawRefreshToken);
        var currentRefreshToken = await authStore.FindRefreshTokenByHashAsync(
            tokenHash,
            cancellationToken);

        if (currentRefreshToken is null || currentRefreshToken.User is null)
        {
            throw new InvalidRefreshTokenException();
        }

        var now = systemClock.UtcNow;

        if (!currentRefreshToken.IsActive(now) || !currentRefreshToken.User.IsActive)
        {
            if (currentRefreshToken.RevokedAtUtc is not null)
            {
                await authStore.RevokeRefreshTokenFamilyAsync(
                    currentRefreshToken.UserId,
                    currentRefreshToken.FamilyId,
                    now,
                    "Refresh token reuse detected",
                    ipAddress,
                    cancellationToken);
                await authStore.SaveChangesAsync(cancellationToken);
            }

            throw new InvalidRefreshTokenException();
        }

        var user = currentRefreshToken.User;
        var roleNames = GetRoleNames(user);
        var accessToken = tokenService.CreateAccessToken(user, roleNames);
        var rawReplacementToken = tokenService.CreateRefreshToken();
        var replacementToken = RefreshToken.Create(
            user.UserId,
            tokenService.HashRefreshToken(rawReplacementToken),
            currentRefreshToken.FamilyId,
            now,
            tokenService.GetRefreshTokenExpiry(now),
            ipAddress);

        currentRefreshToken.Revoke(
            now,
            "Rotated",
            ipAddress,
            replacementToken.RefreshTokenId);

        await authStore.AddRefreshTokenAsync(replacementToken, cancellationToken);
        await authStore.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            rawReplacementToken,
            replacementToken.ExpiresAtUtc,
            ToUserResponse(user, roleNames));
    }

    public async Task LogoutAsync(
        string? rawRefreshToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var refreshToken = await authStore.FindRefreshTokenByHashAsync(
            tokenService.HashRefreshToken(rawRefreshToken),
            cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive(systemClock.UtcNow))
        {
            return;
        }

        refreshToken.Revoke(systemClock.UtcNow, "Logged out", ipAddress);
        await authStore.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserResponse> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await authStore.FindUserByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UserNotFoundException();
        }

        return ToUserResponse(user);
    }

    private static async Task ValidateAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static IReadOnlyCollection<string> GetRoleNames(User user)
    {
        return user.UserRoles
            .Where(userRole => userRole.Role is not null)
            .Select(userRole => userRole.Role!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static UserResponse ToUserResponse(
        User user,
        IReadOnlyCollection<string>? roleNames = null)
    {
        return new UserResponse(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            roleNames ?? GetRoleNames(user));
    }
}
