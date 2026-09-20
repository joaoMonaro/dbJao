using System;
using System.Collections.Generic;

public static class EnemyRegistry
{
    public const string WolfId = "wolf";
    public const string BearThiefId = "bear_thief";

    private static readonly IReadOnlyDictionary<string, EnemyDefinition> Definitions =
        new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal)
        {
            [WolfId] = new(
                WolfId, "Wolf", 1500, 0.80m, 0.60m, 1.00m,
                xpReward: 25, moveSpeed: 105, detectionRange: 480,
                attackRange: 62, attackCooldown: 1.2f),
            [BearThiefId] = new(
                BearThiefId, "Bear Thief", 5000, 1.00m, 0.90m, 5.00m,
                xpReward: 500, moveSpeed: 75, detectionRange: 620,
                attackRange: 82, attackCooldown: 1.5f),
        };

    public static EnemyDefinition Get(string id)
    {
        if (!string.IsNullOrWhiteSpace(id)
            && Definitions.TryGetValue(id.Trim(), out EnemyDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Definição de inimigo não encontrada: '{id}'.");
    }
}
