namespace GameBackend.Api.Authentication;

public interface IGameServerKeyValidator
{
    bool IsValid(string? providedKey);
}
