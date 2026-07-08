using System.Collections.Generic;

public class Room
{
    public int RoomNumber { get; private set; }

    public List<Enemy> Enemies { get; private set; }

    public Room(int roomNumber)
    {
        RoomNumber = roomNumber;
        Enemies = new List<Enemy>();
    }

    public void AddEnemy(Enemy enemy)
    {
        Enemies.Add(enemy);
    }
}