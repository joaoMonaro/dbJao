using GameBackend.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameBackend.Api.Data.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.IsConsumed).HasDefaultValue(false).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.IsConsumed });
        builder.HasOne(token => token.User)
            .WithMany(user => user.PasswordResetTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
