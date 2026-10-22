using System;
using System.Collections.Generic;

// Shared by the playable scene and integration checks: one authored test layout.
public class TestRoomSetup
{
    public Room Room { get; private set; }
    public Player Player { get; private set; }
    public Enemy Enemy { get; private set; }
    public Door Door { get; private set; }
    public WallProp Bookcase { get; private set; }
    public GridEncounter Encounter { get; private set; }
    public Chest Chest { get; private set; }
    public Chest SecondChest { get; private set; }
    public Enemy SecondEnemy { get; private set; }
    public bool Connected { get; private set; }
    private readonly List<DungeonArea> areas = new List<DungeonArea>();
    public IReadOnlyList<DungeonArea> Areas { get { return areas.AsReadOnly(); } }
    private readonly List<WallProp> lamps = new List<WallProp>();
    public IReadOnlyList<WallProp> Lamps { get { return lamps; } }

    private TestRoomSetup() { }

    public static TestRoomSetup Create(bool largeEnemy = false, bool rangedEnemy = false, bool connected = false)
    {
        TestRoomSetup setup = new TestRoomSetup();
        setup.Connected = connected;
        setup.Room = new Room(1, 12, connected ? 19 : 10);
        RoomGrid grid = setup.Room.Grid;
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
                grid.TrySetBlocked(x, y, x == 0 || y == 0 || x == grid.Width - 1 || y == grid.Height - 1);
        if (connected)
            for (int x = 1; x < grid.Width - 1; x++) grid.TrySetBlocked(x, 9, true);

        setup.Player = new Player("Hero", 4, 3, 30, 5);
        setup.Enemy = new Enemy(rangedEnemy ? "Skeletal archer" : largeEnemy ? "Large skeletal sentinel" : "Bone-club skeleton", 7, 6,
            largeEnemy ? 25 : 10, 3, largeEnemy ? 2 : 1, largeEnemy ? 2 : 1, largeEnemy ? 2 : 0, rangedEnemy ? 5 : 1);
        setup.Door = new Door("North door", 5, 9, WallFacing.North, connected ? 2 : 1);
        setup.Bookcase = new WallProp("Bookcase", 2, 9, WallFacing.North, true,
            "A faded note describes a sealed chamber.", 2, 1);

        if (!grid.TryAdd(setup.Player)) throw new InvalidOperationException("Test-room player placement failed.");
        setup.Room.AddNPC(setup.Enemy);
        setup.Chest = new Chest("Supply chest", 2, 2, new InventoryItem[] {
            new Weapon("weapon.copper_blade", "Copper-edged blade", 6, 2),
            new Weapon("weapon.shortbow", "Copper shortbow", 4, 1, 5),
            new Armor("armor.iron_vest", "Iron vest", 2),
            new Consumable("consumable.healing_draught", "Healing draught", 8, 2)
        });
        setup.Room.AddProp(setup.Chest);
        setup.Room.AddProp(new BlockingProp("Storage crate", 6, 3));
        setup.Room.AddProp(new BlockingProp("Storage crate", 8, 4));
        if (!grid.TryMount(setup.Door) || !grid.TryMount(setup.Bookcase))
            throw new InvalidOperationException("Test-room wall fixture placement failed.");
        setup.lamps.Add(new WallProp("Violet lamp", 0, 3, WallFacing.West));
        setup.lamps.Add(new WallProp("Violet lamp", 11, 6, WallFacing.East));
        setup.lamps.Add(new WallProp("Violet lamp", 8, 9, WallFacing.North));
        setup.lamps.Add(new WallProp("Violet lamp", 3, 0, WallFacing.South));
        foreach (WallProp lamp in setup.lamps)
            if (!grid.TryMount(lamp)) throw new InvalidOperationException("Test-room lamp placement failed.");
        var firstArea = new DungeonArea(1, 1, 8, true);
        firstArea.AddEnemy(setup.Enemy);
        setup.areas.Add(firstArea);
        var roster = new List<Enemy> { setup.Enemy };
        if (connected)
        {
            var secondArea = new DungeonArea(2, 10, 17);
            setup.SecondEnemy = new Enemy("Chamber archer", 7, 15, 14, 3, attackRange: 5);
            setup.Room.AddNPC(setup.SecondEnemy);
            secondArea.AddEnemy(setup.SecondEnemy);
            setup.areas.Add(secondArea);
            roster.Add(setup.SecondEnemy);
            setup.SecondChest = new Chest("Chamber chest", 2, 14, new InventoryItem[] {
                new Consumable("consumable.chamber_draught", "Chamber draught", 12, 2)
            });
            setup.Room.AddProp(setup.SecondChest);
            setup.Room.AddProp(new BlockingProp("Chamber crate", 5, 13));
            setup.Room.AddProp(new BlockingProp("Chamber crate", 8, 12));
            var lamp = new WallProp("Chamber lamp", 8, 18, WallFacing.North);
            if (!grid.TryMount(lamp)) throw new InvalidOperationException("Chamber lamp placement failed.");
            setup.lamps.Add(lamp);
        }
        setup.Encounter = new GridEncounter(grid, setup.Player, roster, enemy => {
            foreach (DungeonArea area in setup.areas) area.Observe(setup.Player);
            foreach (DungeonArea area in setup.areas)
                if (area.Owns(enemy)) return area.Awakened;
            return false;
        });
        return setup;
    }
}
