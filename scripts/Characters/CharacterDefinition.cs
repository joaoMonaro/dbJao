using System;

public sealed class CharacterDefinition
{
    public string Id { get; }
    public string Name { get; }
    public decimal AttackMultiplier { get; }
    public decimal DefenseMultiplier { get; }
    public decimal KiAttackMultiplier { get; }
    public decimal MaxHealthMultiplier { get; }
    public string PlayerScenePath { get; }
    public string PortraitTexturePath { get; }

    public CharacterDefinition(
        string id,
        string name,
        decimal attackMultiplier,
        decimal defenseMultiplier,
        decimal kiAttackMultiplier,
        decimal maxHealthMultiplier,
        string playerScenePath,
        string portraitTexturePath)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id do personagem é obrigatório.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do personagem é obrigatório.", nameof(name));
        if (attackMultiplier < 0)
            throw new ArgumentOutOfRangeException(nameof(attackMultiplier));
        if (defenseMultiplier < 0)
            throw new ArgumentOutOfRangeException(nameof(defenseMultiplier));
        if (kiAttackMultiplier < 0)
            throw new ArgumentOutOfRangeException(nameof(kiAttackMultiplier));
        if (maxHealthMultiplier < 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealthMultiplier));
        if (string.IsNullOrWhiteSpace(playerScenePath))
            throw new ArgumentException("Cena do personagem é obrigatória.", nameof(playerScenePath));
        if (string.IsNullOrWhiteSpace(portraitTexturePath))
        {
            throw new ArgumentException(
                "Portrait do personagem é obrigatório.",
                nameof(portraitTexturePath));
        }

        Id = id.Trim();
        Name = name.Trim();
        AttackMultiplier = attackMultiplier;
        DefenseMultiplier = defenseMultiplier;
        KiAttackMultiplier = kiAttackMultiplier;
        MaxHealthMultiplier = maxHealthMultiplier;
        PlayerScenePath = playerScenePath.Trim();
        PortraitTexturePath = portraitTexturePath.Trim();
    }
}
