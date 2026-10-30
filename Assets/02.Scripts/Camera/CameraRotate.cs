using UnityEngine;

// 역할: 마우스를 움직이면 카메라를 그 방향으로 회전하고 싶다.
public class CameraRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;

    [Header("3인칭/빽뷰 위아래 회전 제한")]
    [SerializeField] private float _followMinY = -40f;  // 올려다보는 한계
    [SerializeField] private float _followMaxY = 60f;   // 내려다보는 한계

    private float _accumulationX = 0f;  // 좌우 회전 각도
    private float _accumulationY = 0f;  // 위아래 회전 각도

    private CameraFollow _cameraFollow;

    private void Awake()
    {
        _cameraFollow = GetComponent<CameraFollow>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 1. 마우스 입력을 받는다.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 2. 마우스 입력 만큼 회전값을 누적한다.
        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;
        _accumulationY += -mouseY * _rotationSpeed * Time.deltaTime;

        // 3. 위아래 회전값을 1인칭은 -90 ~ 90도로 제한한다.
        //    플레이어 뒤에서 보는 시점은 카메라가 바닥에 파묻히거나 머리 위로 넘어가지 않도록 더 좁게 제한한다.
        if (_cameraFollow.ViewMode == CameraViewMode.FirstPerson)
        {
            _accumulationY = Mathf.Clamp(_accumulationY, -90, 90);
        }
        else
        {
            _accumulationY = Mathf.Clamp(_accumulationY, _followMinY, _followMaxY);
        }

        // 4. 그 방향으로 회전 시킨다.
        transform.eulerAngles = new Vector3(_accumulationY, _accumulationX, 0f);
    }
}
