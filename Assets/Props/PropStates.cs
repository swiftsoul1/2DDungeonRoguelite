public interface IPropState
{
    bool TryUse(PropObject prop, out string message);
}

public class UnusedPropState : IPropState
{
    public bool TryUse(PropObject prop, out string message)
    {
        prop.MarkUsed();
        message = "Searched " + prop.Name + ": " + prop.ContentsDescription;
        return true;
    }
}

public class UsedPropState : IPropState
{
    public bool TryUse(PropObject prop, out string message)
    {
        message = prop.Name + " has already been searched.";
        return false;
    }
}
