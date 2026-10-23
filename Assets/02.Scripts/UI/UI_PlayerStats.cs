using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _healthBarSlider;
    [SerializeField] private Slider _staminaBarSlider;

    private PlayerMove _playerMove;

    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
    }

    private void LateUpdate()
    {
        // 0 ~ 100% -> 0 ~ 1
        _staminaBarSlider.value = _playerMove.Stamina / _playerMove.MaxStamina;

        // 빌보드: 체력바가 카메라를 쳐다보게한다.
        transform.forward = Camera.main.transform.forward;
    }
}
