using UnityEngine;

// 역할: 마우스를 움직이면 카메라를 그 방향으로 회전하고 싶다.
public class CameraRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;

    private float _accumulationX = 0f;  // 좌우 회전 각도
    private float _accumulationY = 0f;  // 위아래 회전 각도


    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 1. 마우스 입력을 받는다.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 2. 마우스 입력 만큼 회전값을 누적한다.
        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;
        _accumulationY += -mouseY * _rotationSpeed * Time.deltaTime;

        // 3. 그 방향으로 회전 시킨다.
        _accumulationY = Mathf.Clamp(_accumulationY, -90, 90);

        // 4. 위 아래 회전값을 -90 ~ 90도로 제한한다.
        transform.eulerAngles = new Vector3(_accumulationY, _accumulationX, 0f);
    }
}
