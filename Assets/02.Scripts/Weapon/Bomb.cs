using UnityEngine;
using UnityEngine.Pool;

// 무언가에 부딛히면 폭발 이펙트를 남기고 사라지는 폭탄
[RequireComponent(typeof(Rigidbody))]
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _damage = 100f;

    private Rigidbody _rigidbody;
    private IObjectPool<Bomb> _pool;
    private bool _isExploded;   // 한 프레임에 여러 번 부딛혀도 한 번만 폭발하도록

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void SetPool(IObjectPool<Bomb> pool)
    {
        _pool = pool;
    }

    // 풀에서 꺼낸 폭탄을 발사 위치에서 던진다.
    // torque: 날아가는 동안 빙글빙글 돌게 하는 회전력
    public void Throw(Vector3 position, Vector3 force, Vector3 torque)
    {
        _isExploded = false;

        transform.SetPositionAndRotation(position, Quaternion.identity);
        _rigidbody.position = position;
        _rigidbody.linearVelocity = Vector3.zero;   // 이전에 사용될 때 남은 속도 제거
        _rigidbody.angularVelocity = Vector3.zero;

        _rigidbody.AddForce(force, ForceMode.Impulse);
        _rigidbody.AddTorque(torque, ForceMode.Impulse);
    }

    // 다른 콜라이더와 부딛힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        if (_isExploded) return;
        _isExploded = true;

        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        ApplyAreaDamage();

        // 폭탄을 풀로 돌려보낸다.
        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 드럼통들을 모두 찾는다.
        // Physics.OverlapSpere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        // 1. 드럼통과 좀비들에게 대미지를 준다.
        foreach (Collider col in colliders)
        {
            DamageInfo damageInfo = new()
            {
                Value = _damage,

                HitPoint = col.ClosestPoint(transform.position),
                HitNormal = (transform.position - col.ClosestPoint(transform.position)).normalized
            };

            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}
