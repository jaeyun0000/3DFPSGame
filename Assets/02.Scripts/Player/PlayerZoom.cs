using UnityEngine;

public class PlayerZoom : MonoBehaviour
{
    private Player _player;

    private void Start()
    {
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        bool isZooming = _player.WeaponMode == WeaponMode.Sniper && Input.GetMouseButton(1);

        Camera.main.fieldOfView = isZooming ? 15f : 60f;
    }
}