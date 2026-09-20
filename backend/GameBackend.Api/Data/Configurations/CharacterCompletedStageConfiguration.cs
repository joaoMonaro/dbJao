using GameBackend.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameBackend.Api.Data.Configurations;

public sealed class CharacterCompletedStageConfiguration
    : IEntityTypeConfiguration<CharacterCompletedStage>
{
    public void Configure(EntityTypeBuilder<CharacterCompletedStage> builder)
    {
        builder.ToTable("character_completed_stages");
        builder.HasKey(completion => new { completion.CharacterId, completion.StageId });
        builder.Property(completion => completion.StageId).HasMaxLength(64).IsRequired();
        builder.Property(completion => completion.CompletedAt).IsRequired();
        builder.HasOne(completion => completion.Character)
            .WithMany(character => character.CompletedStages)
            .HasForeignKey(completion => completion.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_character_completed_stages_stage_id",
            "length(btrim(\"StageId\")) > 0"));
    }
}
