using UnityEngine;

// 카메라 시점: [ 키를 누르면 왼쪽(이전), ] 키를 누르면 오른쪽(다음) 시점으로 바뀐다.
public enum CameraViewMode
{
    FirstPerson,    // 1인칭: 플레이어 머리에서 마우스로 둘러본다.
    ThirdPerson,    // 3인칭: 플레이어 뒤에서 마우스로 둘러본다. (위아래 포함)
    QuarterView,    // 쿼터뷰: 비스듬히 위에서 고정된 각도로 내려다본다.
    BackView,       // 빽뷰: 플레이어 등 정중앙 뒤에서 마우스로 둘러본다. (위아래 포함, 3인칭보다 멀리)
}

// 시점에 맞춰 카메라 위치를 옮긴다.
// 회전은 CameraRotate가 마우스로 정하고, 쿼터뷰만 여기서 고정된 각도로 정한다.
public class CameraFollow : MonoBehaviour
{
    private const int ViewModeCount = 4;
    private const float CollisionRadius = 0.2f;   // 벽에 파묻히지 않도록 카메라를 감싸는 구의 반지름

    [SerializeField] private Transform _target;   // 플레이어 머리 위치 (CameraPosition)
    [SerializeField] private CameraViewMode _viewMode = CameraViewMode.FirstPerson;

    [Header("3인칭")]
    [SerializeField] private float _thirdPersonDistance = 3.5f;
    [SerializeField] private float _shoulderOffset = 0.6f;     // 화면 가운데를 플레이어 머리가 가리지 않도록 오른쪽 어깨 너머로 본다.

    [Header("쿼터뷰")]
    [SerializeField] private float _quarterDistance = 14f;
    [SerializeField] private Vector3 _quarterAngle = new Vector3(45f, 45f, 0f);

    [Header("빽뷰")]
    [SerializeField] private float _backDistance = 5f;
    [SerializeField] private float _backHeight = 1f;           // 화면 가운데(조준점)를 플레이어가 가리지 않도록 머리보다 이만큼 위에서 본다.

    private Transform _player;
    private int _collisionLayerMask;   // 카메라가 막히는 레이어 (플레이어, 좀비는 통과)

    public CameraViewMode ViewMode => _viewMode;

    // 총과 폭탄이 날아갈 방향
    // 화면 가운데를 향하고, 마우스로 위아래를 보지 않는 쿼터뷰만 플레이어가 바라보는 방향으로 쏜다.
    public Ray AimRay
    {
        get
        {
            if (_viewMode == CameraViewMode.QuarterView)
            {
                return new Ray(_target.position, _player.forward);
            }
            return new Ray(transform.position, transform.forward);
        }
    }

    private void Awake()
    {
        if (_target == null)
        {
            Debug.LogWarning("[CameraFollow] Camera Target null");
            return;
        }

        _player = _target.GetComponentInParent<Player>().transform;
        _collisionLayerMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player", "Zombie");
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            ChangeViewMode(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            ChangeViewMode(1);
        }
    }

    private void LateUpdate()
    {
        switch (_viewMode)
        {
            case CameraViewMode.FirstPerson:
                transform.position = _target.position;
                break;

            case CameraViewMode.ThirdPerson:
                // CameraRotate가 정한 방향의 반대쪽(뒤)으로 물러난다.
                Vector3 pivot = _target.position + transform.right * _shoulderOffset;
                transform.position = GetBlockedPosition(pivot, -transform.forward, _thirdPersonDistance);
                break;

            case CameraViewMode.QuarterView:
                transform.rotation = Quaternion.Euler(_quarterAngle);
                transform.position = _player.position - transform.forward * _quarterDistance;
                break;

            case CameraViewMode.BackView:
                // 3인칭과 같이 뒤로 물러나되, 어깨가 아니라 등 정중앙 위에서 더 멀리 본다.
                Vector3 backPivot = _target.position + Vector3.up * _backHeight;
                transform.position = GetBlockedPosition(backPivot, -transform.forward, _backDistance);
                break;
        }
    }

    // direction: +1이면 오른쪽(다음), -1이면 왼쪽(이전). 끝에서 넘어가면 반대쪽 끝으로 돈다.
    private void ChangeViewMode(int direction)
    {
        int next = ((int)_viewMode + direction + ViewModeCount) % ViewModeCount;
        _viewMode = (CameraViewMode)next;
        GameLogger.Log($"카메라 시점: {_viewMode}");
    }

    // pivot에서 direction 방향으로 distance만큼 물러나되, 벽에 막히면 벽 앞에서 멈춘다.
    private Vector3 GetBlockedPosition(Vector3 pivot, Vector3 direction, float distance)
    {
        if (Physics.SphereCast(pivot, CollisionRadius, direction, out RaycastHit hit, distance, _collisionLayerMask, QueryTriggerInteraction.Ignore))
        {
            distance = hit.distance;
        }
        return pivot + direction * distance;
    }
}
