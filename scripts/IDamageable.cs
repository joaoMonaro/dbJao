public interface IDamageable
{
    HealthComponent Health { get; }
    bool ApplyServerDamage(DamageInfo damageInfo);
}
