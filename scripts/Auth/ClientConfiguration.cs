using System;

public static class ClientConfiguration
{
    public static string ApiBaseUrl =>
        Read("API_BASE_URL", Read("GAME_API_URL", "http://127.0.0.1:5000"));
    public static int RequestTimeoutSeconds =>
        int.TryParse(Read("REQUEST_TIMEOUT_SECONDS", "10"), out int value)
            ? Math.Clamp(value, 1, 60)
            : 10;
    public static string EnvironmentName => Read("ENVIRONMENT", "Development");
    public static string Version => Read("CLIENT_VERSION", "0.1.0");

    private static string Read(string name, string fallback)
    {
        string? value = System.Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
