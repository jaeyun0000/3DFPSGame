using System.Collections;
using UnityEngine;

// 체력이 0이 되면 위로 튀어오르며 폭발하는 드럼통
[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour, IDamageable
{
    private const int ExplosionEffectPoolSize = 8;

    // 폭발 범위 안의 콜라이더를 받는 버퍼 (OverlapSphere처럼 매번 배열을 만들지 않도록 모든 드럼통이 같이 쓴다.)
    private static readonly Collider[] HitBuffer = new Collider[32];

    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _damage = 100f;
    [SerializeField] private float _health = 30f;
    [SerializeField] private float _launchPower = 20f;
    [SerializeField] private float _spinPower = 5f;
    [SerializeField] private float _destoryDelay = 3f;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _explosionDelay = 0.2f;

    private Rigidbody _rigidbody;
    private int _damageLayerMask;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _damageLayerMask = LayerMask.GetMask("Drum", "Zombie");
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0)
        {
            return;
        }

        _health -= damageInfo.Value;

        if (_health <= 0)
        {
            StartCoroutine(Explode_Coroutine());
        }
    }

    private IEnumerator Explode_Coroutine()
    {
        // 0. 폭발전대기
        yield return new WaitForSeconds(_explosionDelay);

        // 1. 폭발 이펙트를 생성한다.
        ParticleSystem effect = EffectPool.Play(_explosionEffectPrefab, transform.position, Quaternion.identity, ExplosionEffectPoolSize);
        effect.transform.localScale = Vector3.one * 10f;

        // 2. 위로 튀어오르며 빙글빙글 돈다.
        _rigidbody.AddForce(Vector3.up * _launchPower, ForceMode.Impulse);
        _rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * _spinPower, ForceMode.Impulse);

        // 3. 주위에 폭발을 전달한다. (대미지를 준다.)
        ApplyAreaDamage();

        // 4. 딜레이 후 사라진다.
        yield return new WaitForSeconds(_destoryDelay);
        Destroy(gameObject);
    }

    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 드럼통들을 모두 찾는다.
        // Physics.OverlapSphereNonAlloc(원점, 반지름, 결과 버퍼, 찾을 레이어들): 찾은 개수를 돌려준다.
        int count = Physics.OverlapSphereNonAlloc(transform.position, _explosionRange, HitBuffer, _damageLayerMask);

        // 1. 드럼통과 좀비들에게 대미지를 준다.
        for (int i = 0; i < count; i++)
        {
            Collider col = HitBuffer[i];
            Vector3 hitPoint = col.ClosestPoint(transform.position);
            DamageInfo damageInfo = new DamageInfo
            {
                Value = _damage,
                HitPoint = hitPoint,
                HitNormal = (hitPoint - transform.position).normalized
            };
            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
            // col.GetComponent<Drum>()?.TakeDamage(_damage);
            // col.GetComponent<Zombie>()?.TakeDamage(_damage);
        }
    }
}