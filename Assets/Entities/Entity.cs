using System;

public class Entity
{
    public string Name { get; protected set; }
    public int XPos { get; protected set; }
    public int YPos { get; protected set; }
    public int FootprintWidth { get; private set; }
    public int FootprintHeight { get; private set; }
    public int BaseFootprintWidth { get; private set; }
    public int BaseFootprintHeight { get; private set; }

    internal RoomGrid Grid { get; set; }

    public Entity(string name, int xPos, int yPos, int footprintWidth = 1, int footprintHeight = 1)
    {
        if (footprintWidth < 1) throw new ArgumentOutOfRangeException(nameof(footprintWidth));
        if (footprintHeight < 1) throw new ArgumentOutOfRangeException(nameof(footprintHeight));
        Name = name;
        XPos = xPos;
        YPos = yPos;
        FootprintWidth = footprintWidth;
        FootprintHeight = footprintHeight;
        BaseFootprintWidth = footprintWidth;
        BaseFootprintHeight = footprintHeight;
    }

    // Effects request a current size; the original size remains available for restoration.
    // Keep the bottom-left anchor fixed. Rejected changes leave size and occupancy untouched.
    public bool TryResize(int width, int height)
    {
        if (width < 1 || height < 1) return false;
        if (Grid != null) return Grid.TryResize(this, width, height);
        SetGridSize(width, height);
        return true;
    }

    public bool TryRestoreBaseSize()
    {
        return TryResize(BaseFootprintWidth, BaseFootprintHeight);
    }

    internal void SetGridSize(int width, int height)
    {
        FootprintWidth = width;
        FootprintHeight = height;
    }

    // Unplaced entities may be positioned freely. Placed entities obey their room grid.
    public void SetPosition(int xPos, int yPos)
    {
        if (Grid != null)
        {
            if (!Grid.TryMove(this, xPos, yPos))
                throw new InvalidOperationException("The entity cannot move to that cell.");
            return;
        }
        SetGridPosition(xPos, yPos);
    }

    internal void SetGridPosition(int xPos, int yPos)
    {
        XPos = xPos;
        YPos = yPos;
    }
}
