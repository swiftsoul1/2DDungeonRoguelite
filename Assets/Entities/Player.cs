using System;

public class Player : LivingEntity
{
    private readonly Random damageRandom = new Random();
    public Inventory Inventory { get; private set; }
    public Weapon EquippedWeapon { get; private set; }
    public Armor EquippedArmor { get; private set; }
    public override int Armor
    {
        get { return (int)Math.Min(int.MaxValue, (long)BaseArmor + (EquippedArmor == null ? 0 : EquippedArmor.Defense)); }
    }

    public Player(string name, int xPos, int yPos, int maxHealth, int attackDamage, int armor = 0, int inventoryCapacity = 12)
        : base(name, xPos, yPos, maxHealth, attackDamage, 1, 1, armor)
    {
        Inventory = new Inventory(this, inventoryCapacity);
    }

    public override void Attack(LivingEntity target)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (!IsAlive) return;
        AttackDamage = EquippedWeapon == null ? BaseAttackDamage : EquippedWeapon.RollDamage(damageRandom);
        base.Attack(target);
    }

    public bool EquipWeapon(Weapon weapon)
    {
        if (!IsAlive || weapon == null || !Inventory.Contains(weapon) || EquippedWeapon == weapon) return false;
        EquippedWeapon = weapon;
        return true;
    }

    public void UnequipWeapon()
    {
        EquippedWeapon = null;
        AttackDamage = BaseAttackDamage;
    }

    public bool EquipArmor(Armor armor)
    {
        if (!IsAlive || armor == null || !Inventory.Contains(armor) || EquippedArmor == armor) return false;
        EquippedArmor = armor;
        return true;
    }

    public void UnequipArmor() { EquippedArmor = null; }

    internal void OnItemRemoved(InventoryItem item)
    {
        if (EquippedWeapon == item) UnequipWeapon();
        if (EquippedArmor == item) UnequipArmor();
    }
}
