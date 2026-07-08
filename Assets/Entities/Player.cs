using UnityEngine;

public class Player : LivingEntity
{
    //inventory
    //armor number
    //item EquippedWeapon 


    public Player(string name, int xPos, int yPos, int maxHealth, int attackDamage)
        : base(name, xPos, yPos, maxHealth, attackDamage)
    {
    }
    public void TakeDamage(int damage) 
    {
        base.TakeDamage(damage);
        //base.TakeDamage(damage-armorAmount);
    }
    public void EquipWeapon()
    {
        
    }
    public void UnequipWeapon() 
    {
    
    }


}