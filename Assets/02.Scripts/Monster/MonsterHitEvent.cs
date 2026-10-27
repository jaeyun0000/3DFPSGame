using UnityEngine;

public class MonsterHitEvent : MonoBehaviour
{
    private Zombie _zombie;

    private void Start()
    {
        _zombie = GetComponentInParent<Zombie>();
    }

    public void HitEvent()
    {
        _zombie.AttackAction();
    }
}
