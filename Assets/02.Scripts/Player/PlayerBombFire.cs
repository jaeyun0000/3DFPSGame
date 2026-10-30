using System;
using UnityEngine;
using UnityEngine.Pool;

// 마우스 오른쪽 버튼을 누르면 시선이 바라보는 방향으로 수류탄을 던진다.
public class PlayerBombFire : MonoBehaviour
{
    // 애니메이터 트리거 이름 (문자열 비교를 피하려고 해시로 미리 바꿔둔다.)
    private static readonly int ThrowHash = Animator.StringToHash("Throw");

    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _throwPower = 15f;
    [SerializeField] private float _throwPowerUp = 2f;
    [SerializeField] private float _currentThrowPower = 0f;
    [SerializeField] private float _spinPower = 0.4f;   // 던질 때 주는 회전력 (폭탄이 가벼워서 작은 값으로도 빙글빙글 돈다)
    [SerializeField] private int _bombCount = 10;

    [Header("오브젝트 풀")]
    [SerializeField] private int _poolSize = 10;

    private IObjectPool<Bomb> _bombPool;

    private Animator _animator;  // 군인 모델의 애니메이터

    private Player _player;
    private CameraFollow _cameraFollow;  // 시점에 따라 던지는 방향을 알려준다.

    public int BombCount => _bombCount;

    // 폭탄 수가 바뀌면 남은 폭탄 수를 알려준다.
    public event Action<int> OnBombCountChanged;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        _bombPool = new ObjectPool<Bomb>(
            CreateBomb,
            bomb => bomb.gameObject.SetActive(true),
            bomb => bomb.gameObject.SetActive(false),
            bomb => Destroy(bomb.gameObject),
            collectionCheck: true,
            defaultCapacity: _poolSize,
            maxSize: _poolSize);

        _player = GetComponent<Player>();
        _cameraFollow = Camera.main.GetComponent<CameraFollow>();
    }

    private void Start()
    {
        _currentThrowPower = _throwPower;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 일반 모드가 아니면 폭탄을 던지지 않는다. 모으던 힘도 버린다.
        if (_player.WeaponMode != WeaponMode.Normal)
        {
            _currentThrowPower = _throwPower;
            return;
        }

        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButton("Fire2"))
        {
            _currentThrowPower += _throwPowerUp * Time.deltaTime;
        }
        if (Input.GetButtonUp("Fire2"))
        {
            Shoot();
            _currentThrowPower = _throwPower;
        }
    }

    private void Shoot()
    {
        if (_bombCount > 0)
        {
            // 2. 풀에서 수류탄을 꺼내서
            Bomb bomb = _bombPool.Get();

            // 3. 발사 위치에서 시선 방향으로 던지고, 무작위 방향으로 회전시키기
            Vector3 force = _cameraFollow.AimRay.direction * _currentThrowPower;
            Vector3 torque = UnityEngine.Random.onUnitSphere * _spinPower;
            bomb.Throw(_firePoint.position, force, torque);

            // 4. 오른팔만 던지는 동작을 한다. (ThrowLayer가 RightArmMask로 오른팔만 덮는다.)
            _animator.SetTrigger(ThrowHash);

            _bombCount--;
            OnBombCountChanged?.Invoke(_bombCount);
        }
    }

    private Bomb CreateBomb()
    {
        Bomb bomb = Instantiate(_bombPrefab);
        bomb.SetPool(_bombPool);
        return bomb;
    }
}