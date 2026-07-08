using UnityEngine;

public class DungeonDebugRunner : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Dungeon test starting...");

        Player player = new Player("Hero", 0, 0, 30, 5);

        Room room = new Room(1);

        LivingEntity goblin = new Enemy("Goblin", 2, 3, 10, 3);

        room.AddEnemy(goblin);

        Debug.Log("Created player: " + player.Name + " HP: " + player.CurrentHealth);
        Debug.Log("Created room: " + room.RoomNumber);
        Debug.Log("Room enemy count: " + room.Enemies.Count);
        Debug.Log("First enemy: " + room.Enemies[0].Name + " HP: " + room.Enemies[0].CurrentHealth);

        player.Attack(goblin);
    }
}