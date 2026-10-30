using UnityEngine;

public enum MeleeHitMode
{
    Collider,
    Fan
}

public class PlayerMeleeAttack : MonoBehaviour
{
    private Player _player;
    private Animator _animator;
    private int _meleeLayerIndex;
    [SerializeField] private Collider _katanaCollider;   // 칼날 트리거 콜라이더 (휘두르는 동안만 켠다)
    [SerializeField] private TrailRenderer _slashTrail;  // 칼끝 궤적 이펙트 (휘두르는 동안만 남긴다)

    [SerializeField] private float _damage = 25f;
    [SerializeField] private MeleeHitMode _meleeHitMode = MeleeHitMode.Collider;

    [Header("부채꼴")]
    [SerializeField] private float _range = 2f;
    [SerializeField] private float _angle = 90f;
    [SerializeField] private LayerMask _targetMask;

    private int _wallLayerMask;   // 적과 나 사이를 가로막는 레이어

    private void Awake()
    {
        _player = GetComponent<Player>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayerIndex = _animator.GetLayerIndex("Melee Layer");

        // 휘두르지 않을 때 칼날에 닿아도 맞지 않도록 처음에는 꺼 둔다.
        _katanaCollider.enabled = false;
        _slashTrail.emitting = false;

        _wallLayerMask = LayerMask.GetMask("Wall");
    }

    public void FanAttack()
    {
        if (_meleeHitMode != MeleeHitMode.Fan) return;

        Vector3 origin = _player.transform.position;

        Collider[] colliders = Physics.OverlapSphere(origin, _range, _targetMask);
        foreach (Collider col in colliders)
        {
            // 적의 타격점을 찾는다;
            Vector3 hitPoint = col.ClosestPoint(origin);

            // 방향을 판단한다.
            Vector3 direction = hitPoint - origin;
            direction.y = 0f;
            float angle = Vector3.Angle(_player.transform.forward, direction);
            if (angle > _angle / 2f) continue;

            // 적과 나 사이에 벽이 있다면 때리지 않는다.
            // Linecast는 레이어 번호(NameToLayer)가 아니라 레이어 마스크(GetMask)를 받는다.
            if (Physics.Linecast(origin, hitPoint, _wallLayerMask)) continue;

            if (col.TryGetComponent(out IDamageable damageable))
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.Value = _damage;
                damageInfo.HitPoint = hitPoint;
                damageInfo.HitNormal = (origin - hitPoint).normalized;
                damageable.TakeDamage(damageInfo);
            }
        }
    }

    // 칼 휘두르기 애니메이션 이벤트(PlayerMeleeEvent)에서 호출한다.
    // 궤적은 판정 방식과 상관없이 베는 구간에만 보이고, 콜라이더는 Collider 방식일 때만 켠다.
    public void ActivateCollider()
    {
        _slashTrail.Clear();   // 이전에 휘두른 궤적이 남아 있으면 지운다.
        _slashTrail.emitting = true;

        if (_meleeHitMode != MeleeHitMode.Collider) return;

        GameLogger.Log("카타나 콜라이더 활성화");
        _katanaCollider.enabled = true;
    }

    public void DeactivateCollider()
    {
        _slashTrail.emitting = false;

        if (_meleeHitMode != MeleeHitMode.Collider) return;

        GameLogger.Log("카타나 콜라이더 비활성화");
        _katanaCollider.enabled = false;
    }

    // 씬 뷰에서 플레이어를 선택하면 부채꼴 공격 범위를 노란 선으로 보여준다.
    private void OnDrawGizmosSelected()
    {
        if (_meleeHitMode != MeleeHitMode.Fan) return;

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Gizmos.color = Color.yellow;

        // 앞쪽을 y축으로 -angle/2, +angle/2만큼 돌린 두 방향이 부채꼴의 양 끝이다.
        Vector3 left = Quaternion.Euler(0f, -_angle / 2f, 0f) * forward;
        Vector3 right = Quaternion.Euler(0f, _angle / 2f, 0f) * forward;
        Gizmos.DrawLine(origin, origin + left * _range);
        Gizmos.DrawLine(origin, origin + right * _range);

        // 호: 10도마다 짧은 선을 이어 그린다.
        int count = Mathf.Max(1, Mathf.CeilToInt(_angle / 10f));
        Vector3 previous = origin + left * _range;
        for (int i = 1; i <= count; i++)
        {
            float angle = -_angle / 2f + _angle * i / count;
            Vector3 next = origin + Quaternion.Euler(0f, angle, 0f) * forward * _range;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
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
