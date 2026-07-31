using GameBackend.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameBackend.Api.Data.Configurations;

public sealed class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.ToTable("game_sessions");
        builder.HasKey(session => session.Id);

        builder.Property(session => session.TokenHash)
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();
        builder.Property(session => session.ExpiresAt).IsRequired();
        builder.Property(session => session.CreatedAt).IsRequired();
        builder.Property(session => session.IsConsumed).HasDefaultValue(false).IsRequired();
        builder.Property(session => session.GameServerId).HasMaxLength(64);

        builder.HasIndex(session => session.TokenHash).IsUnique();
        builder.HasIndex(session => new
        {
            session.UserId,
            session.IsConsumed,
            session.ExpiresAt,
        });
        builder.HasIndex(session => new
        {
            session.CharacterId,
            session.IsConsumed,
            session.ExpiresAt,
        });

        builder.HasOne(session => session.User)
            .WithMany(user => user.GameSessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(session => session.Character)
            .WithMany(character => character.GameSessions)
            .HasForeignKey(session => session.CharacterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
