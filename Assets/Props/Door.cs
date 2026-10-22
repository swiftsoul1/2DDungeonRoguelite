public enum DoorState { Closed, Open, Locked }

public class Door : WallProp
{
    public DoorState State { get; private set; }
    public bool IsOpen { get { return State == DoorState.Open; } }
    public override bool IsInteractive { get { return true; } }
    public override bool BlocksPassage { get { return !IsOpen; } }

    public Door(string name, int x, int y, WallFacing facing, int width = 1, int height = 1, bool locked = false)
        : base(name, x, y, facing, width: width, height: height)
    {
        State = locked ? DoorState.Locked : DoorState.Closed;
    }

    public bool TryOpen()
    {
        if (State != DoorState.Closed) return false;
        State = DoorState.Open;
        return true;
    }

    public bool TryClose()
    {
        if (!IsOpen || (Grid != null && Grid.HasOccupantInFootprint(this))) return false;
        State = DoorState.Closed;
        return true;
    }

    // A future key/switch system must authorize calls to these methods.
    public bool TryUnlock()
    {
        if (State != DoorState.Locked) return false;
        State = DoorState.Closed;
        return true;
    }

    public bool TryLock()
    {
        if (State != DoorState.Closed) return false;
        State = DoorState.Locked;
        return true;
    }

    public override bool TryInteract(LivingEntity actor, out string message)
    {
        if (!CanInteract(actor))
        {
            message = "Move next to " + Name + " first.";
            return false;
        }
        if (State == DoorState.Locked)
        {
            message = Name + " is locked.";
            return false;
        }
        bool opening = !IsOpen;
        bool changed = opening ? TryOpen() : TryClose();
        message = changed ? Name + (opening ? " opened." : " closed.") : "The doorway is occupied.";
        return changed;
    }
}
