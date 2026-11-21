using UnityEngine;
using UnityEngine.InputSystem;

// Lab Part 3: переключение схемы передвижения прямо в игре (без пересборки).
// - Методы SetTeleportMode / SetSmoothMode / ResetPosition / ToggleHelp вешаются на кнопки меню на запястье.
// - Кнопка Y на левом контроллере (secondaryButton) тоже переключает Teleport <-> Smooth.
// Само переключение делает ComfortSettings (он включает/выключает smooth motion на контроллерах).
public class ModeSwitcher : MonoBehaviour
{
    public ComfortSettings comfort;
    public GameObject helpPanel;
    public Transform startPoint;              // куда возвращает Reset (Marker_Start)
    public bool enableButtonToggle = true;    // кнопка Y на левом контроллере

    InputAction toggleAction;

    void Awake()
    {
        if (comfort == null) comfort = FindAnyObjectByType<ComfortSettings>(FindObjectsInactive.Include);
    }

    void OnEnable()
    {
        if (!enableButtonToggle) return;
        toggleAction = new InputAction("ToggleLocomotion", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
        toggleAction.performed += OnTogglePerformed;
        toggleAction.Enable();
    }

    void OnDisable()
    {
        if (toggleAction == null) return;
        toggleAction.performed -= OnTogglePerformed;
        toggleAction.Disable();
        toggleAction.Dispose();
        toggleAction = null;
    }

    void OnTogglePerformed(InputAction.CallbackContext ctx) { Toggle(); }

    public void Toggle()
    {
        if (comfort == null) return;
        Set(comfort.scheme == ComfortSettings.Scheme.Teleport ? ComfortSettings.Scheme.Smooth : ComfortSettings.Scheme.Teleport);
    }

    public void SetTeleportMode() { Set(ComfortSettings.Scheme.Teleport); }
    public void SetSmoothMode()   { Set(ComfortSettings.Scheme.Smooth); }

    void Set(ComfortSettings.Scheme s)
    {
        if (comfort == null) { Debug.LogWarning("[Lab] ModeSwitcher: нет ComfortSettings"); return; }
        comfort.scheme = s;
        comfort.Apply();
        Debug.Log("[Lab] Mode -> " + s);
    }

    public void ResetPosition()
    {
        Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (cam == null) return;
        Transform rig = cam.transform.root;
        Vector3 target = startPoint != null ? startPoint.position : Vector3.zero;

        var cc = rig.GetComponentInChildren<CharacterController>();
        bool wasEnabled = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;           // иначе CharacterController вернёт позицию назад
        rig.SetPositionAndRotation(target, Quaternion.identity);
        if (cc != null) cc.enabled = wasEnabled;

        var score = FindAnyObjectByType<ScoreTracker>();
        if (score != null) score.ResetScore();
        Debug.Log("[Lab] Reset to start");
    }

    public void ToggleHelp()
    {
        if (helpPanel != null) helpPanel.SetActive(!helpPanel.activeSelf);
    }
}
