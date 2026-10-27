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
    Jump,       // 점프: NavMesh 링크(낭떠러지, 높은 곳, 장애물)를 포물선으로 뛰어넘는다.
    Death,      // 죽음: 쓰러지고 잠시 뒤 사라진다.
}

[RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
public class Zombie : MonoBehaviour, IDamageable
{
    private const float JumpCheckInterval = 0.3f;   // 점프할지 판단하는 간격 (매 프레임 하면 무겁다)
    private const float MinJumpGain = 2f;           // 걸어가는 것보다 이만큼(m) 이상 빨라야 점프한다. (거의 같으면 그냥 걷는다)
    private const int JumpSearchDirections = 8;     // 착지 지점을 찾을 방향 수 (목적지 방향 + 주변)
    private const float JumpClearance = 1f;         // 낮게 뛸 때 출발/착지 중 높은 쪽보다 이만큼 더 솟는다. (필요한 만큼만 뛴다)
    private const float StuckJumpMinCloser = 0.5f;  // 막혔을 때 점프는 플레이어에게 이만큼 이상 가까워져야 한다.
    private const float GroundSearchRadius = 3f;    // 목표 아래에 걸을 수 있는 바닥이 없을 때 주변에서 찾는 반경

    // 애니메이터 트리거 이름 (문자열 비교를 피하려고 해시로 미리 바꿔둔다.)
    private static readonly int IdleToMoveHash = Animator.StringToHash("IdleToMove");
    private static readonly int MoveToIdleHash = Animator.StringToHash("MoveToIdle");
    private static readonly int MoveAttackDelayHash = Animator.StringToHash("MoveAttackDelay");
    private static readonly int StartAttackHash = Animator.StringToHash("StartAttack");
    private static readonly int AttackMoveHash = Animator.StringToHash("AttackMove");
    private static readonly int HitHash = Animator.StringToHash("Hit");

    // 트리거는 소비되기 전까지 남아 있어서, 새 트리거를 켤 때 나머지는 모두 지운다.
    private static readonly int[] AllTriggerHashes =
    {
        IdleToMoveHash, MoveToIdleHash, MoveAttackDelayHash, StartAttackHash, AttackMoveHash, HitHash,
    };

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
    [SerializeField] private float _hitDuration = 1f;       // 피격 시 멈칫하는 시간 (Hit 애니메이션 길이와 같게: 2초 클립 x 속도 2)
    [SerializeField] private float _deathDuration = 2f;     // 쓰러진 뒤 사라질 때까지의 시간

    [Header("점프")]
    // 점프력 하나로 오를 수 있는 높이, 뛰어내릴 수 있는 깊이, 갈 수 있는 거리, 체공 시간, 궤적이 모두 정해진다.
    [SerializeField] private float _jumpPower = 2f;             // 점프력: 출발 지점에서 최대로 솟아오르는 높이(m)
    [SerializeField] private float _jumpCooldown = 1f;          // 착지 후 다시 점프하기까지 기다리는 시간

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;
    [SerializeField] private float _bloodSprayDuration = 0.5f;  // 피 이펙트가 남아 있는 시간

    private float _health;
    private CapsuleCollider _collider;  // 총(Raycast)과 폭발(OverlapSphere)에 맞기 위한 충돌체
    private Vector3 _startPosition;
    private Transform _player;

    private ZombieState _state;

    private NavMeshAgent _agent;
    private Animator _animator;     // 자식 Model에 붙어 있다.
    private bool _isAttackAnimation;    // 애니메이터가 공격 쪽(AttackDelay, Attack) 상태에 있는가?

    private Vector3 _jumpStart;         // 점프를 시작한 위치
    private Vector3 _jumpEnd;           // 점프로 착지할 위치
    private float _jumpTime;            // 이번 점프의 체공 시간
    private float _jumpApex;            // 이번 점프에서 출발 지점보다 솟는 높이 (점프력 이하)
    private bool _isJumpFromLink;       // NavMesh 링크(자동 생성된 낙하 링크 등)를 타는 점프인가?
    private float _nextJumpCheckTime;   // 다음에 점프를 판단할 시각
    private float _jumpReadyTime;       // 착지 후 쿨타임이 끝나는 시각
    private int _jumpBlockLayerMask;    // 점프 궤적을 막는지 검사할 레이어 (좀비, 플레이어 제외)
    private NavMeshPath _path;          // 경로 길이 계산용 (매번 만들지 않도록 재사용, Awake에서 만든다.)
    private readonly RaycastHit[] _groundHits = new RaycastHit[8];  // 한 지점의 여러 층 바닥(다리 위, 다리 밑)을 받는 버퍼

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
    public float JumpDuration => _jumpTime;
    public Vector3 StartPosition => _startPosition;
    public float StoppingDistance => _agent.stoppingDistance;
    public Vector3 PlayerPosition => _player.position;

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

        // 링크를 에이전트가 직선으로 미끄러지듯 지나가지 않게 하고, 점프는 직접 처리한다.
        _agent.autoTraverseOffMeshLink = false;
        _jumpBlockLayerMask = ~LayerMask.GetMask("Zombie", "Player");

        // NavMeshPath는 필드 초기화(생성자)에서 만들면 Unity가 막으므로 여기서 만든다.
        _path = new NavMeshPath();

        // 애니메이터도 Idle 진입에서 사용하므로 상태를 바꾸기 전에 찾아둔다.
        _animator = GetComponentInChildren<Animator>();

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

        // 공중에서는 멈칫하거나 쓰러지지 않는다. (공중에 멈춰 버리므로)
        // 대미지만 받고, 착지한 뒤에 체력이 없으면 그때 죽는다.
        if (State == ZombieStateType.Jump)
        {
            if (_health > 0f)
            {
                SpawnBloodSpray(damageInfo);
            }
            return;
        }

        if (_health > 0f)
        {
            SpawnBloodSpray(damageInfo);

            PlayHitAnimation();
            _state.ChangeState(ZombieStateType.Hit);
        }
        else
        {
            Die();
        }
    }

    // 쓰러지는 애니메이션을 재생하고 사망 상태로 바꾼다.
    public void Die()
    {
        _animator.SetTrigger("Death");
        _state.ChangeState(ZombieStateType.Death);
    }

    // 목적지까지 점프해서 가는 게 걸어가는 것보다 빠른지 판단한다. 빠르면 착지 지점을 정해 두고 true를 돌려준다.
    // - 플레이어가 높은 곳에 있음, 앞을 막은 장애물, 앞에 낭떠러지 같은 상황을 모두 같은 방법으로 판단한다.
    //   "걸어서 가는 경로 길이"와 "점프 비용 + 착지 후 남은 경로 길이"를 비교해서, 점프가 MinJumpGain 이상 짧으면 뛴다.
    //   점프 비용은 체공 시간 동안 걸어갈 수 있는 거리로 친다. (시간 기준으로 비교하기 위해)
    // - isStuck: 추격 중인데 제자리에 막혀 있을 때. 빠른지 따지지 않고, 플레이어에게 가장 가까워지는 곳으로 뛴다.
    public bool TryFindJump(Vector3 destination, bool isStuck = false)
    {
        if (!_agent.isOnNavMesh) return false;

        // 자동 생성된 낙하 링크 위에 올라섰으면 그 링크를 따라 뛴다.
        // (링크는 경로 탐색이 이미 "가장 빠른 길"로 고른 것이다.)
        if (_agent.isOnOffMeshLink)
        {
            _isJumpFromLink = true;
            _jumpEnd = _agent.currentOffMeshLinkData.endPos;

            float linkHeightDiff = _jumpEnd.y - _agent.currentOffMeshLinkData.startPos.y;
            _jumpApex = Mathf.Max(GetLowArcApex(linkHeightDiff), linkHeightDiff);
            _jumpTime = GetJumpAirTime(linkHeightDiff, _jumpApex);
            return true;
        }

        // 착지 직후에는 쿨타임만큼 쉰다.
        if (Time.time < _jumpReadyTime) return false;

        // 판단은 무거우므로 평소에는 일정 간격으로만 한다. (막혔을 때는 ZombieState가 따로 간격을 잰다.)
        if (!isStuck)
        {
            if (Time.time < _nextJumpCheckTime) return false;
            _nextJumpCheckTime = Time.time + JumpCheckInterval;
        }

        Vector3 from = _agent.nextPosition - Vector3.up * _agent.baseOffset;   // 발 위치
        Vector3 goal = destination;
        float bestCost;

        if (isStuck)
        {
            // 막혔을 때: 지금보다 플레이어에게 확실히 가까워지는 곳이어야 한다.
            bestCost = Vector3.Distance(from + Vector3.up * _agent.baseOffset, destination) - StuckJumpMinCloser;
        }
        else
        {
            if (!TryGetGroundPoint(destination, out goal)) return false;

            // 걸어서 가는 비용. 경로가 끊겨 있으면(못 가는 곳) 무한대로 본다.
            float walkCost = GetPathLength(from, goal);

            // 점프해도 결국 목적지까지의 수평 거리만큼은 가야 하므로, 걷는 길이 거의 직선이면 점프로 아낄 게 없다.
            Vector3 toGoalFlat = goal - from;
            toGoalFlat.y = 0f;
            if (walkCost - MinJumpGain <= toGoalFlat.magnitude) return false;

            bestCost = walkCost - MinJumpGain;
        }

        // 목적지 방향을 먼저, 그다음 주변 방향을 돌아가며 착지 지점을 찾는다.
        // (플레이어가 바로 아래에 있으면 목적지 방향으로는 다리에 막히므로, 옆으로 뛰어내려야 한다.)
        Vector3 toGoal = goal - from;
        toGoal.y = 0f;
        Vector3 baseDirection = toGoal.sqrMagnitude > 0.01f ? toGoal.normalized : transform.forward;
        float maxReach = _moveSpeed * GetJumpAirTime(-_jumpPower, _jumpPower);   // 가장 깊이 뛰어내릴 때 가장 멀리 간다.
        bool found = false;

        for (int i = 0; i < JumpSearchDirections; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, 360f / JumpSearchDirections * i, 0f) * baseDirection;
            for (float distance = 1f; distance <= maxReach; distance += 0.5f)
            {
                // 그 지점에 있는 바닥을 위층, 아래층 모두 후보로 본다. (다리 위와 다리 밑)
                Vector3 rayStart = from + direction * distance + Vector3.up * (_jumpPower + 0.5f);
                float rayLength = _jumpPower * 2f + 0.5f;
                int hitCount = Physics.RaycastNonAlloc(rayStart, Vector3.down, _groundHits, rayLength, _jumpBlockLayerMask, QueryTriggerInteraction.Ignore);

                for (int h = 0; h < hitCount; h++)
                {
                    // 그 바닥이 실제로 걸어 다닐 수 있는 곳(NavMesh)인지 확인한다. (드럼통 위 같은 곳은 제외된다.)
                    if (!NavMesh.SamplePosition(_groundHits[h].point, out NavMeshHit navHit, 0.3f, NavMesh.AllAreas)) continue;

                    if (TryEvaluateLanding(from, navHit.position, distance, goal, isStuck, ref bestCost))
                    {
                        found = true;
                    }
                }
            }
        }

        _isJumpFromLink = false;
        return found;
    }

    // 착지 후보로 점프할 수 있고, 지금까지 찾은 것보다 좋으면 착지 지점으로 정한다.
    // - 평소: 점프 비용 + 착지 후 남은 경로가 가장 짧은 곳
    // - 막혔을 때(isStuck): 플레이어에게 가장 가까운 곳
    private bool TryEvaluateLanding(Vector3 from, Vector3 landing, float horizontalDistance, Vector3 goal, bool isStuck, ref float bestCost)
    {
        // 점프력만큼 오르거나 뛰어내릴 수 있다.
        float heightDiff = landing.y - from.y;
        if (Mathf.Abs(heightDiff) > _jumpPower) return false;

        // 무거운 검사(경로, 궤적) 전에, 가장 좋게 봐도 지금까지 찾은 것보다 못하면 바로 버린다.
        float cost;
        if (isStuck)
        {
            cost = Vector3.Distance(landing + Vector3.up * _agent.baseOffset, goal);
            if (cost >= bestCost) return false;
        }
        else
        {
            float shortestAirTime = GetJumpAirTime(heightDiff, GetLowArcApex(heightDiff));
            if (_moveSpeed * shortestAirTime + Vector3.Distance(landing, goal) >= bestCost) return false;
            cost = 0f;
        }

        if (!TryGetJumpArc(from, landing, horizontalDistance, out float apex, out float airTime)) return false;

        if (!isStuck)
        {
            cost = _moveSpeed * airTime + GetPathLength(landing, goal);
            if (cost >= bestCost) return false;
        }

        bestCost = cost;
        _jumpEnd = landing;
        _jumpApex = apex;
        _jumpTime = airTime;
        return true;
    }

    // 착지 지점까지 뛸 수 있는 궤적을 찾는다.
    // 먼저 필요한 만큼만 낮게 뛰어 보고(천장이나 구조물에 덜 걸리고 빠르다), 막히면 점프력을 다 써서 높이 뛴다.
    private bool TryGetJumpArc(Vector3 from, Vector3 landing, float horizontalDistance, out float apex, out float airTime)
    {
        float heightDiff = landing.y - from.y;
        float lowApex = GetLowArcApex(heightDiff);

        for (int i = 0; i < 2; i++)
        {
            apex = i == 0 ? lowApex : _jumpPower;
            if (i == 1 && Mathf.Approximately(apex, lowApex)) break;

            // 공중에서도 평소 이동 속도로만 나아간다. (그래야 평지에서 점프로 더 빨리 가는 꼼수를 쓰지 않는다.)
            airTime = GetJumpAirTime(heightDiff, apex);
            if (horizontalDistance > _moveSpeed * airTime) continue;
            if (!IsJumpPathClear(from, landing, airTime, apex)) continue;

            return true;
        }

        apex = 0f;
        airTime = 0f;
        return false;
    }

    // 낮게 뛸 때 솟는 높이: 출발/착지 중 높은 쪽보다 JumpClearance만큼 위 (점프력을 넘지 않는다)
    private float GetLowArcApex(float heightDiff)
    {
        return Mathf.Min(_jumpPower, Mathf.Max(heightDiff, 0f) + JumpClearance);
    }

    // 위치 바로 아래의 바닥(NavMesh 위)을 찾는다.
    // 그냥 가장 가까운 NavMesh를 찾으면, 다리 밑에 있는 플레이어의 위치가 바로 위 다리 위로 잡히기 때문이다.
    // 바로 아래가 걸을 수 없는 곳(드럼통 위)이면 그 주변에서 가장 가까운 바닥(드럼통 옆)을 찾는다.
    private bool TryGetGroundPoint(Vector3 position, out Vector3 ground)
    {
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 10f, _jumpBlockLayerMask, QueryTriggerInteraction.Ignore)
            && NavMesh.SamplePosition(hit.point, out NavMeshHit groundHit, GroundSearchRadius, NavMesh.AllAreas))
        {
            ground = groundHit.position;
            return true;
        }

        if (NavMesh.SamplePosition(position, out NavMeshHit nearHit, GroundSearchRadius, NavMesh.AllAreas))
        {
            ground = nearHit.position;
            return true;
        }

        ground = position;
        return false;
    }

    // 두 지점 사이를 걸어서 가는 경로 길이. 갈 수 없으면 무한대.
    private float GetPathLength(Vector3 from, Vector3 to)
    {
        if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _path)) return float.PositiveInfinity;
        if (_path.status != NavMeshPathStatus.PathComplete) return float.PositiveInfinity;

        float length = 0f;
        Vector3[] corners = _path.corners;
        for (int i = 1; i < corners.Length; i++)
        {
            length += Vector3.Distance(corners[i - 1], corners[i]);
        }
        return length;
    }

    // 점프 궤적(포물선)을 따라가며 벽이나 천장에 부딪히지 않는지 검사한다.
    private bool IsJumpPathClear(Vector3 from, Vector3 to, float airTime, float apex)
    {
        const int segments = 8;
        Vector3 bodyOffset = Vector3.up * _agent.baseOffset;   // 발이 아니라 몸 가운데 높이로 검사한다.

        Vector3 previous = from + bodyOffset;
        for (int i = 1; i <= segments; i++)
        {
            Vector3 point = GetJumpPosition(from, to, airTime, apex, (float)i / segments) + bodyOffset;
            if (Physics.Linecast(previous, point, _jumpBlockLayerMask, QueryTriggerInteraction.Ignore)) return false;
            previous = point;
        }
        return true;
    }

    // apex만큼 솟았다가 heightDiff(착지 높이 - 출발 높이)만큼 높은 곳에 내려앉을 때까지의 체공 시간.
    // 올라가는 시간: apex 높이까지 솟는 시간 / 내려오는 시간: 꼭대기에서 착지 높이까지 떨어지는 시간
    private float GetJumpAirTime(float heightDiff, float apex)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        float riseTime = Mathf.Sqrt(2f * apex / gravity);
        float fallTime = Mathf.Sqrt(2f * Mathf.Max(0f, apex - heightDiff) / gravity);
        return riseTime + fallTime;
    }

    // 점프 진행도(0~1)일 때의 위치.
    // 수평으로는 일정한 속도로, 수직으로는 apex 높이까지 솟았다가 중력으로 떨어지는 실제 포물선을 따른다.
    private Vector3 GetJumpPosition(Vector3 from, Vector3 to, float airTime, float apex, float progress)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        float jumpSpeed = Mathf.Sqrt(2f * gravity * apex);   // apex 높이까지 솟기 위한 처음 속도
        float time = airTime * progress;

        Vector3 position = Vector3.Lerp(from, to, progress);
        position.y = from.y + jumpSpeed * time - 0.5f * gravity * time * time;
        return position;
    }

    // TryFindJump에서 정한 착지 지점을 향해 점프를 시작한다.
    public void BeginJump()
    {
        _jumpStart = transform.position;

        // 착지 지점은 바닥 높이라서, 에이전트의 기준 높이(baseOffset)만큼 올려야 몸이 바닥에 묻히지 않는다.
        _jumpEnd += Vector3.up * _agent.baseOffset;

        // 점프하는 동안은 에이전트가 위치를 덮어쓰지 않게 하고, 직접 옮긴다.
        _agent.updatePosition = false;

        // 뛰는 방향을 바라본다. (높이는 무시)
        Vector3 direction = _jumpEnd - _jumpStart;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.forward = direction.normalized;
        }
    }

    // 점프 진행도(0~1)에 맞춰 포물선 위의 위치로 옮긴다.
    public void UpdateJump(float progress)
    {
        transform.position = GetJumpPosition(_jumpStart, _jumpEnd, _jumpTime, _jumpApex, progress);
    }

    // 착지하고 에이전트를 착지 지점으로 옮겨 다시 경로를 따라가게 한다.
    public void EndJump()
    {
        if (_isJumpFromLink)
        {
            _agent.CompleteOffMeshLink();
        }

        _agent.Warp(_jumpEnd);
        _agent.updatePosition = true;

        _jumpReadyTime = Time.time + _jumpCooldown;
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

        // 목표 바로 아래 바닥으로 간다. (다리 밑에 있는 플레이어를 다리 위에서 기다리지 않도록)
        TryGetGroundPoint(targetPosition, out Vector3 ground);

        _agent.isStopped = false;
        _agent.SetDestination(ground);
    }

    // 이동을 멈춘다.
    // SetDestination은 한 번 호출하면 도착할 때까지 계속 가므로, 멈춰야 하는 상태에서 직접 호출한다.
    public void StopMoving()
    {
        if (!_agent.isOnNavMesh) return;

        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;     // 남은 속도 때문에 미끄러지지 않게 바로 멈춘다.
    }

    // 대기 애니메이션으로 전환한다. (Move -> Idle)
    public void PlayIdleAnimation()
    {
        _isAttackAnimation = false;
        SetAnimationTrigger(MoveToIdleHash);
    }

    // 추격(달리기) 애니메이션으로 전환한다.
    // 공격 중이었으면 Attack -> Move, 아니면 Idle -> Move 트리거를 쓴다.
    public void PlayTraceAnimation()
    {
        SetAnimationTrigger(_isAttackAnimation ? AttackMoveHash : IdleToMoveHash);
        _isAttackAnimation = false;
    }

    // 공격 대기 애니메이션으로 전환한다. (Move -> AttackDelay)
    public void PlayAttackDelayAnimation()
    {
        _isAttackAnimation = true;
        SetAnimationTrigger(MoveAttackDelayHash);
    }

    // 피격 애니메이션으로 전환한다. (Any State -> Hit, 끝나면 애니메이터가 알아서 Move로 간다.)
    // 공격 중에 맞아도 공격 모션은 끊기므로 공격 상태가 아니게 된다.
    private void PlayHitAnimation()
    {
        _isAttackAnimation = false;
        SetAnimationTrigger(HitHash);
    }

    // 다른 트리거를 모두 지우고 하나만 켠다.
    // 남아 있는 트리거가 있으면 나중에 엉뚱한 상태로 바로 전환되기 때문이다.
    private void SetAnimationTrigger(int triggerHash)
    {
        foreach (int hash in AllTriggerHashes)
        {
            _animator.ResetTrigger(hash);
        }
        _animator.SetTrigger(triggerHash);
    }

    public void MoveTowardsPlayer()
    {
        MoveTowards(_player.position);
    }

    public void AttackPlayer()
    {
        // 공격 애니메이션 실행 (AttackDelay -> Attack, 끝나면 애니메이터가 알아서 AttackDelay로 돌아간다.)
        SetAnimationTrigger(StartAttackHash);
    }

    // 플레이어에게 실제로 대미지를 준다.
    // 공격 애니메이션 이벤트(MonsterHitEvent.HitEvent)에서 호출한다.
    public void AttackAction()
    {
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
