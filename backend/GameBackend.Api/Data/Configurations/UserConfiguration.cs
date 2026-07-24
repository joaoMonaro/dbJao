using GameBackend.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameBackend.Api.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Username)
            .HasColumnType("citext")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnType("citext")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();

        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();

        builder.HasMany(user => user.Characters)
            .WithOne(character => character.User)
            .HasForeignKey(character => character.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
