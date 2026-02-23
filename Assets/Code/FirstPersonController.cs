using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class FirstPersonController : MonoBehaviour
{
    #region Configuration
    [Header("Camera & Visuals")]
    public Transform cameraRoot;
    public Camera playerCamera;
    public bool cameraMovement = true;
    public float lookSensitivity = 0.5f;
    public float maxLookAngle = 85f;

    [Header("Movement Stats")]
    public float walkSpeed = 5f;
    public float slowDownSpeed = 0.1f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 5f;

    [Header("Procedural Animation (Juice)")]
    public bool enableHeadBob = true;
    public bool enableCameraNoise = false;
    public bool enableCameraTilt = false;
    public bool invertCamMoveTilt = false;
    public float cameraNoiseIntensity = 0.1f;
    public float cameraNoiseFrequency = 1f;
    public float bobFrequency = 10f;
    public float bobSprinting = 14f;
    public float bobAmplitude = 0.1f;
    public float moveTiltAngle = 2f;
    public float moveTiltSpeed = 5f;
    public float viewTiltAmount = 1f;

    [Header("Zoom Settings")]
    public float defaultFov = 60f;
    public float sprintFov = 75f;
    public float zoomFov = 30f;
    public float zoomDuration = 0.3f;
    public float sprintFovDuration = 0.1f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchTransitionDuration = 0.2f;

    [Header("Ground Check")]
    public LayerMask groundMask;
    public float groundCheckOffset = 0.1f;
    
    [Header("Debug")]
    public bool _debugMode = false;
    #endregion

    // Referências Internas
    private Rigidbody _rb;
    private CapsuleCollider _collider;
    
    // Input State Cache
    private Vector2 _currentInputVector;
    
    // State Variables
    private float _cameraPitch = 0f;
    private float _currentTilt; 
    private float _defaultYPosCamera;
    private bool _isGrounded;
    private bool _hasStepped;
    private float _bobTimer;

    // Acessores de Estado via InputManager
    private bool IsSprinting => _input.Sprint.IsPressed();
    private bool IsCrouching => _input.Crouch.IsPressed();
    private bool IsMoving => _currentInputVector.sqrMagnitude > 0.01f;

    // Eventos
    public event Action OnJumpPerformed;
    public event Action OnLandPerformed;
    public event Action<bool> OnCrouchChanged;
    public event Action<float> OnStepTaken;

    // Input Manager
    InputManager _input;

    private void Awake()
    {
        _input = InputManager.Instance;
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.freezeRotation = true;

        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        _defaultYPosCamera = playerCamera.transform.localPosition.y;
    }

    private void Start()
    {
        // Subscreve usando o Singleton (Garante que o Manager já acordou)
        SubscribeInput();
        LogDebug("Controller Initialized linked to InputManager.");
    }

    private void OnDestroy()
    {
        // Boa prática: Unsubscribe ao destruir para evitar erros de referência nula
        if (_input != null) UnsubscribeInput();
    }

    private void SubscribeInput()
    {
        _input.Jump.performed += OnJump;
        _input.Crouch.performed += OnCrouchStart;
        _input.Crouch.canceled += OnCrouchEnd;
        _input.Zoom.performed += OnZoomStart;
        _input.Zoom.canceled += OnZoomEnd;
        _input.Sprint.performed += OnSprintStart;
        _input.Sprint.canceled += OnSprintEnd;
    }

    private void UnsubscribeInput()
    {
        _input.Jump.performed -= OnJump;
        _input.Crouch.performed -= OnCrouchStart;
        _input.Crouch.canceled -= OnCrouchEnd;
        _input.Zoom.performed -= OnZoomStart;
        _input.Zoom.canceled -= OnZoomEnd;
        _input.Sprint.performed -= OnSprintStart;
        _input.Sprint.canceled -= OnSprintEnd;
    }

    private void Update()
    {
        // Polling de movimento e olhar deve ser feito no Update
        _currentInputVector = _input.Move.ReadValue<Vector2>();

        CheckGround();
        HandleMovement();
        HandleLookRotation();   
        UpdateCameraVisuals();  
    }

    #region Movement & Physics Logic
    private void CheckGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float rayLength = groundCheckOffset + 0.2f;
        bool newGroundState = Physics.Raycast(origin, Vector3.down, rayLength, groundMask);

        if (newGroundState != _isGrounded)
        {
            if (newGroundState) OnLandPerformed?.Invoke();
            LogDebug(newGroundState ? "Status: Landed" : "Status: Airborne");
        }

        _isGrounded = newGroundState;
    }

    private void HandleMovement()
    {
        if (_currentInputVector == Vector2.zero)
        {
            _rb.linearVelocity = _rb.linearVelocity.magnitude < 0.01f ? Vector3.zero : _rb.linearVelocity * (1f - slowDownSpeed * Time.deltaTime);
        }

        float targetSpeed = walkSpeed;
        if (IsCrouching && IsSprinting) targetSpeed = sprintSpeed * 0.5f;
        else if (IsCrouching) targetSpeed = crouchSpeed;
        else if (IsSprinting) targetSpeed = sprintSpeed;

        Vector3 targetDirection = transform.forward * _currentInputVector.y + transform.right * _currentInputVector.x;
        Vector3 targetVelocity = targetDirection.normalized * targetSpeed;

        targetVelocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = targetVelocity;
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (_isGrounded)
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            OnJumpPerformed?.Invoke();
            LogDebug("Action: Jump Performed");
        }
    }
    #endregion

    #region Visuals & Camera
    private void HandleLookRotation()
    {
        if (!cameraMovement) return;
        // Polling do Look Vector direto do Manager
        Vector2 lookInput = _input.Look.ReadValue<Vector2>();

        float yaw = lookInput.x * lookSensitivity;
        Quaternion bodyRotation = Quaternion.Euler(0f, yaw, 0f);
        _rb.MoveRotation(_rb.rotation * bodyRotation);

        _cameraPitch -= lookInput.y * lookSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -maxLookAngle, maxLookAngle);
    }

    private void UpdateCameraVisuals()
    {
        Vector2 lookInput = _input.Look.ReadValue<Vector2>();

        // --- TILT ---
        float targetTilt = 0f;
        
        if (enableCameraTilt && IsMoving)
        {
            float moveTilt = -_currentInputVector.x * moveTiltAngle;
            if (invertCamMoveTilt) moveTilt = -moveTilt;
            targetTilt += moveTilt;
        }

        if (enableCameraTilt)
        {
            targetTilt += -lookInput.x * viewTiltAmount;
        }

        _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * moveTiltSpeed);

        // --- NOISE ---
        float noiseX = 0f;
        float noiseY = 0f;
        if (enableCameraNoise)
        {
            noiseX = (Mathf.PerlinNoise(Time.time * cameraNoiseFrequency, 0f) - 0.5f) * cameraNoiseIntensity;
            noiseY = (Mathf.PerlinNoise(0f, Time.time * cameraNoiseFrequency) - 0.5f) * cameraNoiseIntensity;
        }

        // --- APPLY ---
        Transform targetTransform = cameraRoot != null ? cameraRoot : playerCamera.transform;
        Quaternion finalRotation = Quaternion.Euler(_cameraPitch + noiseX, noiseY, _currentTilt);
        targetTransform.localRotation = finalRotation;

        HandleHeadBobPosition();
    }

    private void HandleHeadBobPosition()
    {
        float targetYPos = _defaultYPosCamera;

        if (IsMoving && _isGrounded)
        {
            float speed = IsSprinting ? bobSprinting : bobFrequency;
            _bobTimer += Time.deltaTime * speed;

            float sineCycle = Mathf.Sin(_bobTimer);
            float wave = sineCycle * bobAmplitude;
            targetYPos = _defaultYPosCamera + wave;

            HandleFootstepAudio(sineCycle); 
        }
        else
        {
            _bobTimer = Mathf.PI;
            _hasStepped = false;
        }

        float smoothSpeed = IsMoving ? 15f : moveTiltSpeed;
        Vector3 currentPos = playerCamera.transform.localPosition;
        Vector3 newPos = new Vector3(currentPos.x, targetYPos, currentPos.z);

        if (!enableHeadBob) { newPos.y = _defaultYPosCamera; }
        
        playerCamera.transform.localPosition = Vector3.Lerp(currentPos, newPos, Time.deltaTime * smoothSpeed);
    }

    private void HandleFootstepAudio(float sineCycle)
    {
        if (sineCycle < -0.95f && !_hasStepped)
        {
            _hasStepped = true;
            float volume = IsSprinting ? 1.0f : (IsCrouching ? 0.3f : 0.6f);
            OnStepTaken?.Invoke(volume);
        }
        else if (sineCycle > 0.0f)
        {
            _hasStepped = false;
        }
    }

    private void OnSprintStart(InputAction.CallbackContext ctx)
    {
        if (_currentInputVector == Vector2.zero) return;
        playerCamera.DOFieldOfView(sprintFov, sprintFovDuration).SetEase(Ease.OutCubic);
    }
    
    private void OnSprintEnd(InputAction.CallbackContext ctx)
    {
        playerCamera.DOFieldOfView(defaultFov, sprintFovDuration).SetEase(Ease.InSine);
    }
    
    private void OnZoomStart(InputAction.CallbackContext ctx)
    {
        playerCamera.DOFieldOfView(zoomFov, zoomDuration).SetEase(Ease.OutCubic);
    }

    private void OnZoomEnd(InputAction.CallbackContext ctx)
    {
        playerCamera.DOFieldOfView(defaultFov, zoomDuration).SetEase(Ease.InSine);
    }
    #endregion

    #region Crouch Logic
    private void OnCrouchStart(InputAction.CallbackContext ctx)
    {
        _collider.height = crouchHeight;
        OnCrouchChanged?.Invoke(true);
    }

    private void OnCrouchEnd(InputAction.CallbackContext ctx)
    {
        _collider.height = standHeight;
        OnCrouchChanged?.Invoke(false);
    }
    #endregion

    private void LogDebug(string message)
    {
        if (_debugMode) Debug.Log($"<color=cyan>[FPC]</color> {message}");
    }

    void OnDrawGizmosSelected()
    {
        if (!_debugMode) return;
        Gizmos.color = Color.green;
        Vector3 spherePosition = transform.position + Vector3.up * (groundCheckOffset * 0.5f);
        Gizmos.DrawWireSphere(spherePosition, groundCheckOffset);
    }
    
    #region Debug Accessors
    public Vector3 Velocity => _rb.linearVelocity;
    public Vector3 LocalVelocity => transform.InverseTransformDirection(Velocity);
    public bool IsGrounded => _isGrounded; 
    public bool IsSprintingFlag => IsSprinting;
    public bool HasSteped => _hasStepped;
    public bool IsCrouchingFlag => IsCrouching;
    public bool CanMoveCamera => cameraMovement;
    public Vector2 InputVector => _currentInputVector;
    public float CurrentFov => playerCamera != null ? playerCamera.fieldOfView : 0f;
    #endregion
}