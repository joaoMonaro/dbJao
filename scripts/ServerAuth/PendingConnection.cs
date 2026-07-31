using System;

public sealed class PendingConnection
{
    public required int PeerId { get; init; }
    public ConnectionAuthState State { get; set; }
    public required DateTimeOffset ConnectedAt { get; init; }
    public required DateTimeOffset AuthenticationDeadline { get; init; }
    public string? CharacterId { get; set; }
}
