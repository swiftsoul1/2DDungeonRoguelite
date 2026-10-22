using System;

public static class AttackRules
{
    // Manhattan tile range. Any clear pair of footprint cells may launch/receive a shot.
    public static bool CanAttack(RoomGrid grid, LivingEntity source, LivingEntity target, int range, bool ranged)
    {
        return source != null && source.IsAlive && source.Grid == grid &&
            CanAttackFrom(grid, source, source.XPos, source.YPos, target, range, ranged);
    }

    public static bool CanAttackFrom(RoomGrid grid, LivingEntity source, int x, int y, LivingEntity target, int range, bool ranged)
    {
        if (grid == null || source == null || target == null || source == target || !target.IsAlive || target.Grid != grid) return false;
        for (int sx = x; sx < x + source.FootprintWidth; sx++)
            for (int sy = y; sy < y + source.FootprintHeight; sy++)
                for (int tx = target.XPos; tx < target.XPos + target.FootprintWidth; tx++)
                    for (int ty = target.YPos; ty < target.YPos + target.FootprintHeight; ty++)
                    {
                        int distance = Math.Abs(sx - tx) + Math.Abs(sy - ty);
                        if (distance < 1 || distance > range) continue;
                        if (!ranged ? distance == 1 : ClearShot(grid, source, target, sx, sy, tx, ty)) return true;
                    }
        return false;
    }

    private static bool ClearShot(RoomGrid grid, Entity source, Entity target, int sx, int sy, int tx, int ty)
    {
        // Supercover: a ray touching the corner of solid cover is blocked too.
        for (int x = Math.Min(sx, tx); x <= Math.Max(sx, tx); x++)
            for (int y = Math.Min(sy, ty); y <= Math.Max(sy, ty); y++)
            {
                double low = 0, high = 1;
                if (!Clip(sx, tx - sx, x - 0.5, x + 0.5, ref low, ref high) ||
                    !Clip(sy, ty - sy, y - 0.5, y + 0.5, ref low, ref high)) continue;
                Entity occupant = grid.GetOccupant(x, y);
                if (grid.IsBlocked(x, y) || (occupant != null && occupant != source && occupant != target)) return false;
            }
        return true;
    }

    private static bool Clip(double start, double delta, double min, double max, ref double low, ref double high)
    {
        if (delta == 0) return start >= min && start <= max;
        double a = (min - start) / delta, b = (max - start) / delta;
        low = Math.Max(low, Math.Min(a, b)); high = Math.Min(high, Math.Max(a, b));
        return low <= high;
    }
}
