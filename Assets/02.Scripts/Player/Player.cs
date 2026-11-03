using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _maxHealth = 100f;
    private float _health;

    private WeaponMode _weaponMode = WeaponMode.Normal;
    public WeaponMode WeaponMode => _weaponMode;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    // Action 자료향: 함수(들)를 저장할 수 있다.
    public event Action OnDamaged;

    // 무기 모드가 바뀌면 바뀐 모드를 알려준다.
    public event Action<WeaponMode> OnWeaponModeChanged;

    private Animator _animator;
    private int _injuredLayerIndex;

    // UI가 Start에서 처음 체력을 읽어가므로, 그보다 먼저 실행되는 Awake에서 초기화한다.
    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _injuredLayerIndex = _animator.GetLayerIndex("Injured Layer");

        _health = _maxHealth;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 숫자 키 1은 일반 모드, 2는 저격 모드;
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeWeaponMode(WeaponMode.Normal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeWeaponMode(WeaponMode.Sniper);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeWeaponMode(WeaponMode.Melee);
        }
    }

    private void ChangeWeaponMode(WeaponMode weaponMode)
    {
        // 같은 모드를 다시 고르면 알리지 않는다.
        if (_weaponMode == weaponMode) return;

        _weaponMode = weaponMode;
        OnWeaponModeChanged?.Invoke(_weaponMode);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return;

        _health -= damageInfo.Value;

        // 체력에 따라 Injured Layer의 가중ㅇ치를 바꿔준다.
        float healthPercent = _health / _maxHealth; // 0 ~ 1;
        float injured = Mathf.InverseLerp(0.5f, 0f, healthPercent);
        _animator.SetLayerWeight(_injuredLayerIndex, injured);

        // 화면 피격 효과는 UI_HitEffect가 이 이벤트를 구독해서 처리한다.
        OnDamaged?.Invoke();

        // GameLogger.Log는 ENABLE_LOG가 없는 빌드에서 호출(문자열 생성 포함)이 통째로 제거된다.
        GameLogger.Log($"플레이어가 맞았다. 대미지 {damageInfo.Value}, 남은 체력 {_health}");
        if (_health <= 0f)
        {
            GameLogger.Log("플레이어가 쓰러졌다!");
            GameManager.Instance.GameOver();
        }
    }
}