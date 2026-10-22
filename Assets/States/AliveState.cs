using System;
using UnityEngine;

public class AliveState : ILivingState
{
    private readonly LivingEntity host;

    public AliveState(LivingEntity host)
    {
        this.host = host;
    }

    public void Attack(LivingEntity target)
    {
        target.TakeDamage(host.AttackDamage);
    }

    public void TakeDamage(int damage)
    {
        damage = Math.Max(0, Math.Max(0, damage) - host.Armor);
        host.ModifyHealth(-damage);
        Debug.Log(host.Name + " took " + damage + " damage. HP: " + host.CurrentHealth + "/" + host.MaxHealth);

        if (host.CurrentHealth == 0)
        {
            host.EnterDeadState();
        }
    }

    public void Heal(int amount)
    {
        amount = Math.Max(0, amount);
        host.ModifyHealth(amount);
        Debug.Log(host.Name + " healed " + amount + " HP. HP: " + host.CurrentHealth + "/" + host.MaxHealth);
    }
}
