using GameBackend.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameBackend.Api.Data.Configurations;

public sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("characters");
        builder.HasKey(character => character.Id);

        builder.Property(character => character.Name)
            .HasColumnType("citext")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(character => character.Level)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(character => character.Experience)
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(character => character.CurrentHealth)
            .HasDefaultValue(100)
            .IsRequired();

        builder.Property(character => character.MaxHealth)
            .HasDefaultValue(100)
            .IsRequired();

        builder.Property(character => character.MapId)
            .HasMaxLength(64)
            .HasDefaultValue("kame_house")
            .IsRequired();

        builder.Property(character => character.PositionX).IsRequired();
        builder.Property(character => character.PositionY).IsRequired();
        builder.Property(character => character.CreatedAt).IsRequired();
        builder.Property(character => character.UpdatedAt).IsRequired();

        builder.HasIndex(character => new { character.UserId, character.Name }).IsUnique();
        builder.HasIndex(character => character.UserId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_characters_level", "\"Level\" >= 1");
            table.HasCheckConstraint("ck_characters_experience", "\"Experience\" >= 0");
            table.HasCheckConstraint("ck_characters_current_health", "\"CurrentHealth\" >= 0");
            table.HasCheckConstraint("ck_characters_max_health", "\"MaxHealth\" > 0");
            table.HasCheckConstraint(
                "ck_characters_health_range",
                "\"CurrentHealth\" <= \"MaxHealth\"");
        });
    }
}
