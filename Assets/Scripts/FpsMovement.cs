using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;

public class FPSMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("Jump and Fall")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -12f;
    [SerializeField] private float initialFallVelocity = -2f;

    [Header("Crouching")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float cameraOffset = 0.4f;

    [Header("Stamina")]
    [SerializeField] private float sprintTime = 4.5f;
    [SerializeField] private float refillTime = 9f;
    [SerializeField] private float refillDelay = 1f;
    [SerializeField] private float minStaminaToRun = 0.25f;
    [SerializeField] private Image staminaFill;

    [Header("Knockback")]
    [SerializeField] private float pushDrag = 4f;

    [Header("References")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InputActionReference crouchAction;

    [SerializeField] private InputActionReference sprintAction;

    private CharacterController _characterController;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private float _verticleVelocity;
    private bool _isRunning;
    private bool _isCrouching;
    private float _targetHeight;
    private float _stamina = 1f;
    private float _refillStartTime;
    private bool _tired;
    private Vector3 _pushVelocity;

    public bool IsGrounded => _isGrounded;
    public bool IsMoving => _isGrounded && _moveInput.sqrMagnitude > 0.01f;
    public bool IsSprinting { get; private set; }
    public float Stamina => _stamina;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _characterController = GetComponent<CharacterController>();
        _targetHeight = standingHeight;
    }

    private void OnEnable()
    {
        moveAction.action.performed += StoreMovementInput;
        moveAction.action.canceled += StoreMovementInput;
        jumpAction.action.performed += Jump;
        crouchAction.action.performed += Crouch;
        sprintAction.action.performed += Sprint;
        sprintAction.action.canceled += Sprint;

    }


    private void OnDisable()
    {
        moveAction.action.performed -= StoreMovementInput;
        moveAction.action.canceled -= StoreMovementInput;
        jumpAction.action.performed -= Jump;
        crouchAction.action.performed -= Crouch;
        sprintAction.action.performed -= Sprint;
        sprintAction.action.canceled -= Sprint;
    }



    private void Update()
    {
        _isGrounded = _characterController.isGrounded;

        HandleGravity();
        HandleStamina();
        HandleMovement();
        HandleCrouchTransition();

    }

    public void Knockback(Vector3 force)
    {
        _pushVelocity += new Vector3(force.x, 0f, force.z);

        if (force.y > 0f)
        {
            _verticleVelocity = force.y;
            _isGrounded = false;
        }
    }

    private void StoreMovementInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }
    private void Jump(InputAction.CallbackContext context)
    {
        if (_isGrounded)
        {
            _verticleVelocity = jumpForce;
        }

    }

    private void Sprint(InputAction.CallbackContext context)
    {
        _isRunning = context.performed;
    }

    private void Crouch(InputAction.CallbackContext context)
    {
        if (_isCrouching)
        {
            if (!CanStandUp())
            {
                return;
            }
            _targetHeight = standingHeight;

        }
        else
        {
            _targetHeight = crouchingHeight;
        }

        _isCrouching = !_isCrouching;
    }

    private bool CanStandUp()
    {
        //Used capsule cast to check if there is enough space above the player to stand up, did not use simple racast bcuz it fails at edges.
        return !Physics.CapsuleCast(
            transform.position + _characterController.center,
            transform.position + (Vector3.up * _characterController.height / 2),
            _characterController.radius,
            Vector3.up
            );
    }

    private void HandleGravity()
    {
        if (_isGrounded && _verticleVelocity < 0)
        {
            _verticleVelocity = initialFallVelocity;
        }

        _verticleVelocity += gravity * Time.deltaTime;
    }

    private void HandleStamina()
    {
        IsSprinting = _isRunning && !_isCrouching && !_tired && _moveInput.sqrMagnitude > 0.01f;

        if (IsSprinting)
        {
            _stamina -= Time.deltaTime / sprintTime;
            _refillStartTime = Time.time + refillDelay;

            if (_stamina <= 0f)
            {
                _stamina = 0f;
                _tired = true;
            }
        }
        else if (Time.time >= _refillStartTime)
        {
            _stamina = Mathf.Min(1f, _stamina + Time.deltaTime / refillTime);
        }

        if (_tired && _stamina >= minStaminaToRun)
            _tired = false;

        if (staminaFill != null)
            staminaFill.fillAmount = _stamina;
    }

    private void HandleMovement()
    {
        var move = cameraTransform.TransformDirection(new Vector3(_moveInput.x, 0, _moveInput.y));
        move.y = 0f;    
        move.Normalize();
        var currentSpeed = _isCrouching ? crouchSpeed : IsSprinting ? runSpeed : walkSpeed;
        var finalMove = move * currentSpeed + _pushVelocity;
        finalMove.y = _verticleVelocity;
        _pushVelocity = Vector3.Lerp(_pushVelocity, Vector3.zero, pushDrag * Time.deltaTime);

        CollisionFlags collisions = _characterController.Move(finalMove * Time.deltaTime);

        if ((collisions & CollisionFlags.Above) != 0)
        {
            _verticleVelocity = initialFallVelocity;
        }
    }

    private void HandleCrouchTransition()
    {
        var currentHeight = _characterController.height;
        if (Mathf.Abs(currentHeight - _targetHeight) > 0.01f)
        {
            var newHeight = Mathf.Lerp(currentHeight, _targetHeight, crouchTransitionSpeed * Time.deltaTime);
            _characterController.height = newHeight;
            _characterController.center = Vector3.up * (newHeight * 0.5f);

            var cameraTargetPosition = cameraTransform.localPosition;
            cameraTargetPosition.y = _targetHeight - cameraOffset;
            cameraTransform.localPosition = Vector3.Lerp(
                    cameraTransform.localPosition,
                    cameraTargetPosition,
                    crouchTransitionSpeed * Time.deltaTime
                    );
        }
        else
        {
            _characterController.height = _targetHeight;
        }
    }
}