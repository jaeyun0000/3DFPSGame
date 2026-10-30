using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    private Player _player;
    private Animator _animator;
    private int _meleeLayerIndex;
    [SerializeField] private Collider _katanaCollider;   // 칼날 트리거 콜라이더 (휘두르는 동안만 켠다)

    private void Awake()
    {
        _player = GetComponent<Player>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayerIndex = _animator.GetLayerIndex("Melee Layer");

        // 휘두르지 않을 때 칼날에 닿아도 맞지 않도록 처음에는 꺼 둔다.
        _katanaCollider.enabled = false;
    }

    // 칼 휘두르기 애니메이션 이벤트(PlayerMeleeEvent)에서 호출한다.
    public void ActivateCollider()
    {
        GameLogger.Log("카타나 콜라이더 활성화");
        _katanaCollider.enabled = true;
    }

    public void DeactivateCollider()
    {
        GameLogger.Log("카타나 콜라이더 비활성화");
        _katanaCollider.enabled = false;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_player.WeaponMode != WeaponMode.Melee) return;

        if (Input.GetButtonDown("Fire2"))
        {
            _animator.SetTrigger("Slash");
        }
    }
}
