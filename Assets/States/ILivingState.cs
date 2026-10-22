using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface ILivingState
{
    public void Attack(LivingEntity target);
    public void TakeDamage(int damage);
    public void Heal(int amount);
}
