using Godot;
using System;

public sealed class GameConnectionManager : IDisposable
{
    public ENetMultiplayerPeer? Peer { get; private set; }

    public Error Connect(MultiplayerApi multiplayer, string host, int port)
    {
        Close();
        if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535)
            return Error.InvalidParameter;

        Peer = new ENetMultiplayerPeer();
        Error error = Peer.CreateClient(host.Trim(), port);
        if (error != Error.Ok)
        {
            Close();
            return error;
        }

        multiplayer.MultiplayerPeer = Peer;
        return Error.Ok;
    }

    public void Close()
    {
        Peer?.Close();
        Peer = null;
    }

    public void Dispose() => Close();
}
