using UnityEngine;

public class CursorManager : MonoBehaviour
{
    private bool _isCursorVisible = false; // Nome mais claro que _currentState

    private void OnEnable() => GameEvents.OnGameStateChanged += ToggleCursorState;
    private void OnDisable() => GameEvents.OnGameStateChanged -= ToggleCursorState;

    private void Start()
    {
        // Força estado inicial (Bloqueado)
        _isCursorVisible = false;
        ApplyCursorState(_isCursorVisible);
    }

    // Chamado pelo Evento
    private void ToggleCursorState()
    {
        _isCursorVisible = !_isCursorVisible;
        ApplyCursorState(_isCursorVisible);
        Debug.Log($"Cursor Logic Switched to: {_isCursorVisible}");
    }

    private void ApplyCursorState(bool isVisible)
    {
        Cursor.visible = isVisible;
        Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}