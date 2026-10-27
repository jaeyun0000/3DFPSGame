using UnityEngine;

// 좀비의 상태별 행동을 모아둔 클래스
// - Enter(): 상태에 들어올 때 한 번 실행 (타이머 초기화 등)
// - UpdateXxx(): 그 상태인 동안 매 프레임 실행
// Zombie는 상태들이 사용할 기능(이동, 공격, 거리 계산)을 제공하고, 언제 무엇을 할지는 이 클래스가 정한다.
public class ZombieState
{
    private const float RoamArriveDistance = 0.5f;      // 로밍 목표 지점에 도착했다고 보는 거리
    private const float MaxRoamTime = 5f;               // 벽에 막혀 도착하지 못해도 이 시간이 지나면 멈춘다.
    private const float ComebackArriveDistance = 1f;    // 처음 자리에 도착했다고 보는 거리
    private const float StuckCheckTime = 1f;            // 추격 중 이 시간 동안
    private const float StuckMoveDistance = 0.5f;       // 이만큼도 못 움직였으면 막힌 것으로 본다.

    private readonly Zombie _zombie;

    private ZombieStateType _state;
    private ZombieStateType _stateBeforeJump;   // 점프가 끝나면 돌아갈 상태
    private float _stateTimer;      // Idle, Roam, Hit, Jump, Death 상태에서 남은 시간
    private float _attackTimer;
    private Vector3 _roamTarget;
    private float _stuckTimer;              // 막혔는지 확인하기까지 남은 시간
    private Vector3 _stuckCheckPosition;    // 막혔는지 비교할 위치 (StuckCheckTime 전 위치)

    public ZombieStateType State => _state;

    public ZombieState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void ChangeState(ZombieStateType nextState)
    {
        GameLogger.Log($"{_state} -> {nextState}");

        if (nextState == ZombieStateType.Jump)
        {
            _stateBeforeJump = _state;
        }

        _state = nextState;
        Enter(nextState);
    }

    public void Update()
    {
        switch (_state)
        {
            case ZombieStateType.Idle:
                UpdateIdle();
                break;

            case ZombieStateType.Roam:
                UpdateRoam();
                break;

            case ZombieStateType.Trace:
                UpdateTrace();
                break;

            case ZombieStateType.Comeback:
                UpdateComeback();
                break;

            case ZombieStateType.Attack:
                UpdateAttack();
                break;

            case ZombieStateType.Hit:
                UpdateHit();
                break;

            case ZombieStateType.Jump:
                UpdateJump();
                break;

            case ZombieStateType.Death:
                UpdateDeath();
                break;
        }
    }

    private void Enter(ZombieStateType state)
    {
        switch (state)
        {
            case ZombieStateType.Idle:
                _zombie.PlayIdleAnimation();
                _zombie.StopMoving();
                _stateTimer = _zombie.GetRandomIdleTime();
                break;

            case ZombieStateType.Roam:
                // Todo. Walk 애니메이션 실행
                Vector2 randomCircle = Random.insideUnitCircle * _zombie.RoamRadius;
                _roamTarget = _zombie.StartPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
                _stateTimer = MaxRoamTime;
                break;

            case ZombieStateType.Trace:
                _zombie.PlayTraceAnimation();
                _stuckTimer = StuckCheckTime;
                _stuckCheckPosition = _zombie.transform.position;
                break;

            case ZombieStateType.Attack:
                _zombie.PlayAttackDelayAnimation();
                _zombie.StopMoving();
                _attackTimer = 0f;
                break;

            case ZombieStateType.Hit:
                // 피격 중에 또 맞으면 멈칫 시간이 처음부터 다시 시작된다.
                _zombie.StopMoving();
                _stateTimer = _zombie.HitDuration;
                break;

            case ZombieStateType.Jump:
                _zombie.BeginJump();
                _stateTimer = _zombie.JumpDuration;
                break;

            case ZombieStateType.Death:
                // 에이전트를 끄기 전에 멈춘다. (꺼진 에이전트에는 명령할 수 없다.)
                _zombie.StopMoving();
                _zombie.DisableCollision();
                _stateTimer = _zombie.DeathDuration;
                break;
        }
    }

    // 대기: 제자리에서 잠시 쉬다가 로밍을 시작한다.
    private void UpdateIdle()
    {
        // 플레이어 가까워지면 Trace 상태로 전이한다.
        if (IsPlayerDetected())
        {
            ChangeState(ZombieStateType.Trace);
            return;
        }

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            ChangeState(ZombieStateType.Roam);
        }
    }

    // 로밍: 처음 자리 주변(RoamRadius 안)의 랜덤 지점으로 걸어간다.
    private void UpdateRoam()
    {
        if (IsPlayerDetected())
        {
            ChangeState(ZombieStateType.Trace);
            return;
        }

        Vector3 toTarget = _roamTarget - _zombie.transform.position;
        toTarget.y = 0f;

        _stateTimer -= Time.deltaTime;
        if (toTarget.magnitude <= RoamArriveDistance || _stateTimer <= 0f)
        {
            ChangeState(ZombieStateType.Idle);
            return;
        }

        _zombie.MoveTowards(_roamTarget);
    }

    // 추격: 플레이어를 쫓아간다.
    private void UpdateTrace()
    {
        // 추격 중에는 집과의 거리와 상관없이 계속 따라가고, 플레이어가 추격 범위를 벗어나면 돌아간다.
        if (_zombie.GetDistanceToPlayer() > _zombie.TraceDistance)
        {
            ChangeState(ZombieStateType.Comeback);
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다.
        if (_zombie.GetDistanceToPlayer() <= _zombie.AttackRange)
        {
            ChangeState(ZombieStateType.Attack);
            return;
        }

        if (TryStartJump(_zombie.PlayerPosition)) return;
        if (TryStartStuckJump()) return;

        _zombie.MoveTowardsPlayer();
    }

    // 복귀: 처음 자리로 돌아간다.
    private void UpdateComeback()
    {
        // 처음 자리에 도착하면 Idle
        // 에이전트는 stoppingDistance만큼 떨어진 곳에서 멈추므로, 그보다 작은 거리로 판정하면 영원히 도착하지 못한다.
        float arriveDistance = Mathf.Max(ComebackArriveDistance, _zombie.StoppingDistance);
        if (_zombie.GetDistanceFromStart() <= arriveDistance)
        {
            ChangeState(ZombieStateType.Idle);
            return;
        }

        if (TryStartJump(_zombie.StartPosition)) return;

        _zombie.MoveTowards(_zombie.StartPosition);
    }

    // 공격: 일정 시간마다 플레이어를 때린다.
    private void UpdateAttack()
    {
        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        if (_zombie.GetDistanceToPlayer() > _zombie.AttackRange)
        {
            ChangeState(ZombieStateType.Trace);
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _zombie.AttackInterval)
        {
            _zombie.AttackPlayer();
            _attackTimer = 0f;
        }
    }

    // 피격: 잠깐 멈칫한 뒤 플레이어를 쫓아간다.
    private void UpdateHit()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            // 맞았으니 쫓아온다.
            ChangeState(ZombieStateType.Trace);
        }
    }

    // 점프: 링크 끝까지 포물선으로 뛰어간 뒤, 뛰기 전 상태로 돌아간다.
    private void UpdateJump()
    {
        _stateTimer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(_stateTimer / _zombie.JumpDuration);
        _zombie.UpdateJump(progress);

        if (_stateTimer > 0f) return;

        _zombie.EndJump();

        // 공중에서 체력이 다 떨어졌으면 착지하자마자 쓰러진다.
        if (_zombie.Health <= 0f)
        {
            _zombie.Die();
            return;
        }

        // 로밍 중이었으면 목표 지점이 새로 정해지지 않도록 대기로 돌아간다.
        ChangeState(_stateBeforeJump == ZombieStateType.Roam ? ZombieStateType.Idle : _stateBeforeJump);
    }

    // 죽음: 쓰러지고 잠시 뒤 사라진다.
    private void UpdateDeath()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            _zombie.Despawn();
        }
    }

    private bool IsPlayerDetected()
    {
        return _zombie.GetDistanceToPlayer() <= _zombie.DetectDistance;
    }

    // 목적지까지 점프하는 게 걸어가는 것보다 빠르면 점프를 시작한다. (판단은 Zombie.TryFindJump가 한다.)
    private bool TryStartJump(Vector3 destination)
    {
        if (!_zombie.TryFindJump(destination)) return false;

        ChangeState(ZombieStateType.Jump);
        return true;
    }

    // 추격 중인데 공격 거리에 닿지 못한 채 제자리에 막혀 있으면, 플레이어 쪽으로 가장 가까워지는 곳에 점프한다.
    // (플레이어가 애매하게 높은 곳에 있어서 걸어서도 못 가고, 공격도 안 닿는 경우)
    private bool TryStartStuckJump()
    {
        _stuckTimer -= Time.deltaTime;
        if (_stuckTimer > 0f) return false;

        Vector3 currentPosition = _zombie.transform.position;
        Vector3 moved = currentPosition - _stuckCheckPosition;
        moved.y = 0f;

        _stuckTimer = StuckCheckTime;
        _stuckCheckPosition = currentPosition;

        if (moved.magnitude >= StuckMoveDistance) return false;
        if (!_zombie.TryFindJump(_zombie.PlayerPosition, isStuck: true)) return false;

        ChangeState(ZombieStateType.Jump);
        return true;
    }
}
