using UnityEngine;

public class Enemy : LivingEntity
{

    public Enemy(string name, int xPos, int yPos, int maxHealth, int attackDamage)
        : base(name, xPos, yPos, maxHealth, attackDamage)
    {
    }
}
