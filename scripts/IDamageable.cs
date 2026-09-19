public interface IDamageable
{
    HealthComponent Health { get; }
    long Defense { get; }
    bool ApplyServerDamage(DamageInfo damageInfo);
}
