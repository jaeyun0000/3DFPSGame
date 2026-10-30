using UnityEngine;

public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private float _normalPov = 60f;
    [SerializeField] private float _sniperPov = 15f;
    private Player _player;
    private CameraFollow _cameraFollow;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _cameraFollow = Camera.main.GetComponent<CameraFollow>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 저격 줌은 1인칭에서만 쓴다.
        bool isZooming = _player.WeaponMode == WeaponMode.Sniper
                         && _cameraFollow.ViewMode == CameraViewMode.FirstPerson
                         && Input.GetMouseButton(1);
        _sniperZoom.SetActive(isZooming);
        Camera.main.fieldOfView = isZooming ? _sniperPov : _normalPov;
    }
}