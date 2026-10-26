using System;
using UnityEngine;
using UnityEngine.Pool;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태": 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이": 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"
// 상태별 행동은 ZombieState이 담당하고, Zombie는 상태들이 사용할 기능을 제공한다.


// 좀비가 가질 수 있는 상태
public enum ZombieStateType
{
    Idle,       // 대기: 제자리에서 잠시 쉰다.
    Roam,       // 로밍: 처음 자리 주변을 돌아다닌다.
    Trace,      // 추격: 플레이어를 쫓아간다.
    Comeback,   // 복귀: 처음 자리로 돌아간다.
    Attack,     // 공격: 일정 시간마다 플레이어를 때린다.
    Hit,        // 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death,      // 죽음: 쓰러지고 잠시 뒤 사라진다.
}

[RequireComponent(typeof(CharacterController))]
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8;    // 플레이어를 발견하는 거리
    [SerializeField] private float _combackDistance = 15;   // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _attackDamage = 10f;     // 공격 대미지
    [SerializeField] private float _attackRange = 1.5f;     // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f;  // 공격 간격
    [SerializeField] private float _moveSpeed = 5;          // 이동속도
    [SerializeField] private float _maxHealth = 30;

    [Header("로밍")]
    [SerializeField] private float _roamRadius = 5f;        // 처음 자리에서 돌아다니는 반경
    [SerializeField] private float _minIdleTime = 1f;       // 로밍 전 최소 대기 시간
    [SerializeField] private float _maxIdleTime = 3f;       // 로밍 전 최대 대기 시간

    [Header("피격/사망")]
    [SerializeField] private float _hitDuration = 0.3f;     // 피격 시 멈칫하는 시간
    [SerializeField] private float _deathDuration = 0.5f;   // 쓰러진 뒤 사라질 때까지의 시간

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    private float _health;
    private CharacterController _controller;
    private Vector3 _startPosition;
    private Transform _player;

    private ZombieState _state;

    // 스포너에서 생성된 좀비만 풀을 가진다. (씬에 직접 배치한 좀비는 null)
    private IObjectPool<Zombie> _pool;

    public float MaxHealth => _maxHealth;
    public float Health => _health;
    public ZombieStateType State => _state.State;

    public float DetectDistance => _detectDistance;
    public float ComebackDistance => _combackDistance;
    public float AttackRange => _attackRange;
    public float AttackInterval => _attackInterval;
    public float RoamRadius => _roamRadius;
    public float HitDuration => _hitDuration;
    public float DeathDuration => _deathDuration;
    public Vector3 StartPosition => _startPosition;

    // 체력이 바뀔 때 알린다. (옵저버 패턴)
    public event Action OnHealthChanged;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;

        _state = new ZombieState(this);

        _health = _maxHealth;
        _startPosition = transform.position;
        _state.ChangeState(ZombieStateType.Idle);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        _state.Update();
    }

    public void SetPool(IObjectPool<Zombie> pool)
    {
        _pool = pool;
    }

    // 풀에서 꺼낸 좀비를 지정한 위치에 새 좀비처럼 등장시킨다.
    public void Spawn(Vector3 position)
    {
        // CharacterController가 켜져 있으면 위치 이동이 덮어써질 수 있어서 잠시 끈다.
        _controller.enabled = false;
        transform.position = position;
        _controller.enabled = true;

        _startPosition = position;
        _health = _maxHealth;
        OnHealthChanged?.Invoke();

        _state.ChangeState(ZombieStateType.Idle);
    }

    // 대미지를 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (State == ZombieStateType.Death) return;

        _health -= damageInfo.Value;
        OnHealthChanged?.Invoke();

        if (_health > 0f)
        {
            // 좀비의 자식으로 블러드 스프레이를 생성! -> 위치 수정!
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;

            _state.ChangeState(ZombieStateType.Hit);
        }
        else
        {
            _state.ChangeState(ZombieStateType.Death);
        }
    }

    public float GetDistanceToPlayer()
    {
        return Vector3.Distance(transform.position, _player.position);
    }

    public float GetDistanceFromStart()
    {
        return Vector3.Distance(transform.position, _startPosition);
    }

    // 목표 지점을 바라보며 그 방향으로 이동한다. (높이는 무시)
    public void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    public void MoveTowardsPlayer()
    {
        MoveTowards(_player.position);
    }

    public void AttackPlayer()
    {
        // Todo. 공격 애니메이션 실행

        DamageInfo info = new DamageInfo()
        {
            Value = _attackDamage,
            HitPoint = _player.position,
            HitNormal = (transform.position - _player.position).normalized,
        };

        _player.GetComponent<IDamageable>().TakeDamage(info);
    }

    public float GetRandomIdleTime()
    {
        return UnityEngine.Random.Range(_minIdleTime, _maxIdleTime);
    }

    public void DisableCollision()
    {
        _controller.enabled = false;
    }

    // 사망 처리가 끝나면 풀로 돌아가고, 풀이 없으면 파괴한다.
    public void Despawn()
    {
        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
