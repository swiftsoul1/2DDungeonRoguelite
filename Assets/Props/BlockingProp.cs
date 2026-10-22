public class BlockingProp : PropObject
{
    public override bool IsInteractive { get { return false; } }

    public BlockingProp(string name, int x, int y, int width = 1, int height = 1)
        : base(name, x, y, "", width, height) { }
}
