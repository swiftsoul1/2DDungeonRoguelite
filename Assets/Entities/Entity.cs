using UnityEngine;

public class Entity
{
    public string Name { get; protected set; }

    public int XPos { get; protected set; }
    public int YPos { get; protected set; }

    public Entity(string name, int xPos, int yPos)
    {
        Name = name;
        XPos = xPos;
        YPos = yPos;
    }

    public void SetPosition(int xPos, int yPos)
    {
        XPos = xPos;
        YPos = yPos;
    }
}