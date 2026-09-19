using System;

public sealed class CharacterDefinition
{
    public string Id { get; }
    public string Name { get; }
    public decimal AttackMultiplier { get; }
    public decimal DefenseMultiplier { get; }
    public decimal KiAttackMultiplier { get; }
    public string PlayerScenePath { get; }

    public CharacterDefinition(
        string id,
        string name,
        decimal attackMultiplier,
        decimal defenseMultiplier,
        decimal kiAttackMultiplier,
        string playerScenePath)
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
        if (string.IsNullOrWhiteSpace(playerScenePath))
            throw new ArgumentException("Cena do personagem é obrigatória.", nameof(playerScenePath));

        Id = id.Trim();
        Name = name.Trim();
        AttackMultiplier = attackMultiplier;
        DefenseMultiplier = defenseMultiplier;
        KiAttackMultiplier = kiAttackMultiplier;
        PlayerScenePath = playerScenePath.Trim();
    }
}
