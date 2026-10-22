using UnityEngine;

public class Enemy : LivingEntity
{
    public int AttackRange { get; private set; }
    public bool IsRanged { get { return AttackRange > 1; } }

    public Enemy(string name, int xPos, int yPos, int maxHealth, int attackDamage, int footprintWidth = 1, int footprintHeight = 1, int armor = 0, int attackRange = 1)
        : base(name, xPos, yPos, maxHealth, attackDamage, footprintWidth, footprintHeight, armor)
    {
        if (attackRange < 1) throw new System.ArgumentOutOfRangeException(nameof(attackRange));
        AttackRange = attackRange;
    }
}
