using System;

public class Weapon : InventoryItem
{
    public int Range { get; private set; }
    public bool IsRanged { get { return Range > 1; } }
    public int Dice { get; private set; }
    public int Bonus { get; private set; }
    public override string Description { get { return (Bonus + 1) + "-" + (Bonus + Dice) + " damage; range " + Range; } }

    public Weapon(string id, string name, int dice, int bonus, int range = 1) : base(id, name)
    {
        if (dice < 1 || bonus < 0 || (long)dice + bonus > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(dice));
        if (range < 1) throw new ArgumentOutOfRangeException(nameof(range));
        Range = range;
        Dice = dice;
        Bonus = bonus;
    }

    public int RollDamage(Random random)
    {
        if (random == null) throw new ArgumentNullException(nameof(random));
        return random.Next(Dice) + 1 + Bonus;
    }

    public override bool TryUse(Player user, LivingEntity target, out string message)
    {
        bool equipped = CanUse(user) && user.EquipWeapon(this);
        message = equipped ? "Equipped " + Name + "." : "That weapon cannot be equipped right now.";
        return equipped;
    }
}
