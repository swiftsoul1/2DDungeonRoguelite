public enum WallFacing { North, East, South, West }

// Mounted in a separate wall layer, so an open door and a character can share a cell.
public class WallProp : PropObject
{
    public WallFacing Facing { get; private set; }
    private readonly bool searchable;
    public override bool IsInteractive { get { return searchable; } }
    public virtual bool BlocksPassage { get { return true; } }

    public WallProp(string name, int x, int y, WallFacing facing, bool searchable = false,
        string contentsDescription = "Nothing useful.", int width = 1, int height = 1)
        : base(name, x, y, contentsDescription, width, height)
    {
        Facing = facing;
        this.searchable = searchable;
    }

    public override bool TryInteract(LivingEntity actor, out string message)
    {
        // Facing describes the wall edge: a north-wall shelf opens toward the south.
        bool accessible = CanInteract(actor) && (
            Facing == WallFacing.North ? (long)actor.YPos + actor.FootprintHeight == YPos :
            Facing == WallFacing.South ? actor.YPos == (long)YPos + FootprintHeight :
            Facing == WallFacing.East ? (long)actor.XPos + actor.FootprintWidth == XPos :
            actor.XPos == (long)XPos + FootprintWidth);
        if (!accessible)
        {
            message = "Move to the front of " + Name + " first.";
            return false;
        }
        return base.TryInteract(actor, out message);
    }
}
