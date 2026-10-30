using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    private Player _player;
    private Animator _animator;
    private int _meleeLayerIndex;
    [SerializeField] private Collider _katanaCollider;   // 칼날 트리거 콜라이더 (휘두르는 동안만 켠다)
    [SerializeField] private TrailRenderer _slashTrail;  // 칼끝 궤적 이펙트 (휘두르는 동안만 남긴다)

    private void Awake()
    {
        _player = GetComponent<Player>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayerIndex = _animator.GetLayerIndex("Melee Layer");

        // 휘두르지 않을 때 칼날에 닿아도 맞지 않도록 처음에는 꺼 둔다.
        _katanaCollider.enabled = false;
        _slashTrail.emitting = false;
    }

    // 칼 휘두르기 애니메이션 이벤트(PlayerMeleeEvent)에서 호출한다.
    // 실제로 베는 구간에만 궤적이 보이도록 콜라이더와 함께 켜고 끈다.
    public void ActivateCollider()
    {
        GameLogger.Log("카타나 콜라이더 활성화");
        _katanaCollider.enabled = true;
        _slashTrail.Clear();   // 이전에 휘두른 궤적이 남아 있으면 지운다.
        _slashTrail.emitting = true;
    }

    public void DeactivateCollider()
    {
        GameLogger.Log("카타나 콜라이더 비활성화");
        _katanaCollider.enabled = false;
        _slashTrail.emitting = false;
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
