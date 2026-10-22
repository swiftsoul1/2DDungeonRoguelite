using System;
using System.Collections.Generic;

public class Chest : PropObject
{
    private readonly List<InventoryItem> loot;
    public int RemainingItems { get { return loot.Count; } }

    public Chest(string name, int x, int y, IEnumerable<InventoryItem> contents, int width = 1, int height = 1)
        : base(name, x, y, "", width, height)
    {
        if (contents == null) throw new ArgumentNullException(nameof(contents));
        loot = new List<InventoryItem>();
        HashSet<InventoryItem> seen = new HashSet<InventoryItem>();
        foreach (InventoryItem item in contents)
        {
            if (item == null || item.Owner != null || !seen.Add(item)) throw new ArgumentException("Chest loot must be distinct unowned items.");
            loot.Add(item);
        }
    }

    public override bool TryInteract(LivingEntity actor, out string message)
    {
        Player player = actor as Player;
        if (player == null || !CanInteract(actor))
        {
            message = "Move next to the chest first.";
            return false;
        }
        if (IsUsed) { message = Name + " is empty."; return false; }
        if (!player.Inventory.TryAddRange(loot))
        {
            message = "Not enough inventory space. The chest keeps its contents.";
            return false;
        }
        int collected = loot.Count;
        loot.Clear();
        MarkUsed();
        message = "Opened " + Name + ": collected " + collected + " items.";
        return true;
    }
}
