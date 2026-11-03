using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private Zombie _zombie;
    private Transform _cameraTransform;   // 좀비마다 매 프레임 Camera.main을 찾지 않도록 캐싱

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
        _cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        // 체력이 바뀔 때만 슬라이더를 갱신한다. (옵저버 패턴)
        _zombie.OnHealthChanged += RefreshHealth;
        RefreshHealth();
    }

    private void OnDestroy()
    {
        _zombie.OnHealthChanged -= RefreshHealth;
    }

    private void LateUpdate()
    {
        // 빌보드: 체력바가 카메라를 쳐다보게한다.
        _slider.transform.forward = _cameraTransform.forward;
    }

    private void RefreshHealth()
    {
        // 0 ~ 100% -> 0 ~ 1
        _slider.value = _zombie.Health / _zombie.MaxHealth;
    }
}
