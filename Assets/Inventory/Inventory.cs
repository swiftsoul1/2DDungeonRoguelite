using System;
using System.Collections.Generic;

public class Inventory
{
    private readonly Player owner;
    private readonly List<InventoryItem> items = new List<InventoryItem>();
    private readonly IReadOnlyList<InventoryItem> view;
    public IReadOnlyList<InventoryItem> Items { get { return view; } }
    public int Capacity { get; private set; }

    public Inventory(Player owner, int capacity = 12)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        view = items.AsReadOnly();
    }

    public bool Contains(InventoryItem item) { return items.Contains(item); }
    public bool TryAdd(InventoryItem item) { return TryAddRange(new[] { item }); }

    // All-or-nothing loot transfer: a failed chest pickup cannot lose or duplicate items.
    public bool TryAddRange(IEnumerable<InventoryItem> incoming)
    {
        if (incoming == null) return false;
        List<InventoryItem> batch = new List<InventoryItem>();
        HashSet<InventoryItem> seen = new HashSet<InventoryItem>();
        foreach (InventoryItem item in incoming)
        {
            Consumable consumable = item as Consumable;
            if (item == null || item.Owner != null || !seen.Add(item) ||
                (consumable != null && consumable.UsesRemaining == 0)) return false;
            batch.Add(item);
            if (batch.Count > Capacity - items.Count) return false;
        }
        foreach (InventoryItem item in batch) { item.Owner = this; items.Add(item); }
        return true;
    }

    public bool TryRemove(InventoryItem item)
    {
        if (item == null || item.Owner != this || !items.Remove(item)) return false;
        item.Owner = null;
        owner.OnItemRemoved(item);
        return true;
    }
}
