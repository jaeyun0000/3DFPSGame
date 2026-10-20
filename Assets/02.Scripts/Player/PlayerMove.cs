using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;
    private const float GroundedYVelocity = -1f;
    private const float StaminaTickInterval = 0.1f;

    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _sprintSpeed = 12f;
    [SerializeField] private float _jumpPower = 10f;
    [SerializeField] private int _maxJumpCount = 2;

    [SerializeField] private int _maxStamina = 100;
    [SerializeField] private int _sprintCostPerTick = 2;
    [SerializeField] private int _regenPerTick = 1;
    [SerializeField] private int _jumpCost = 20;

    [SerializeField] private CharacterController _characterController;

    private float _yVelocity = 0f;
    private int _jumpCount = 0;
    private bool _isSprintJumping = false;
    private int _stamina;
    private bool _isSprinting = false;
    private float _sprintTimer = 0f;
    private float _regenTimer = 0f;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stamina = _maxStamina;
    }

    private void Update()
    {
        Vector3 direction = GetInputDirection();
        float speed;

        if (_characterController.isGrounded)
        {
            _jumpCount = 0;
            _isSprintJumping = false;
            _yVelocity = GroundedYVelocity;

            UpdateSprint(Input.GetKey(KeyCode.LeftShift) && direction != Vector3.zero);
            speed = _isSprinting ? _sprintSpeed : _moveSpeed;
        }
        else
        {
            _yVelocity += Gravity * Time.deltaTime;
            _isSprinting = false;
            speed = _isSprintJumping ? _sprintSpeed : _moveSpeed;
        }

        UpdateRegen();
        TryJump();

        Vector3 velocity = direction * speed;
        velocity.y = _yVelocity;
        _characterController.Move(velocity * Time.deltaTime);
    }

    // 입력 방향을 월드 기준 단위 벡터로 반환
    private Vector3 GetInputDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(h, 0f, v).normalized;
        return transform.TransformDirection(direction);
    }

    // 질주 시작 시 즉시 소모하고 이후 0.1초마다 소모
    private void UpdateSprint(bool wantsSprint)
    {
        if (!wantsSprint)
        {
            _isSprinting = false;
            return;
        }

        if (!_isSprinting)
        {
            if (_stamina < _sprintCostPerTick)
            {
                return;
            }

            _stamina -= _sprintCostPerTick;
            _isSprinting = true;
            _sprintTimer = 0f;
            return;
        }

        _sprintTimer += Time.deltaTime;
        while (_sprintTimer >= StaminaTickInterval)
        {
            _sprintTimer -= StaminaTickInterval;

            if (_stamina < _sprintCostPerTick)
            {
                _isSprinting = false;
                return;
            }

            _stamina -= _sprintCostPerTick;
        }
    }

    // 질주하지 않는 동안 0.1초마다 회복
    private void UpdateRegen()
    {
        if (_isSprinting || _stamina >= _maxStamina)
        {
            _regenTimer = 0f;
            return;
        }

        _regenTimer += Time.deltaTime;
        while (_regenTimer >= StaminaTickInterval)
        {
            _regenTimer -= StaminaTickInterval;
            _stamina = Mathf.Min(_stamina + _regenPerTick, _maxStamina);
        }
    }

    // 점프 입력 시 횟수, 스태미나 조건을 확인하고 점프
    private void TryJump()
    {
        if (!Input.GetButtonDown("Jump"))
        {
            return;
        }

        if (_jumpCount >= _maxJumpCount || _stamina < _jumpCost)
        {
            return;
        }

        if (_isSprinting)
        {
            _isSprintJumping = true;
        }

        _yVelocity = _jumpPower;
        _jumpCount++;
        _stamina -= _jumpCost;
        _regenTimer = 0f;
    }
}