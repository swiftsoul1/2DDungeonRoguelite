using UnityEngine;

public class Player : LivingEntity
{
    public Player(string name, int xPos, int yPos, int maxHealth)
        : base(name, xPos, yPos, maxHealth)
    {
    }
}