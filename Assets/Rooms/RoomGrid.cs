using System;
using System.Collections.Generic;

// Coordinates identify the bottom-left cell of a rectangular footprint.
// One unit is one tile. Rendering and texture resolution do not affect occupancy.
public class RoomGrid
{
    private readonly bool[,] blocked;
    private readonly List<Entity> occupants = new List<Entity>();
    private readonly List<WallProp> wallObjects = new List<WallProp>();
    public IReadOnlyList<WallProp> WallObjects { get { return wallObjects; } }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public RoomGrid(int width, int height)
    {
        if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
        blocked = new bool[width, height];
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Width && y < Height;
    }

    public bool IsBlocked(int x, int y)
    {
        if (!IsInside(x, y)) return true;
        WallProp fixture = GetWallObject(x, y);
        return fixture != null ? fixture.BlocksPassage : blocked[x, y];
    }

    public WallProp GetWallObject(int x, int y)
    {
        foreach (WallProp prop in wallObjects)
        {
            if (x >= prop.XPos && (long)x < (long)prop.XPos + prop.FootprintWidth &&
                y >= prop.YPos && (long)y < (long)prop.YPos + prop.FootprintHeight) return prop;
        }
        return null;
    }

    internal bool HasOccupantInFootprint(Entity entity)
    {
        for (int x = entity.XPos; x < (long)entity.XPos + entity.FootprintWidth; x++)
            for (int y = entity.YPos; y < (long)entity.YPos + entity.FootprintHeight; y++)
                if (GetOccupant(x, y) != null) return true;
        return false;
    }

    private bool CanMount(WallProp prop, int width, int height)
    {
        if (prop == null || width < 1 || height < 1 || prop.XPos < 0 || prop.YPos < 0 ||
            (long)prop.XPos + width > Width || (long)prop.YPos + height > Height) return false;
        for (int x = prop.XPos; x < prop.XPos + width; x++)
        {
            for (int y = prop.YPos; y < prop.YPos + height; y++)
            {
                WallProp other = GetWallObject(x, y);
                if (!blocked[x, y] || (other != null && other != prop) || GetOccupant(x, y) != null)
                    return false;
            }
        }
        return true;
    }

    public bool TryMount(WallProp prop)
    {
        if (prop == null || prop.Grid != null || !CanMount(prop, prop.FootprintWidth, prop.FootprintHeight)) return false;
        wallObjects.Add(prop);
        prop.Grid = this;
        return true;
    }

    public Entity GetOccupant(int x, int y)
    {
        foreach (Entity entity in occupants)
        {
            if (x >= entity.XPos && (long)x < (long)entity.XPos + entity.FootprintWidth &&
                y >= entity.YPos && (long)y < (long)entity.YPos + entity.FootprintHeight)
                return entity;
        }
        return null;
    }

    public bool TrySetBlocked(int x, int y, bool value)
    {
        if (!IsInside(x, y) || GetWallObject(x, y) != null || (value && GetOccupant(x, y) != null)) return false;
        blocked[x, y] = value;
        return true;
    }

    public bool CanPlace(Entity entity, int x, int y)
    {
        return entity != null && CanFit(entity, x, y, entity.FootprintWidth, entity.FootprintHeight);
    }

    private bool CanFit(Entity entity, int x, int y, int width, int height)
    {
        if (entity == null || x < 0 || y < 0 ||
            width < 1 || height < 1 ||
            (long)x + width > Width || (long)y + height > Height) return false;

        for (int col = x; col < x + width; col++)
        {
            for (int row = y; row < y + height; row++)
            {
                Entity occupant = GetOccupant(col, row);
                if (IsBlocked(col, row) || (occupant != null && occupant != entity)) return false;
            }
        }
        return true;
    }

    public bool TryResize(Entity entity, int width, int height)
    {
        WallProp prop = entity as WallProp;
        if (prop != null)
        {
            if (prop.Grid != this || HasOccupantInFootprint(prop) || !CanMount(prop, width, height)) return false;
            prop.SetGridSize(width, height);
            return true;
        }
        if (entity == null || entity.Grid != this || !CanFit(entity, entity.XPos, entity.YPos, width, height))
            return false;
        entity.SetGridSize(width, height);
        return true;
    }

    public bool TryAdd(Entity entity)
    {
        if (entity == null || entity is WallProp || entity.Grid != null || !CanPlace(entity, entity.XPos, entity.YPos))
            return false;
        occupants.Add(entity);
        entity.Grid = this;
        return true;
    }

    // Normal movement is one cardinal step, preventing diagonal moves and wall teleporting.
    public bool TryMove(Entity entity, int x, int y)
    {
        if (entity == null || entity is WallProp || entity.Grid != this) return false;
        LivingEntity living = entity as LivingEntity;
        if (living != null && !living.IsAlive) return false;
        long distance = Math.Abs((long)x - entity.XPos) + Math.Abs((long)y - entity.YPos);
        if (distance != 1 || !CanPlace(entity, x, y)) return false;
        entity.SetGridPosition(x, y);
        return true;
    }

    public bool Remove(Entity entity)
    {
        WallProp prop = entity as WallProp;
        if (prop != null)
        {
            if (prop.Grid != this || HasOccupantInFootprint(prop) || !wallObjects.Remove(prop)) return false;
            prop.Grid = null;
            return true;
        }
        if (entity == null || entity.Grid != this || !occupants.Remove(entity)) return false;
        entity.Grid = null;
        return true;
    }

    public bool AreAdjacent(Entity first, Entity second)
    {
        if (first == null || second == null || first == second ||
            first.Grid != this || second.Grid != this) return false;
        long firstRight = (long)first.XPos + first.FootprintWidth;
        long secondRight = (long)second.XPos + second.FootprintWidth;
        long firstTop = (long)first.YPos + first.FootprintHeight;
        long secondTop = (long)second.YPos + second.FootprintHeight;
        bool overlapX = first.XPos < secondRight && second.XPos < firstRight;
        bool overlapY = first.YPos < secondTop && second.YPos < firstTop;
        return (overlapY && (firstRight == second.XPos || secondRight == first.XPos)) ||
               (overlapX && (firstTop == second.YPos || secondTop == first.YPos));
    }
}
