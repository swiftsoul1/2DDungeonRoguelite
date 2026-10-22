using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadState : ILivingState
{

    LivingEntity host;

    public DeadState(LivingEntity host)
    {
        this.host = host;
    }

    public void Attack(LivingEntity enemy)
    {
        return;
    }

    public void TakeDamage(int dmg)
    {
        return;
    }
    public void Heal(int amount)
    {
        //if revive spell do something else, we'll figure that out when we do items.
        //might no even add reviving since itll be a single player game
        return;
    }

}