using UnityEngine;

public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefabs;
    [SerializeField] private float _range = 100f;            // 사거리
    [SerializeField] private float _damage = 10f;            // 공격력
    [SerializeField] private float _fireInterval = 0.1f;     // 발사 속도
    [SerializeField] private int _maxAmmo = 50;              // 탄창 최대 탄약 수
    [SerializeField] private float _reloadTime = 1.5f;       // 재장전 시간
    private float _fireTimer = 0f;
    private int _currentAmmo;                                // 현재 탄약 수
    private float _reloadTimer = 0f;                         // 재장전 남은 시간
    private bool _isReloading = false;                       // 재장전 중 여부

    private void Awake()
    {
        _currentAmmo = _maxAmmo;
    }

    private void Update()
    {
        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        if (_isReloading)
        {
            UpdateReload();
        }

        if (Input.GetKeyDown(KeyCode.R) && !_isReloading && _currentAmmo < _maxAmmo)
        {
            StartReload();
        }

        // 1. 마우스 왼쪽 버튼 입력 받기 (재장전 중이 아니고 탄약이 남아 있을 때만)
        if (Input.GetButton("Fire1") && _fireTimer <= 0f && !_isReloading && _currentAmmo > 0)
        {
            _fireTimer = _fireInterval;
            _currentAmmo--;
            Shoot();
        }
    }

    private void StartReload()
    {
        _isReloading = true;
        _reloadTimer = _reloadTime;
    }

    private void UpdateReload()
    {
        _reloadTimer -= Time.deltaTime;
        if (_reloadTimer <= 0f)
        {
            _currentAmmo = _maxAmmo;
            _isReloading = false;
        }
    }

    // 카메라 정면으로 레이를 쏴서 피격 이펙트 생성
    private void Shoot()
    {
        // Ray: 레이저(시작위치, 날아갈 방향)
        // Physics.RayCast: 레이저를 발사
        // RayCastHit: 레이저가 물체와 부닫히면 그 정보를 저장하는 구조체

        // 2. 레이(광선)을 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힐 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알에 맞았다. 대미지: {_damage}");
            ParticleSystem effect = Instantiate(_hitEffectPrefabs);
            effect.transform.position = hitInfo.point;  // 맞은 자리
            effect.transform.forward = hitInfo.normal;  // 수직 위치로 회전
            effect.Play();
        }
    }
}