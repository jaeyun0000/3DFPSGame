using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Katana : MonoBehaviour
{
    [SerializeField] private float _damage = 25f;

    private Player _player;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 칼을 든 플레이어 자신은 베지 않는다.
        if (other.GetComponentInParent<Player>() == _player) return;
        if (!other.TryGetComponent(out IDamageable damageable)) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = transform.position - hitPoint;   // 맞은 자리에서 칼날 쪽 방향

        // 칼날 중심이 이미 대상 안에 있으면 ClosestPoint가 칼날 위치 그대로라 방향이 0이 된다. 그때는 플레이어 쪽을 향하게 한다.
        if (hitNormal.sqrMagnitude < 0.0001f)
        {
            hitNormal = _player.transform.position - hitPoint;
            hitNormal.y = 0f;
        }

        DamageInfo damageInfo = new DamageInfo
        {
            Value = _damage,
            HitPoint = hitPoint,
            HitNormal = hitNormal.normalized,
        };

        damageable.TakeDamage(damageInfo);
    }
}
