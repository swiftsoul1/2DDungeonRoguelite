using System.Collections.Generic;

public static class RangedTactics
{
    public static bool TryMove(RoomGrid grid, Enemy enemy, Player player)
    {
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        // Adjacent enemies fight in melee; movement must never restart a kiting loop.
        if (grid.AreAdjacent(enemy, player))
        {
            return false;
        }
        var queue = new Queue<(int x, int y, int fx, int fy)>();
        var seen = new HashSet<(int x, int y)>();
        queue.Enqueue((enemy.XPos, enemy.YPos, enemy.XPos, enemy.YPos));
        seen.Add((enemy.XPos, enemy.YPos));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                int x = current.x + dx[i], y = current.y + dy[i];
                if (!grid.CanPlace(enemy, x, y) || !seen.Add((x, y))) continue;
                bool first = current.x == enemy.XPos && current.y == enemy.YPos;
                int fx = first ? x : current.fx, fy = first ? y : current.fy;
                if (Distance(enemy, x, y, player) >= 2 && AttackRules.CanAttackFrom(grid, enemy, x, y, player, enemy.AttackRange, true))
                    return grid.TryMove(enemy, fx, fy);
                queue.Enqueue((x, y, fx, fy));
            }
        }
        return false;
    }

    private static int Distance(Entity enemy, int x, int y, Entity player)
    {
        int gapX = System.Math.Max(0, System.Math.Max(player.XPos - (x + enemy.FootprintWidth - 1), x - (player.XPos + player.FootprintWidth - 1)));
        int gapY = System.Math.Max(0, System.Math.Max(player.YPos - (y + enemy.FootprintHeight - 1), y - (player.YPos + player.FootprintHeight - 1)));
        return gapX + gapY;
    }
}
