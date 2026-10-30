using UnityEngine;

// 애니메이션 이벤트는 Animator가 붙은 오브젝트(자식 Model)에만 전달되므로,
// Model에 붙여서 부모 Player의 PlayerMeleeAttack으로 전달한다.
public class PlayerMeleeEvent : MonoBehaviour
{
    private PlayerMeleeAttack _meleeAttack;

    private void Start()
    {
        _meleeAttack = GetComponentInParent<PlayerMeleeAttack>();
    }

    public void ActivateCollider()
    {
        _meleeAttack.ActivateCollider();
    }

    public void DeactivateCollider()
    {
        _meleeAttack.DeactivateCollider();
    }

    public void FanAttack()
    {
        _meleeAttack.FanAttack();
    }
}
