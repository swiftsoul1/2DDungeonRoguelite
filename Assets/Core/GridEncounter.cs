using System;
using System.Collections.Generic;

// Prototype policy: each successful move, adjacent attack, or wait gives every living enemy one turn.
public class GridEncounter
{
    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly Func<Enemy, bool> canAct;
    public RoomGrid Grid { get; private set; }
    public Player Player { get; private set; }
    public IReadOnlyList<Enemy> Enemies { get { return enemies; } }
    public int TurnNumber { get; private set; } = 1;
    public string LastAction { get; private set; } = "Your turn. Move, attack, or wait.";
    public bool IsOver { get { return !Player.IsAlive || enemies.TrueForAll(enemy => !enemy.IsAlive); } }

    public GridEncounter(RoomGrid grid, Player player, IEnumerable<Enemy> enemies, Func<Enemy, bool> canAct = null)
    {
        this.canAct = canAct;
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        Player = player ?? throw new ArgumentNullException(nameof(player));
        if (!player.IsAlive || player.Grid != grid)
            throw new ArgumentException("Place a living player on the grid first.");
        if (enemies == null) throw new ArgumentNullException(nameof(enemies));
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive || enemy.Grid != grid || this.enemies.Contains(enemy))
                throw new ArgumentException("Place distinct living enemies on the grid first.");
            this.enemies.Add(enemy);
        }
    }

    public bool TryStep(int dx, int dy)
    {
        if (!Player.IsAlive || Math.Abs((long)dx) + Math.Abs((long)dy) != 1) return false;
        int x = Player.XPos + dx;
        int y = Player.YPos + dy;
        if (Grid.TryMove(Player, x, y))
        {
            LastAction = "You moved.";
            FinishTurn();
            return true;
        }

        // Check the whole leading edge, including enlarged players. If multiple enemies
        // block the step, require an explicit click rather than choosing a target arbitrarily.
        Entity target = null;
        for (int col = x; col < (long)x + Player.FootprintWidth; col++)
        {
            for (int row = y; row < (long)y + Player.FootprintHeight; row++)
            {
                Enemy enemy = Grid.GetOccupant(col, row) as Enemy;
                WallProp fixture = Grid.GetWallObject(col, row);
                PropObject floorProp = Grid.GetOccupant(col, row) as PropObject;
                Entity candidate = enemy != null && enemy.IsAlive && enemies.Contains(enemy) ? (Entity)enemy :
                    floorProp != null && floorProp.IsInteractive ? floorProp :
                    fixture != null && fixture.IsInteractive && fixture.BlocksPassage ? fixture : null;
                if (candidate == null) continue;
                if (target != null && target != candidate) return false;
                target = candidate;
            }
        }
        Enemy attackTarget = target as Enemy;
        return attackTarget != null ? TryAttack(attackTarget) : TryUseProp(target as PropObject);
    }

    public bool TryInteractCell(int x, int y)
    {
        Enemy target = Grid.GetOccupant(x, y) as Enemy;
        if (target != null) return TryAttack(target);
        PropObject floorProp = Grid.GetOccupant(x, y) as PropObject;
        if (floorProp != null) return TryUseProp(floorProp);
        WallProp fixture = Grid.GetWallObject(x, y);
        Door door = fixture as Door;
        if (fixture != null && (door == null || !door.IsOpen)) return TryUseWallObject(fixture);
        bool overlapX = x >= Player.XPos && (long)x < (long)Player.XPos + Player.FootprintWidth;
        bool overlapY = y >= Player.YPos && (long)y < (long)Player.YPos + Player.FootprintHeight;
        if (overlapY && x == (long)Player.XPos + Player.FootprintWidth) return TryStep(1, 0);
        if (overlapY && x == (long)Player.XPos - 1) return TryStep(-1, 0);
        if (overlapX && y == (long)Player.YPos + Player.FootprintHeight) return TryStep(0, 1);
        if (overlapX && y == (long)Player.YPos - 1) return TryStep(0, -1);
        return false;
    }

    public bool TryUseWallObject(WallProp prop)
    {
        return TryUseProp(prop);
    }

    public bool TryUseProp(PropObject prop)
    {
        if (!Player.IsAlive || prop == null || prop.Grid != Grid) return false;
        string message;
        bool used = prop.TryInteract(Player, out message);
        LastAction = message;
        if (used) FinishTurn();
        return used;
    }

    public bool TryUseNearbyWallObject()
    {
        PropObject selected = null;
        // Gather unique adjacent floor props without assuming they occupy a single cell.
        for (int x = Player.XPos - 1; x <= (long)Player.XPos + Player.FootprintWidth; x++)
        {
            for (int y = Player.YPos - 1; y <= (long)Player.YPos + Player.FootprintHeight; y++)
            {
                PropObject prop = Grid.GetOccupant(x, y) as PropObject;
                if (prop == null || !prop.IsInteractive || !Grid.AreAdjacent(Player, prop)) continue;
                if (selected != null && selected != prop)
                {
                    LastAction = "Several objects are nearby. Click the one you want.";
                    return false;
                }
                selected = prop;
            }
        }
        foreach (WallProp prop in Grid.WallObjects)
        {
            if (!prop.IsInteractive || !Grid.AreAdjacent(Player, prop)) continue;
            if (selected != null)
            {
                LastAction = "Several objects are nearby. Click the one you want.";
                return false;
            }
            selected = prop;
        }
        return TryUseProp(selected);
    }

    public bool TryUseItem(InventoryItem item)
    {
        if (!Player.IsAlive || item == null || !Player.Inventory.Contains(item)) return false;
        string message;
        bool used = item.TryUse(Player, Player, out message);
        LastAction = message;
        if (used) FinishTurn();
        return used;
    }

    public bool TryUnequipWeapon()
    {
        if (!Player.IsAlive || Player.EquippedWeapon == null) return false;
        Player.UnequipWeapon();
        LastAction = "Weapon unequipped.";
        FinishTurn();
        return true;
    }

    public bool TryUnequipArmor()
    {
        if (!Player.IsAlive || Player.EquippedArmor == null) return false;
        Player.UnequipArmor();
        LastAction = "Armor unequipped.";
        FinishTurn();
        return true;
    }

    public bool CanAttack(Enemy target)
    {
        Weapon weapon = Player.EquippedWeapon;
        return !IsOver && enemies.Contains(target) && AttackRules.CanAttack(Grid, Player, target,
            weapon == null ? 1 : weapon.Range, weapon != null && weapon.IsRanged);
    }

    public bool TryAttack(Enemy target)
    {
        if (!CanAttack(target)) { LastAction = "No clear target in weapon range."; return false; }
        Player.Attack(target);
        LastAction = "You hit " + target.Name + " for " + target.LastDamageTaken + ".";
        if (!target.IsAlive)
        {
            Grid.Remove(target);
            LastAction += " Enemy defeated.";
        }
        FinishTurn();
        return true;
    }

    public bool Wait()
    {
        if (!Player.IsAlive) return false;
        LastAction = "You waited.";
        FinishTurn();
        return true;
    }

    private void FinishTurn()
    {
        foreach (Enemy enemy in enemies)
        {
            if (!Player.IsAlive) break;
            if (!enemy.IsAlive || (canAct != null && !canAct(enemy))) continue;
            if (Grid.AreAdjacent(enemy, Player))
            {
                enemy.Attack(Player);
                LastAction += " " + enemy.Name + " strikes in melee for " + Player.LastDamageTaken + ".";
                continue;
            }
            if (AttackRules.CanAttack(Grid, enemy, Player, enemy.AttackRange, enemy.IsRanged))
            {
                enemy.Attack(Player);
                LastAction += " " + enemy.Name + " hits for " + Player.LastDamageTaken + ".";
            }
            else
            {
                if (enemy.IsRanged) { RangedTactics.TryMove(Grid, enemy, Player); continue; }
                int nextX, nextY;
                if (GridPathfinder.TryGetStep(Grid, enemy, Player, out nextX, out nextY))
                    Grid.TryMove(enemy, nextX, nextY);
            }
        }
        TurnNumber++;
        if (!Player.IsAlive) LastAction += " You fell. Restart to try again.";
        else if (IsOver) LastAction += " Room cleared!";
    }
}

