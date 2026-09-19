using Godot;
using System;
using System.Globalization;

public readonly record struct DebugXpCommandResult(
    bool Success,
    string Message,
    long GrantedXp = 0);

public static class DebugXpCommands
{
    public const int MaximumCommandLength = 128;
    private const string EnabledEnvironmentVariable = "DEBUG_COMMANDS_ENABLED";

    public static bool ServerCommandsEnabled =>
        OS.IsDebugBuild() && IsEnabledValue(
            System.Environment.GetEnvironmentVariable(EnabledEnvironmentVariable));

    public static DebugXpCommandResult Execute(
        Player player,
        string? commandText,
        bool commandsEnabled)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!commandsEnabled)
            return Failure("Comandos de debug estão desabilitados no servidor.");
        if (string.IsNullOrWhiteSpace(commandText))
            return Failure("Informe /addxp <quantidade>, /addxpnext ou /xpinfo.");
        if (commandText.Length > MaximumCommandLength)
            return Failure("Comando de debug excede o tamanho permitido.");

        string[] parts = commandText.Trim().Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string command = parts[0].ToLowerInvariant();

        return command switch
        {
            "/addxp" => ExecuteAddXp(player, parts),
            "/addxpnext" => parts.Length == 1
                ? ExecuteAddXpNext(player)
                : Failure("Uso: /addxpnext"),
            "/xpinfo" => parts.Length == 1
                ? GetXpInfo(player)
                : Failure("Uso: /xpinfo"),
            _ => Failure("Comando desconhecido. Use /addxp, /addxpnext ou /xpinfo."),
        };
    }

    private static DebugXpCommandResult ExecuteAddXp(Player player, string[] parts)
    {
        if (parts.Length != 2)
            return Failure("Uso: /addxp <quantidade>");

        if (!long.TryParse(
                parts[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long amount))
        {
            return Failure("Quantidade de XP inválida ou fora do limite de long.");
        }

        if (amount < 0)
            return Failure("A quantidade de XP não pode ser negativa.");
        if (amount == 0)
            return new(true, "Nenhum XP foi concedido: quantidade igual a zero.");

        return GrantXp(player, amount);
    }

    private static DebugXpCommandResult ExecuteAddXpNext(Player player)
    {
        ProgressionSnapshot snapshot;
        try
        {
            snapshot = Player.RebuildProgression(player.TotalXp);
        }
        catch (ArgumentException exception)
        {
            return Failure($"Estado de progressão inválido: {exception.Message}");
        }

        long missingXp = snapshot.XpRequiredForNextLevel - snapshot.XpIntoLevel;
        return GrantXp(player, missingXp);
    }

    private static DebugXpCommandResult GrantXp(Player player, long amount)
    {
        long previousTotalXp = player.TotalXp;
        int previousLevel = player.Level;
        long previousReset = player.Reset;

        if (!player.AddXp(amount))
            return Failure("O servidor não conseguiu conceder o XP solicitado.");

        return new(
            true,
            $"[DEBUG XP] +{amount} XP\n"
            + $"Level: {previousLevel} -> {player.Level}\n"
            + $"Reset: {previousReset} -> {player.Reset}\n"
            + $"TotalXp: {previousTotalXp} -> {player.TotalXp}",
            amount);
    }

    private static DebugXpCommandResult GetXpInfo(Player player)
    {
        ProgressionSnapshot snapshot;
        long globalLevel;
        try
        {
            snapshot = Player.RebuildProgression(player.TotalXp);
            globalLevel = Player.GetGlobalLevel(snapshot.State.Reset, snapshot.State.Level);
        }
        catch (ArgumentException exception)
        {
            return Failure($"Estado de progressão inválido: {exception.Message}");
        }

        long remainingXp = snapshot.XpRequiredForNextLevel - snapshot.XpIntoLevel;
        return new(
            true,
            "[XP INFO]\n"
            + $"TotalXp: {snapshot.State.TotalXp}\n"
            + $"Reset: {snapshot.State.Reset}\n"
            + $"Level: {snapshot.State.Level}\n"
            + $"GlobalLevel: {globalLevel}\n"
            + $"XP atual: {snapshot.XpIntoLevel} / {snapshot.XpRequiredForNextLevel}\n"
            + $"XP restante: {remainingXp}");
    }

    private static bool IsEnabledValue(string? value) =>
        string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value?.Trim(), "1", StringComparison.Ordinal);

    private static DebugXpCommandResult Failure(string message) => new(false, message);
}
