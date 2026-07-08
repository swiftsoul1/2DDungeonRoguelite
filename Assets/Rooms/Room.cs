using System.Collections.Generic;

public class Room
{
    public int RoomNumber { get; private set; }

    public List<LivingEntity> Enemies { get; private set; }

    public Room(int roomNumber)
    {
        RoomNumber = roomNumber;
        Enemies = new List<LivingEntity>();
    }

    public void AddEnemy(LivingEntity enemy)
    {
        Enemies.Add(enemy);
    }
}