using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 오른쪽 아래에 지금 무기 이미지, 남은 총알 / 탄창 크기, 남은 폭탄 수를 보여준다.
public class UI_PlayerWeapons : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private PlayerGunFire _playerGunFire;
    [SerializeField] private PlayerBombFire _playerBombFire;

    [Header("UI")]
    [SerializeField] private Image _weaponImage;
    [SerializeField] private TextMeshProUGUI _bulletText;
    [SerializeField] private TextMeshProUGUI _bombText;

    [Header("무기 이미지")]
    [SerializeField] private Sprite _normalWeaponSprite;
    [SerializeField] private Sprite _sniperWeaponSprite;

    private void Start()
    {
        // 값이 바뀔 때만 UI를 갱신한다. (옵저버 패턴)
        _player.OnWeaponModeChanged += RefreshWeaponImage;
        _playerGunFire.OnBulletCountChanged += RefreshBulletCount;
        _playerBombFire.OnBombCountChanged += RefreshBombCount;

        // 이벤트는 값이 바뀔 때만 오므로, 시작할 때 한 번 직접 그려준다.
        RefreshWeaponImage(_player.WeaponMode);
        RefreshBulletCount(_playerGunFire.BulletCount, _playerGunFire.MaxBulletCount);
        RefreshBombCount(_playerBombFire.BombCount);
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnWeaponModeChanged -= RefreshWeaponImage;
        }
        if (_playerGunFire != null)
        {
            _playerGunFire.OnBulletCountChanged -= RefreshBulletCount;
        }
        if (_playerBombFire != null)
        {
            _playerBombFire.OnBombCountChanged -= RefreshBombCount;
        }
    }

    // Inspector에서 무기 이미지를 넣거나 바꾸면 (플레이 중에도) 바로 반영한다.
    private void OnValidate()
    {
        WeaponMode weaponMode = _player != null ? _player.WeaponMode : WeaponMode.Normal;
        RefreshWeaponImage(weaponMode);
    }

    private void RefreshWeaponImage(WeaponMode weaponMode)
    {
        if (_weaponImage == null) return;

        _weaponImage.sprite = weaponMode == WeaponMode.Sniper ? _sniperWeaponSprite : _normalWeaponSprite;
    }

    private void RefreshBulletCount(int bulletCount, int maxBulletCount)
    {
        _bulletText.text = $"{bulletCount} / {maxBulletCount}";
    }

    private void RefreshBombCount(int bombCount)
    {
        _bombText.text = $"폭탄 {bombCount}";
    }
}