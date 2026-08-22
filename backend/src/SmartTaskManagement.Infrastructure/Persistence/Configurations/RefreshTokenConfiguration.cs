using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(refreshToken => refreshToken.RefreshTokenId);

        builder.Property(refreshToken => refreshToken.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.TokenHash)
            .IsUnique();

        builder.Property(refreshToken => refreshToken.CreatedByIp)
            .HasMaxLength(64);

        builder.Property(refreshToken => refreshToken.RevokedByIp)
            .HasMaxLength(64);

        builder.Property(refreshToken => refreshToken.RevocationReason)
            .HasMaxLength(200);

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(refreshToken => new
        {
            refreshToken.UserId,
            refreshToken.FamilyId,
            refreshToken.ExpiresAtUtc
        });
    }
}
