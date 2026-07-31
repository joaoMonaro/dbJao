using Godot;

public partial class NetworkInterpolation2D : Node
{
    [Export] public NodePath VisualRootPath { get; set; } = new("../VisualRoot");
    [Export] public float InterpolationSpeed { get; set; } = 12.0f;
    [Export] public float TeleportThreshold { get; set; } = 120.0f;
    [Export] public float PositionSnapThreshold { get; set; } = 1.0f;
    [Export] public float InitializationDelay { get; set; } = 0.1f;

    public bool HasNetworkState { get; private set; }

    private Node2D? _entity;
    private Node2D? _visualRoot;
    private Vector2 _visualGlobalPosition;
    private Vector2 _lastOfficialPosition;
    private float _initializationRemaining;
    private bool _suspended;
    private bool _snapNextOfficialChange;
    private bool _initializationLogged;
    private bool _smoothingLogged;
    private bool _invalidStateLogged;

    public override void _Ready()
    {
        if (NetworkManager.RunningAsServer)
        {
            SetProcess(false);
            return;
        }

        _entity = GetParentOrNull<Node2D>();
        if (_entity is null)
        {
            GD.PushError($"[CLIENT][INTERPOLATION] Node físico pai ausente: {GetPath()}");
            SetProcess(false);
            return;
        }

        _visualRoot = GetNodeOrNull<Node2D>(VisualRootPath);
        if (_visualRoot is null)
        {
            GD.PushError(
                $"[CLIENT][INTERPOLATION] VisualRoot ausente em {_entity.GetPath()} "
                + $"(caminho configurado: {VisualRootPath})."
            );
            SetProcess(false);
            return;
        }

        int interpolationComponentCount = 0;
        foreach (Node child in _entity.GetChildren())
        {
            if (child is NetworkInterpolation2D)
                interpolationComponentCount++;
        }

        if (interpolationComponentCount > 1)
        {
            GD.PushError(
                $"[CLIENT][INTERPOLATION] Componente duplicado em {_entity.GetPath()}."
            );
            SetProcess(false);
            return;
        }

        if (
            InterpolationSpeed <= 0.0f
            || TeleportThreshold <= 0.0f
            || PositionSnapThreshold < 0.0f
        )
        {
            GD.PushError(
                $"[CLIENT][INTERPOLATION] Configuração inválida em {_entity.GetPath()}."
            );
            SetProcess(false);
            return;
        }

        _initializationRemaining = Mathf.Max(InitializationDelay, 0.0f);
        SnapToCurrentOfficialPosition();
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_entity) || !GodotObject.IsInstanceValid(_visualRoot))
        {
            SetProcess(false);
            return;
        }

        Vector2 officialPosition = _entity!.GlobalPosition;
        if (!IsFinite(officialPosition))
        {
            if (!_invalidStateLogged)
            {
                _invalidStateLogged = true;
                GD.PushError(
                    $"[CLIENT][INTERPOLATION] Posição oficial inválida ignorada: "
                    + $"{_entity.GetPath()} = {officialPosition}"
                );
            }
            return;
        }

        _invalidStateLogged = false;

        if (ShouldBypassInterpolation() || _suspended)
        {
            ApplyImmediatePosition(officialPosition);
            return;
        }

        if (!HasNetworkState || _initializationRemaining > 0.0f)
        {
            _initializationRemaining = Mathf.Max(
                _initializationRemaining - (float)delta,
                0.0f
            );
            ApplyImmediatePosition(officialPosition);

            if (_initializationRemaining <= 0.0f)
                CompleteInitialization();
            return;
        }

        bool officialPositionChanged =
            officialPosition.DistanceSquaredTo(_lastOfficialPosition) > 0.000001f;

        if (officialPositionChanged)
        {
            float visualCorrectionDistance = _visualGlobalPosition.DistanceTo(officialPosition);
            float officialStepDistance = _lastOfficialPosition.DistanceTo(officialPosition);

            if (
                _snapNextOfficialChange
                || visualCorrectionDistance > TeleportThreshold
                || officialStepDistance > TeleportThreshold
            )
            {
                ApplyImmediatePosition(officialPosition);
                _snapNextOfficialChange = false;
                GD.Print(
                    $"[CLIENT][INTERPOLATION] Snap aplicado: {GetEntityLabel()}"
                );
                return;
            }

            _lastOfficialPosition = officialPosition;
        }

        float distanceSquared = _visualGlobalPosition.DistanceSquaredTo(officialPosition);
        float snapThresholdSquared = PositionSnapThreshold * PositionSnapThreshold;
        if (distanceSquared <= snapThresholdSquared)
        {
            ApplyImmediatePosition(officialPosition);
            return;
        }

        float weight = 1.0f - Mathf.Exp(-InterpolationSpeed * (float)delta);
        weight = Mathf.Clamp(weight, 0.0f, 1.0f);
        _visualGlobalPosition = _visualGlobalPosition.Lerp(officialPosition, weight);

        if (!IsFinite(_visualGlobalPosition))
        {
            ApplyImmediatePosition(officialPosition);
            GD.PushError(
                $"[CLIENT][INTERPOLATION] Resultado inválido; snap de segurança: "
                + GetEntityLabel()
            );
            return;
        }

        _visualRoot!.GlobalPosition = _visualGlobalPosition;

        if (!_smoothingLogged)
        {
            _smoothingLogged = true;
            GD.Print(
                $"[CLIENT][INTERPOLATION] Suavização ativa: {GetEntityLabel()}"
            );
        }
    }

    public void SetSuspended(bool suspended, string reason)
    {
        if (NetworkManager.RunningAsServer || _suspended == suspended)
            return;

        _suspended = suspended;
        SnapToCurrentOfficialPosition();

        if (suspended)
        {
            _snapNextOfficialChange = true;
            return;
        }

        _snapNextOfficialChange = true;
        GD.Print(
            $"[CLIENT][INTERPOLATION] Estado resetado após {reason}: {GetEntityLabel()}"
        );
    }

    public void SnapToOfficial(string reason)
    {
        if (NetworkManager.RunningAsServer)
            return;

        SnapToCurrentOfficialPosition();
        _snapNextOfficialChange = true;
        GD.Print(
            $"[CLIENT][INTERPOLATION] Snap solicitado ({reason}): {GetEntityLabel()}"
        );
    }

    public void PrepareForRemoval()
    {
        HasNetworkState = false;
        _entity = null;
        _visualRoot = null;
        SetProcess(false);
    }

    private bool ShouldBypassInterpolation()
    {
        return _entity is Player player
            && player.OwnerPeerId == Multiplayer.GetUniqueId();
    }

    private void CompleteInitialization()
    {
        HasNetworkState = true;
        _snapNextOfficialChange = false;

        if (_initializationLogged || ShouldBypassInterpolation())
            return;

        _initializationLogged = true;
        GD.Print(
            $"[CLIENT][INTERPOLATION] Inicializado: {GetEntityLabel()}"
        );
    }

    private void SnapToCurrentOfficialPosition()
    {
        if (!GodotObject.IsInstanceValid(_entity) || !GodotObject.IsInstanceValid(_visualRoot))
            return;

        Vector2 officialPosition = _entity!.GlobalPosition;
        if (!IsFinite(officialPosition))
            return;

        ApplyImmediatePosition(officialPosition);
    }

    private void ApplyImmediatePosition(Vector2 officialPosition)
    {
        _visualGlobalPosition = officialPosition;
        _lastOfficialPosition = officialPosition;
        _visualRoot!.GlobalPosition = officialPosition;
    }

    private string GetEntityLabel()
    {
        if (_entity is Player player)
            return $"jogador remoto {player.OwnerPeerId}";

        return _entity is NpcBase ? $"NPC {_entity.Name}" : _entity?.Name ?? Name;
    }

    private static bool IsFinite(Vector2 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y);
    }
}
