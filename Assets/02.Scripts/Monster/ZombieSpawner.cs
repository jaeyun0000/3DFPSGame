using UnityEngine;
using UnityEngine.Pool;

// 스폰 시간(랜덤)마다 스포너 주변 반경 안의 빈 자리에 좀비를 스폰한다.
// - 스폰할 자리가 없거나 전체 최대 좀비 수에 도달하면 딱 한 마리만 대기하고, 대기 중에는 스폰 시간 카운트를 멈춘다.
// - 대기하던 좀비는 스폰할 수 있게 되는 즉시 스폰되고, 그때부터 다시 스폰 시간을 센다.
public class ZombieSpawner : MonoBehaviour
{
    private const int MaxFindPositionAttempts = 10;   // 빈 자리를 찾을 때 시도하는 횟수

    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private ZombieSpawnManager _spawnManager;

    [Header("스폰 시간")]
    [SerializeField] private float _minSpawnInterval = 3f;
    [SerializeField] private float _maxSpawnInterval = 6f;

    [Header("스폰 위치")]
    [SerializeField] private float _spawnRadius = 10f;        // 스포너 중심에서 랜덤으로 스폰할 반경
    [SerializeField] private float _occupiedCheckRadius = 1f; // 이 반경 안에 좀비가 있으면 그 자리는 피한다.

    private IObjectPool<Zombie> _pool;
    private int _zombieLayerMask;
    private float _spawnTimer;
    private bool _isSpawnWaiting;   // 스폰 시간이 됐지만 스폰하지 못하고 대기 중인 좀비가 있는가?

    private void Awake()
    {
        _pool = new ObjectPool<Zombie>(
            CreateZombie,
            OnGetZombie,
            OnReleaseZombie,
            OnDestroyZombie,
            collectionCheck: true);

        _zombieLayerMask = LayerMask.GetMask("Zombie");
        ResetSpawnTimer();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 대기 중인 좀비가 있으면 스폰 시간을 세지 않는다.
        if (!_isSpawnWaiting)
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f) return;

            _isSpawnWaiting = true;
        }

        TrySpawn();
    }

    private void TrySpawn()
    {
        if (!_spawnManager.CanSpawn) return;
        if (!TryFindSpawnPosition(out Vector3 spawnPosition)) return;

        Zombie zombie = _pool.Get();
        zombie.Spawn(spawnPosition);

        _isSpawnWaiting = false;
        ResetSpawnTimer();
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPosition)
    {
        for (int i = 0; i < MaxFindPositionAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;
            spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (!Physics.CheckSphere(spawnPosition, _occupiedCheckRadius, _zombieLayerMask))
            {
                return true;
            }
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private void ResetSpawnTimer()
    {
        _spawnTimer = Random.Range(_minSpawnInterval, _maxSpawnInterval);
    }

    private Zombie CreateZombie()
    {
        Zombie zombie = Instantiate(_zombiePrefab, transform);
        zombie.SetPool(_pool);
        return zombie;
    }

    private void OnGetZombie(Zombie zombie)
    {
        zombie.gameObject.SetActive(true);
        _spawnManager.AddZombie();
    }

    private void OnReleaseZombie(Zombie zombie)
    {
        zombie.gameObject.SetActive(false);
        _spawnManager.RemoveZombie();
    }

    private void OnDestroyZombie(Zombie zombie)
    {
        Destroy(zombie.gameObject);
    }

    // Scene 뷰에서 스폰 반경을 보여준다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _spawnRadius);
    }
}
