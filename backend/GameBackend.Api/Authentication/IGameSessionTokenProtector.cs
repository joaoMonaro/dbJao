namespace GameBackend.Api.Authentication;

public interface IGameSessionTokenProtector
{
    string GenerateToken();
    string ComputeHash(string token);
}
