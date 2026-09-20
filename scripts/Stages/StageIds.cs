using System;

public static class StageIds
{
    public const string BearThief = "bear_thief";

    public static bool IsValid(string? stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId) || stageId.Length > 64)
            return false;

        foreach (char character in stageId)
        {
            if (!(character is >= 'a' and <= 'z'
                || character is >= '0' and <= '9'
                || character == '_'))
            {
                return false;
            }
        }

        return true;
    }
}
