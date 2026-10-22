using System;
using UnityEngine;

public class LivingEntity : Entity
{
    public int MaxHealth { get; protected set; }
    public int CurrentHealth { get; protected set; }
    public int AttackDamage { get; protected set; }
    public int BaseAttackDamage { get; private set; }
    public int BaseArmor { get; private set; }
    public virtual int Armor { get { return BaseArmor; } }
    public int LastDamageTaken { get; private set; }
    public event Action<LivingEntity> AttackPerformed;
    public event Action<LivingEntity> DamageReceived;

    private ILivingState currentState;
    private readonly ILivingState aliveState;
    private readonly ILivingState deadState;

    public bool IsAlive
    {
        get { return CurrentHealth > 0; }
    }

    public LivingEntity(string name, int xPos, int yPos, int maxHealth, int attackDamage, int footprintWidth = 1, int footprintHeight = 1, int armor = 0)
        : base(name, xPos, yPos, footprintWidth, footprintHeight)
    {
        if (maxHealth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHealth), "Maximum health must be positive.");
        }

        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        AttackDamage = Math.Max(0, attackDamage);
        BaseAttackDamage = AttackDamage;
        BaseArmor = Math.Max(0, armor);
        aliveState = new AliveState(this);
        deadState = new DeadState(this);
        currentState = aliveState;
    }

    public virtual void TakeDamage(int damageAmount)
    {
        int before = CurrentHealth;
        currentState.TakeDamage(damageAmount);
        LastDamageTaken = before - CurrentHealth;
        if (LastDamageTaken > 0) DamageReceived?.Invoke(this);
    }

    public void Heal(int healAmount)
    {
        currentState.Heal(healAmount);
    }

    public virtual void Attack(LivingEntity target)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        bool performed = IsAlive && target.IsAlive;
        currentState.Attack(target);
        if (performed) AttackPerformed?.Invoke(this);
    }

    // States request changes; the entity keeps health within its bounds.
    internal void ModifyHealth(int amount)
    {
        long nextHealth = (long)CurrentHealth + amount;
        CurrentHealth = (int)Math.Max(0L, Math.Min((long)MaxHealth, nextHealth));
    }

    internal void EnterDeadState()
    {
        currentState = deadState;
        Debug.Log(Name + " died.");
    }
}
