using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class CharacterSelectionManager(ApiClient apiClient, AuthManager authManager)
{
    private IReadOnlyList<CharacterApiResponse> _characters = [];

    public IReadOnlyList<CharacterApiResponse> Characters => _characters;
    public CharacterApiResponse? SelectedCharacter { get; private set; }

    public async Task<IReadOnlyList<CharacterApiResponse>> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        _characters = await apiClient.GetCharactersAsync(
            authManager.GetRequiredAccessToken(),
            cancellationToken);
        SelectedCharacter = _characters.Count > 0 ? _characters[0] : null;
        return _characters;
    }

    public async Task<CharacterApiResponse> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        CharacterApiResponse created = await apiClient.CreateCharacterAsync(
            authManager.GetRequiredAccessToken(),
            name,
            cancellationToken);
        await RefreshAsync(cancellationToken);
        Select(created.Id);
        return created;
    }

    public bool Select(Guid characterId)
    {
        foreach (CharacterApiResponse character in _characters)
        {
            if (character.Id != characterId)
                continue;
            SelectedCharacter = character;
            return true;
        }

        SelectedCharacter = null;
        return false;
    }

    public Task<CreateGameSessionApiResponse> CreateGameSessionAsync(
        CancellationToken cancellationToken = default)
    {
        CharacterApiResponse character = SelectedCharacter
            ?? throw new InvalidOperationException("Selecione um personagem.");
        return apiClient.CreateGameSessionAsync(
            authManager.GetRequiredAccessToken(),
            character.Id,
            cancellationToken);
    }
}
