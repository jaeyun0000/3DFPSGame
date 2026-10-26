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

    private readonly Zombie _zombie;

    private ZombieStateType _state;
    private float _stateTimer;      // Idle, Roam, Hit, Death 상태에서 남은 시간
    private float _attackTimer;
    private Vector3 _roamTarget;

    public ZombieStateType State => _state;

    public ZombieState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void ChangeState(ZombieStateType nextState)
    {
        GameLogger.Log($"{_state} -> {nextState}");
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
                // Todo. Idle 애니메이션 실행
                _stateTimer = _zombie.GetRandomIdleTime();
                break;

            case ZombieStateType.Roam:
                // Todo. Walk 애니메이션 실행
                Vector2 randomCircle = Random.insideUnitCircle * _zombie.RoamRadius;
                _roamTarget = _zombie.StartPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
                _stateTimer = MaxRoamTime;
                break;

            case ZombieStateType.Attack:
                _attackTimer = 0f;
                break;

            case ZombieStateType.Hit:
                // 피격 중에 또 맞으면 멈칫 시간이 처음부터 다시 시작된다.
                _stateTimer = _zombie.HitDuration;
                break;

            case ZombieStateType.Death:
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
        // Todo. Run 애니메이션 실행

        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (_zombie.GetDistanceFromStart() > _zombie.ComebackDistance)
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

        _zombie.MoveTowardsPlayer();
    }

    // 복귀: 처음 자리로 돌아간다.
    private void UpdateComeback()
    {
        // 처음 자리에 도착하면 Idle
        if (_zombie.GetDistanceFromStart() < ComebackArriveDistance)
        {
            ChangeState(ZombieStateType.Idle);
            return;
        }

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
}
