using UnityEngine;

// 미니맵 카메라: 플레이어 머리 위에서 아
public class MinimapCamera : MonoBehaviour
{
    private Camera _camera;
    [SerializeField] private Transform _target;
    [SerializeField] private float _height = 8f;

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        // Todo. ZoomIn/ZoomOut 함수로 분리
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            _camera.orthographicSize -= 1f;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            _camera.orthographicSize += 1f;
        }
    }

    private void LateUpdate()
    {
        // 1. 대상의 머리 위로 위치를 옮긴다.
        transform.position = _target.position + (Vector3.up * _height);

        // 2. 아래를 보되, 대상이 쳐다보는 방향으로 카메라도 회전한다.
        transform.eulerAngles = new Vector3(90, _target.eulerAngles.y, 0f);
    }
}