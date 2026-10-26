using UnityEngine;

// 미니맵 카메라: 플레이어 머리 위에서 아래를 내려다본다.
public class MinimapCamera : MonoBehaviour
{
    private Camera _camera;
    [SerializeField] private Transform _target;
    [SerializeField] private float _height = 8f;

    [Header("줌")]
    [SerializeField] private float _zoomStep = 1f;   // 한 번에 확대/축소되는 양
    [SerializeField] private float _minSize = 3f;    // 최대로 확대했을 때 크기
    [SerializeField] private float _maxSize = 15f;   // 최대로 축소했을 때 크기

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ZoomIn();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ZoomOut();
        }
    }

    private void LateUpdate()
    {
        // 1. 대상의 머리 위로 위치를 옮긴다.
        transform.position = _target.position + (Vector3.up * _height);

        // 2. 아래를 보되, 대상이 쳐다보는 방향으로 카메라도 회전한다.
        transform.eulerAngles = new Vector3(90, _target.eulerAngles.y, 0f);
    }

    // 직교 카메라는 orthographicSize가 작을수록 확대된다.
    public void ZoomIn()
    {
        SetSize(_camera.orthographicSize - _zoomStep);
    }

    public void ZoomOut()
    {
        SetSize(_camera.orthographicSize + _zoomStep);
    }

    private void SetSize(float size)
    {
        _camera.orthographicSize = Mathf.Clamp(size, _minSize, _maxSize);
    }
}
