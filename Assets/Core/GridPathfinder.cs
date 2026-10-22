using System.Collections.Generic;

// Search legal footprint anchors, stopping at a tile from which melee is possible.
public static class GridPathfinder
{
    public static bool TryGetStep(RoomGrid grid, LivingEntity mover, LivingEntity target, out int nextX, out int nextY)
    {
        nextX = mover == null ? 0 : mover.XPos;
        nextY = mover == null ? 0 : mover.YPos;
        if (grid == null || mover == null || target == null || mover == target ||
            !mover.IsAlive || !target.IsAlive || mover.Grid != grid || target.Grid != grid ||
            grid.AreAdjacent(mover, target)) return false;

        var frontier = new Queue<(int x, int y, int firstX, int firstY)>();
        var visited = new HashSet<(int x, int y)>();
        frontier.Enqueue((mover.XPos, mover.YPos, mover.XPos, mover.YPos));
        visited.Add((mover.XPos, mover.YPos));
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                int x = current.x + dx[i], y = current.y + dy[i];
                if (!grid.CanPlace(mover, x, y) || !visited.Add((x, y))) continue;
                bool first = current.x == mover.XPos && current.y == mover.YPos;
                int firstX = first ? x : current.firstX;
                int firstY = first ? y : current.firstY;
                long right = (long)x + mover.FootprintWidth, top = (long)y + mover.FootprintHeight;
                long targetRight = (long)target.XPos + target.FootprintWidth;
                long targetTop = (long)target.YPos + target.FootprintHeight;
                bool adjacent = (y < targetTop && target.YPos < top && (right == target.XPos || targetRight == x)) ||
                    (x < targetRight && target.XPos < right && (top == target.YPos || targetTop == y));
                if (adjacent) { nextX = firstX; nextY = firstY; return true; }
                frontier.Enqueue((x, y, firstX, firstY));
            }
        }
        return false;
    }
}
