using System.Collections.Generic;

public class Room
{
    public int RoomNumber { get; private set; }

    public List<LivingEntity> Enemies { get; private set; }

    public RoomGrid Grid { get; private set; }
    private readonly List<PropObject> props = new List<PropObject>();
    public IReadOnlyList<PropObject> Props { get { return props; } }

    public Room(int roomNumber, int width = 12, int height = 10)
    {
        RoomNumber = roomNumber;
        Grid = new RoomGrid(width, height);
        Enemies = new List<LivingEntity>();
    }

    public void AddNPC(LivingEntity npc)
    {
        if (!Grid.TryAdd(npc))
            throw new System.InvalidOperationException("NPC cannot be placed at its grid position.");
        Enemies.Add(npc);
    }

    public void AddProp(PropObject prop)
    {
        if (prop == null || prop is WallProp || !Grid.TryAdd(prop))
            throw new System.InvalidOperationException("Prop cannot be placed at its grid position.");
        props.Add(prop);
    }
}
