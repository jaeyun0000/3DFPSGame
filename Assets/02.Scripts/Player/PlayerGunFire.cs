using System;
using System.Collections;
using UnityEngine;

// a=마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefabs;

    [Header("Stat")]
    [SerializeField] private float _range = 100f;            // 사거리
    [SerializeField] private float _damage = 10f;            // 공격력
    [SerializeField] private float _fireInterval = 0.1f;     // 발사 속도
    [SerializeField] private int _maxBulletCount = 30;
    [SerializeField] private float _reloadingTime = 3f;

    [Header("레이저")]
    [SerializeField] private LineRenderer _laserLine;
    [SerializeField] private Transform _laserStartPoint;     // 1인칭에서 총알이 나가는 위치(화면 오른쪽 아래 총구)
    [SerializeField] private Transform _playerMuzzle;        // 1인칭이 아닐 때 총알이 나가는 위치(플레이어 손)
    [SerializeField] private float _laserDuration = 0.05f;   // 레이저가 보였다가 사라지는 시간

    [SerializeField] private GameObject[] _muzzleFlashes;
    [SerializeField] private float _flashTime = 0.05f;
    private float _fireTimer = 0f;
    private int _bulletCount;

    private Coroutine _reloadCoroutine;
    private Coroutine _laserCoroutine;

    private Animator _animator;  // 군인 모델의 애니메이터
    private CameraFollow _cameraFollow;  // 시점에 따라 조준 방향을 알려준다.
    private int _hitLayerMask;           // 총알이 맞는 레이어 (3인칭에서 뒤에서 쏘면 자기 몸에 맞으므로 플레이어는 제외)

    public int BulletCount => _bulletCount;
    public int MaxBulletCount => _maxBulletCount;

    // 총알 수가 바뀌면 (현재 총알 수, 탄창 크기)를 알려준다.
    public event Action<int, int> OnBulletCountChanged;

    // UI가 Start에서 처음 총알 수를 읽어가므로, 그보다 먼저 실행되는 Awake에서 초기화한다.
    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _cameraFollow = Camera.main.GetComponent<CameraFollow>();
        _hitLayerMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player");

        _bulletCount = _maxBulletCount;
    }

    private void Start()
    {
        _laserLine.positionCount = 2;
        _laserLine.useWorldSpace = true;
        _laserLine.enabled = false;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기
        bool isMouseVisible = Cursor.visible;
        bool isFiring = !isMouseVisible && Input.GetButton("Fire1") && _bulletCount > 0;

        // 쏘는 동안 상체(Fire Layer)만 쏘는 동작을 한다. 연사 간격 사이에도 켜 둬야 동작이 끊기지 않는다.
        _animator.SetBool("Fire", isFiring);

        if (isFiring && _fireTimer <= 0f)
        {
            _bulletCount--;
            OnBulletCountChanged?.Invoke(_bulletCount, _maxBulletCount);
            _fireTimer = _fireInterval;
            Shoot();

            // 이 방식은: 이 모노비헤어비어가 등록한 모든 코루틴을 실행 취소;
            //StopAllCoroutines();
            if (_reloadCoroutine == null) return;

            StopCoroutine(_reloadCoroutine);
            Debug.Log("재장전 취소");
            _reloadCoroutine = null;
        }

        // 2. R키 누르면 총알 재장전 시작
        if (Input.GetKeyDown(KeyCode.R) && _reloadCoroutine == null)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    private IEnumerator Reloading_Coroutine()
    {
        Debug.Log("재장전 시작");
        yield return new WaitForSeconds(_reloadingTime);

        _bulletCount = _maxBulletCount;
        OnBulletCountChanged?.Invoke(_bulletCount, _maxBulletCount);
        Debug.Log("재장전 완료");
    }

    private void ShowLaser(Vector3 startPoint, Vector3 endPoint)
    {
        _laserLine.SetPosition(0, startPoint);
        _laserLine.SetPosition(1, endPoint);
        _laserLine.enabled = true;

        // 연사 중이면 이전 레이저의 사라지는 시간을 새로 센다.
        if (_laserCoroutine != null)
        {
            StopCoroutine(_laserCoroutine);
        }
        _laserCoroutine = StartCoroutine(HideLaser_Coroutine());
    }

    private IEnumerator HideLaser_Coroutine()
    {
        yield return new WaitForSeconds(_laserDuration);

        _laserLine.enabled = false;
        _laserCoroutine = null;
    }

    private void Shoot()
    {
        StartCoroutine(MuzzleFlash_Coroutine());

        // Ray: 레이저(시작위치, 날아갈 방향)
        // Physics.RayCast: 레이저를 발사
        // RayCastHit: 레이저가 물체와 부닫히면 그 정보를 저장하는 구조체

        // 2. 레이(광선)을 생성하고 발사될 위치와 진행 방향 설정하기
        //    조준점은 카메라가 정하고(화면 가운데), 총알은 총구에서 그 조준점을 향해 나간다.
        Vector3 muzzlePosition = GetMuzzlePosition();
        Vector3 aimPoint = GetAimPoint(muzzlePosition);
        Ray ray = new Ray(muzzlePosition, (aimPoint - muzzlePosition).normalized);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힐 물체가 있다면 이펙트 표시하기
        //    총구와 조준점 사이에 벽이 있으면 벽에 맞는다.
        bool isHit = Physics.Raycast(ray, out hitInfo, _range, _hitLayerMask);

        // 맞으면 맞은 자리까지, 안 맞으면 사거리 끝까지 레이저를 그린다.
        Vector3 laserEndPoint = isHit ? hitInfo.point : ray.GetPoint(_range);
        ShowLaser(muzzlePosition, laserEndPoint);

        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알에 맞았다. 대미지: {_damage}");

            // 맞은 자리에 총알 자국 이펙트 만들기
            ParticleSystem effect = Instantiate(_hitEffectPrefabs);
            effect.transform.position = hitInfo.point;  // 맞은 자리
            effect.transform.forward = hitInfo.normal;  // 수직 위치로 회전
            effect.Play();

            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            hitInfo.collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);

            // 맞은 대상이 좀비라면 TakeDamage
            // hitInfo.collider.GetComponent<Zombie>()?.TakeDamage(_damage);
            // if (hitInfo.collider.TryGetComponent(out Zombie zombie))
            // {
            //     zombie.TakeDamage(_damage);
            // }

            // if (hitInfo.collider.TryGetComponent(out Drum drum))
            // {
            //     drum.TakeDamage(_damage);
            // }

            // hitInfo.collider.GetComponent<Box>()?.TakeDamage(_damage);
        }
    }

    // 1인칭은 화면에 보이는 총구, 다른 시점은 플레이어 손에서 쏜다.
    private Vector3 GetMuzzlePosition()
    {
        if (_cameraFollow.ViewMode == CameraViewMode.FirstPerson) return _laserStartPoint.position;
        return _playerMuzzle.position;
    }

    // 조준 방향(화면 가운데)이 가리키는 곳. 아무것도 없으면 사거리 끝을 조준한다.
    private Vector3 GetAimPoint(Vector3 muzzlePosition)
    {
        Ray aimRay = _cameraFollow.AimRay;

        // 3인칭처럼 카메라가 플레이어 뒤에 있으면, 카메라와 총구 사이에 있는 물체를 조준하지 않도록 총구 높이부터 검사한다.
        float skipDistance = Mathf.Max(0f, Vector3.Dot(muzzlePosition - aimRay.origin, aimRay.direction));
        aimRay.origin += aimRay.direction * skipDistance;

        if (Physics.Raycast(aimRay, out RaycastHit aimHit, _range, _hitLayerMask))
        {
            return aimHit.point;
        }
        return aimRay.GetPoint(_range);
    }

    private IEnumerator MuzzleFlash_Coroutine()
    {
        // Inspector에서 총구 불꽃을 연결하지 않았으면 그냥 넘어간다.
        if (_muzzleFlashes.Length == 0) yield break;

        int randomIndex = UnityEngine.Random.Range(0, _muzzleFlashes.Length);
        GameObject flash = _muzzleFlashes[randomIndex];
        flash.SetActive(true);

        yield return new WaitForSeconds(_flashTime);

        flash.SetActive(false);
    }
}