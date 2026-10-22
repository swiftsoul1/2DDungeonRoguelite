using System.Collections.Generic;

// Areas share one world grid; movement never transfers or rebuilds entities.
public sealed class DungeonArea
{
    public int Id { get; private set; }
    public int MinY { get; private set; }
    public int MaxY { get; private set; }
    public bool Awakened { get; private set; }
    private readonly List<Enemy> enemies = new List<Enemy>();
    public IReadOnlyList<Enemy> Enemies { get { return enemies.AsReadOnly(); } }

    public DungeonArea(int id, int minY, int maxY, bool awakened = false)
    {
        Id = id; MinY = minY; MaxY = maxY; Awakened = awakened;
    }

    public void AddEnemy(Enemy enemy) { enemies.Add(enemy); }
    public bool Owns(Enemy enemy) { return enemies.Contains(enemy); }
    public bool Contains(Entity entity)
    {
        return entity.YPos <= MaxY && (long)entity.YPos + entity.FootprintHeight - 1 >= MinY;
    }
    public void Observe(Player player)
    {
        if (Contains(player)) Awakened = true;
        foreach (Enemy enemy in enemies)
            if (enemy.CurrentHealth < enemy.MaxHealth) Awakened = true;
    }
}
