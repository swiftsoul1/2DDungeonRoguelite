using System;

public class Armor : InventoryItem
{
    public int Defense { get; private set; }
    public override string Description { get { return Defense + " armor"; } }

    public Armor(string id, string name, int defense) : base(id, name)
    {
        if (defense < 0) throw new ArgumentOutOfRangeException(nameof(defense));
        Defense = defense;
    }

    public override bool TryUse(Player user, LivingEntity target, out string message)
    {
        bool equipped = CanUse(user) && user.EquipArmor(this);
        message = equipped ? "Equipped " + Name + "." : "That armor cannot be equipped right now.";
        return equipped;
    }
}
