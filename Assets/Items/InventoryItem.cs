using System;

public interface IInventoryItem
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    bool TryUse(Player user, LivingEntity target, out string message);
}

public abstract class InventoryItem : IInventoryItem
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public abstract string Description { get; }
    internal Inventory Owner { get; set; }

    protected InventoryItem(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Item definition needs an ID.");
        Id = id;
        Name = name;
    }

    protected bool CanUse(Player user)
    {
        return user != null && user.IsAlive && Owner == user.Inventory && user.Inventory.Contains(this);
    }

    public abstract bool TryUse(Player user, LivingEntity target, out string message);
}
