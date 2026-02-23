using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    // Temp
    [SerializeField] GameObject player;

    [Header("Input Asset")]
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] private string _playerActionMapName = "Player";
    [SerializeField] private string _uiActionMapName = "UI";

    // Ações expostas publicamente (Getter only)
    public InputAction Move { get; private set; }
    public InputAction Look { get; private set; }
    public InputAction Jump { get; private set; }
    public InputAction Sprint { get; private set; }
    public InputAction Crouch { get; private set; }
    public InputAction Zoom { get; private set; }
    public InputAction ToggleDebug { get; private set; }
    public InputAction EnableMouse { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeActions();
        player.SetActive(true);
    }

    private void OnEnable() => _inputActions.Enable();
    private void OnDisable() => _inputActions.Disable();

    private void InitializeActions()
    {
        var map = _inputActions.FindActionMap(_playerActionMapName);
        if (map == null)
        {
            Debug.LogError($"[InputManager] Map '{_playerActionMapName}' not found!");
            return;
        }

        // Movement
        Move = map.FindAction("Move");
        Look = map.FindAction("Look");
        Jump = map.FindAction("Jump");
        Sprint = map.FindAction("Sprint");
        Crouch = map.FindAction("Crouch");
        Zoom = map.FindAction("Zoom");
        
        // Segundo InputMAP
        map = _inputActions.FindActionMap(_uiActionMapName);
        if (map == null)
        {
            Debug.LogError($"[InputManager] Map '{_uiActionMapName}' not found!");
            return;
        }

        // UI
        ToggleDebug = map.FindAction("ToggleDebug");
        EnableMouse = map.FindAction("EnableMouse");
    }
}