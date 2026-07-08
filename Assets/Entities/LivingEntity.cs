using UnityEngine;

public class LivingEntity : Entity
{
    public int MaxHealth { get; protected set; }
    public int CurrentHealth { get; protected set; }
    public int AttackDamage { get; private set; }

    public bool IsAlive
    {
        get { return CurrentHealth > 0; }
    }

    public LivingEntity(string name, int xPos, int yPos, int maxHealth, int attackDamage)
        : base(name, xPos, yPos)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        AttackDamage = attackDamage;
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount < 0)
        {
            damageAmount = 0;
        }

        CurrentHealth -= damageAmount;

        if (CurrentHealth < 0)
        {
            CurrentHealth = 0;
            //apply dead state
        }

        Debug.Log(Name + " took " + damageAmount + " damage. HP: " + CurrentHealth + "/" + MaxHealth);
    }

    public void Heal(int healAmount)
    {
        if (healAmount < 0)
        {
            healAmount = 0;
        }

        CurrentHealth += healAmount;

        if (CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
        }

        Debug.Log(Name + " healed " + healAmount + " HP. HP: " + CurrentHealth + "/" + MaxHealth);
    }
    public void Attack(LivingEntity target)
    {
        target.TakeDamage(AttackDamage);
    }
}