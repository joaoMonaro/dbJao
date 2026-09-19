using System;
using System.Collections.Generic;

public static class CharacterRegistry
{
    public const string DefaultCharacterId = "goku";

    private static readonly IReadOnlyDictionary<string, CharacterDefinition> Definitions =
        new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal)
        {
            [DefaultCharacterId] = new(
                DefaultCharacterId,
                "Goku",
                1.00m,
                1.00m,
                1.00m,
                "res://scenes/Player.tscn"),
        };

    public static CharacterDefinition Default => Definitions[DefaultCharacterId];
    public static IEnumerable<CharacterDefinition> All => Definitions.Values;

    public static bool Contains(string? id) =>
        !string.IsNullOrWhiteSpace(id) && Definitions.ContainsKey(id.Trim());

    public static bool TryGet(string? id, out CharacterDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(id)
            && Definitions.TryGetValue(id.Trim(), out CharacterDefinition? found))
        {
            definition = found;
            return true;
        }

        definition = null!;
        return false;
    }

    public static CharacterDefinition Get(string id)
    {
        if (TryGet(id, out CharacterDefinition definition))
            return definition;

        throw new KeyNotFoundException($"Personagem configurável não encontrado: '{id}'.");
    }

    public static CharacterDefinition ResolveOrDefault(string? id, out bool usedFallback)
    {
        if (TryGet(id, out CharacterDefinition definition))
        {
            usedFallback = false;
            return definition;
        }

        usedFallback = true;
        return Default;
    }
}
