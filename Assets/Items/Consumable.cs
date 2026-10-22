using System;

public class Consumable : InventoryItem
{
    public int HealingAmount { get; private set; }
    public int UsesRemaining { get; private set; }
    public override string Description { get { return "Heals " + HealingAmount + " HP; " + UsesRemaining + " uses"; } }

    public Consumable(string id, string name, int healingAmount, int uses = 1) : base(id, name)
    {
        if (healingAmount < 1 || uses < 1) throw new ArgumentOutOfRangeException(nameof(healingAmount));
        HealingAmount = healingAmount;
        UsesRemaining = uses;
    }

    public override bool TryUse(Player user, LivingEntity target, out string message)
    {
        if (!CanUse(user) || UsesRemaining == 0 || target != user || user.CurrentHealth == user.MaxHealth)
        {
            message = "You cannot use that healing item right now.";
            return false;
        }
        int before = user.CurrentHealth;
        user.Heal(HealingAmount);
        UsesRemaining--;
        message = "Used " + Name + ": recovered " + (user.CurrentHealth - before) + " HP.";
        if (UsesRemaining == 0) user.Inventory.TryRemove(this);
        return true;
    }
}
