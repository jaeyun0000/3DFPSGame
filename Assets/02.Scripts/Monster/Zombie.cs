using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

[RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8;    // 플레이어를 발견하는 거리
    [SerializeField] private float _traceDistance = 12;     // 추격 중 플레이어가 이만큼 멀어지면 포기하고 돌아가는 거리
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
    [SerializeField] private float _bloodSprayDuration = 0.5f;  // 피 이펙트가 남아 있는 시간

    private float _health;
    private CapsuleCollider _collider;  // 총(Raycast)과 폭발(OverlapSphere)에 맞기 위한 충돌체
    private Vector3 _startPosition;
    private Transform _player;

    private ZombieState _state;

    private NavMeshAgent _agent;

    // 스포너에서 생성된 좀비만 풀을 가진다. (씬에 직접 배치한 좀비는 null)
    private IObjectPool<Zombie> _pool;

    // 풀에서 다시 꺼낼 때 남은 피를 지우기 위해 생성한 피 이펙트를 기억한다.
    private readonly List<GameObject> _bloodSprays = new List<GameObject>();

    public float MaxHealth => _maxHealth;
    public float Health => _health;
    public ZombieStateType State => _state.State;

    public float DetectDistance => _detectDistance;
    public float TraceDistance => _traceDistance;
    public float AttackRange => _attackRange;
    public float AttackInterval => _attackInterval;
    public float RoamRadius => _roamRadius;
    public float HitDuration => _hitDuration;
    public float DeathDuration => _deathDuration;
    public Vector3 StartPosition => _startPosition;
    public float StoppingDistance => _agent.stoppingDistance;

    // 체력이 바뀔 때 알린다. (옵저버 패턴)
    public event Action OnHealthChanged;

    private void Awake()
    {
        _collider = GetComponent<CapsuleCollider>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;

        // 길을 찾아 걷게 해주는 컴포넌트 NavMeshAgent를 찾아서 참조
        // (아래 Idle 진입에서 이동을 멈추므로 상태를 바꾸기 전에 찾아둔다.)
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;

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
        // 죽을 때 꺼둔 충돌과 에이전트를 다시 켠다.
        _collider.enabled = true;
        _agent.enabled = true;

        // 에이전트는 transform.position을 바꾸면 위치를 덮어쓰므로 Warp로 순간이동한다.
        _agent.Warp(position);

        _startPosition = position;
        _health = _maxHealth;
        OnHealthChanged?.Invoke();

        ClearBloodSprays();

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
            SpawnBloodSpray(damageInfo);

            _state.ChangeState(ZombieStateType.Hit);
        }
        else
        {
            _state.ChangeState(ZombieStateType.Death);
        }
    }

    // 좀비의 자식으로 블러드 스프레이를 생성하고, 지속 시간이 지나면 제거한다.
    // (이펙트가 반복 재생으로 설정되어 있어서 직접 지우지 않으면 계속 피가 나온다.)
    private void SpawnBloodSpray(DamageInfo damageInfo)
    {
        // 이미 지워진 이펙트는 Unity에서 null로 판정되므로 리스트에서 정리한다.
        _bloodSprays.RemoveAll(spray => spray == null);

        GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
        bloodSpray.transform.position = damageInfo.HitPoint;
        bloodSpray.transform.forward = damageInfo.HitNormal;

        _bloodSprays.Add(bloodSpray);
        Destroy(bloodSpray, _bloodSprayDuration);
    }

    // 남아 있는 블러드 스프레이를 모두 제거한다.
    private void ClearBloodSprays()
    {
        foreach (GameObject bloodSpray in _bloodSprays)
        {
            if (bloodSpray != null)
            {
                Destroy(bloodSpray);
            }
        }
        _bloodSprays.Clear();
    }

    public float GetDistanceToPlayer()
    {
        return Vector3.Distance(transform.position, _player.position);
    }

    public float GetDistanceFromStart()
    {
        return Vector3.Distance(transform.position, _startPosition);
    }

    // 목표 지점까지 NavMesh 경로를 따라 이동한다. (방향 전환은 에이전트가 알아서 한다.)
    public void MoveTowards(Vector3 targetPosition)
    {
        // NavMesh 위에 있지 않은 에이전트에 명령하면 에러가 나므로 무시한다.
        if (!_agent.isOnNavMesh) return;

        _agent.isStopped = false;
        _agent.SetDestination(targetPosition);
    }

    // 이동을 멈춘다.
    // SetDestination은 한 번 호출하면 도착할 때까지 계속 가므로, 멈춰야 하는 상태에서 직접 호출한다.
    public void StopMoving()
    {
        if (!_agent.isOnNavMesh) return;

        _agent.isStopped = true;
        _agent.ResetPath();
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
        _collider.enabled = false;
        _agent.enabled = false;
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
