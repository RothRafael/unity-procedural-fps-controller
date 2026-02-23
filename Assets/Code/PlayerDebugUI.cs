using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Text;
using UnityEngine.InputSystem;
using System;

public class PlayerDebugUI : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController player;
    public Canvas debugCanvas;
    
    [Header("Left Side - Stats")]
    public TextMeshProUGUI statsText;

    [Header("Right Side - Toggles")]
    public Toggle headBobToggle;
    public Toggle camNoiseToggle;
    public Toggle camTiltToggle;
    public Toggle soundToggle;

    [Header("Right Side - Sliders Configuration")]
    [Tooltip("Drag the container (e.g. Vertical Layout Group) here")]
    public Transform sliderContainer;
    
    [Tooltip("Drag your slider prefab here. MUST contain children named 'nameText' and 'ValueText'")]
    public GameObject sliderPrefab;

    InputManager _input;
    private StringBuilder _sb = new StringBuilder();

    private void Awake()
    {
        _input = InputManager.Instance;
        SetToggles();
        
        // Generate the 11 sliders
        if (sliderPrefab != null && sliderContainer != null)
        {
            GenerateSliders();
        }
        else
        {
            Debug.LogWarning("PlayerDebugUI: Slider Prefab or Container is missing in Inspector.");
        }
    }

    private void OnEnable()
    {
        if (_input == null) return;
        _input.ToggleDebug.Enable();
        _input.ToggleDebug.performed += ToggleUI;
        _input.EnableMouse.performed += EnableMouse;
    }

    private void OnDisable()
    {
        if (_input == null) return;
        _input.ToggleDebug.Enable();
        _input.ToggleDebug.performed -= ToggleUI;
        _input.EnableMouse.performed -= EnableMouse;
    }

    private void FixedUpdate()
    {
        UpdateStatsDisplay();
    }

    private void ToggleUI(InputAction.CallbackContext ctx)
{
    bool isActive = !debugCanvas.gameObject.activeSelf;
    debugCanvas.gameObject.SetActive(isActive);

    // CRÍTICO: Libere o mouse quando o menu abrir
    if (isActive) {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    } else {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
    
    private void EnableMouse(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        GameEvents.OnGameStateChanged?.Invoke();
    }

    private void UpdateStatsDisplay()
    {
        _sb.Clear();
        _sb.AppendLine($"<color=green>PLAYER STATS</color>");
        _sb.AppendLine($"----------------");
        _sb.AppendLine($"Speed: {player.Velocity.magnitude:F2} m/s");
        
        Vector3 localVel = player.LocalVelocity;
        _sb.AppendLine($"Vel Local: [X:{localVel.x:F1} Y:{localVel.y:F1} Z:{localVel.z:F1}]");
        
        _sb.AppendLine($"Input: {player.InputVector}");
        _sb.AppendLine($"Grounded: {ColorBool(player.IsGrounded)}");
        _sb.AppendLine($"Sprinting: {ColorBool(player.IsSprintingFlag)}");
        _sb.AppendLine($"Crouching: {ColorBool(player.IsCrouchingFlag)}");
        _sb.AppendLine($"Stepped: {ColorBool(player.HasSteped)}");
        _sb.AppendLine($"FOV: {player.CurrentFov:F1}");

        statsText.text = _sb.ToString();
    }

    private string ColorBool(bool value)
    {
        return value ? "<color=green>true</color>" : "<color=red>false</color>";
    }

    private void SetToggles()
    {
        if (player != null)
        {
            if(headBobToggle) {
                headBobToggle.isOn = player.enableHeadBob;
                headBobToggle.onValueChanged.AddListener((val) => player.enableHeadBob = val);
            }
            if(camNoiseToggle) {
                camNoiseToggle.isOn = player.enableCameraNoise;
                camNoiseToggle.onValueChanged.AddListener((val) => player.enableCameraNoise = val);
            }
            if (camTiltToggle) {
                camTiltToggle.isOn = player.enableCameraTilt;
                camTiltToggle.onValueChanged.AddListener((val) => player.enableCameraTilt = val);
            }
        }
    }

    // ---------------------------------------------------------
    // SLIDER GENERATION LOGIC
    // ---------------------------------------------------------

    private void GenerateSliders()
    {
    // Clear existing children if any (optional, useful for reloading)
        foreach (Transform child in sliderContainer) Destroy(child.gameObject);

        // --- Movement Stats (5) ---
        CreateSlider("Walk Speed",      player.walkSpeed,       1f, 10f,  (val) => player.walkSpeed = val);
        CreateSlider("Sprint Speed",    player.sprintSpeed,     5f, 20f,  (val) => player.sprintSpeed = val);
        CreateSlider("Jump Force",      player.jumpForce,       1f, 15f,  (val) => player.jumpForce = val);

        // --- Tilt / Bob / Procedural (3) ---
        CreateSlider("Move Tilt Angle", player.moveTiltAngle,   0f, 15f,  (val) => player.moveTiltAngle = val);
        CreateSlider("Move Tilt Speed", player.moveTiltSpeed,   1f, 20f,  (val) => player.moveTiltSpeed = val);
        CreateSlider("View Tilt Amount", player.viewTiltAmount, 0f, 0.2f, (val) => player.viewTiltAmount = val); // Check variable name
        CreateSlider("Idle noise Amount", player.cameraNoiseIntensity, 0f, 5f, (val) => player.cameraNoiseIntensity = val);     // Check variable name
        CreateSlider("Idle noise Speed", player.cameraNoiseFrequency, 0f, 10f, (val) => player.cameraNoiseFrequency = val); // Check variable name
        CreateSlider("Head Bob ammount", player.bobAmplitude, 0f, 2f, (val) => player.bobAmplitude = val); // Check variable name)
        CreateSlider("Head Bob speed", player.bobFrequency, 0f, 50f, (val) => player.bobFrequency = val); // Check variable name)
        
        // --- Zoom Settings (3) ---
        CreateSlider("Default FOV",     player.defaultFov,      60f, 110f,(val) => player.defaultFov = val);
        CreateSlider("Sprint FOV",      player.sprintFov,       70f, 120f,(val) => player.sprintFov = val);
        CreateSlider("Zoom FOV",        player.zoomFov,         10f, 60f, (val) => player.zoomFov = val);
    }

    private void CreateSlider(string uiName, float currentVal, float min, float max, Action<float> setter)
    {
        // 1. Instantiate
        GameObject item = Instantiate(sliderPrefab, sliderContainer);
        item.name = $"Slider_{uiName}";

        // 2. Find Text Components (Using the specific names from your image)
        // 'nameText' (lowercase start) and 'ValueText' (PascalCase)
        Transform nameTransform = item.transform.Find("nameText"); 
        Transform valTransform  = item.transform.Find("ValueText");

        // 3. Set Name
        if (nameTransform != null)
            nameTransform.GetComponent<TextMeshProUGUI>().text = uiName;

        // 4. Set Initial Value Text
        TextMeshProUGUI valTextComp = valTransform != null ? valTransform.GetComponent<TextMeshProUGUI>() : null;
        if (valTextComp != null)
            valTextComp.text = currentVal.ToString("0.00");

        // 5. Setup Slider
        // GetComponentInChildren finds the slider even if it is inside "sliderWalkSpeed" object
        Slider sliderComp = item.GetComponentInChildren<Slider>();

        if (sliderComp != null)
        {
            sliderComp.minValue = min;
            sliderComp.maxValue = max;
            sliderComp.value = currentVal;

            sliderComp.onValueChanged.AddListener((v) => 
            {
                setter(v); // Update Player variable
                if (valTextComp != null) valTextComp.text = v.ToString("0.00"); // Update Text
            });
        }
        else
        {
            Debug.LogError($"UI Error: No Slider component found in children of {item.name}");
        }
    }
}