public class PropObject : Entity
{
    private IPropState currentState = new UnusedPropState();
    public bool IsUsed { get; private set; }
    public string ContentsDescription { get; private set; }
    public virtual bool IsInteractive { get { return true; } }

    public PropObject(string name, int x, int y, string contentsDescription = "Nothing useful.", int width = 1, int height = 1)
        : base(name, x, y, width, height)
    {
        ContentsDescription = contentsDescription;
    }

    protected bool CanInteract(LivingEntity actor)
    {
        return actor != null && actor.IsAlive && Grid != null && actor.Grid == Grid && Grid.AreAdjacent(actor, this);
    }

    public virtual bool TryInteract(LivingEntity actor, out string message)
    {
        if (!IsInteractive || !CanInteract(actor))
        {
            message = "Move next to an interactive object first.";
            return false;
        }
        return currentState.TryUse(this, out message);
    }

    internal void MarkUsed()
    {
        IsUsed = true;
        currentState = new UsedPropState();
    }
}
