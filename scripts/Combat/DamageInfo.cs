using Godot;

public enum DamageSourceType
{
    Player,
    Npc,
    Server,
}

public readonly struct DamageInfo
{
    public DamageInfo(
        DamageSourceType sourceType,
        string attackerId,
        int amount,
        Vector2 attackOrigin,
        long attackerPeerId = 0
    )
    {
        SourceType = sourceType;
        AttackerId = attackerId;
        Amount = amount;
        AttackOrigin = attackOrigin;
        AttackerPeerId = attackerPeerId;
    }

    public DamageSourceType SourceType { get; }
    public string AttackerId { get; }
    public int Amount { get; }
    public Vector2 AttackOrigin { get; }
    public long AttackerPeerId { get; }
}
