using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;

// Week 6 practical: все настройки комфорта в одном компоненте.
// Меняешь значения в Inspector (даже во время Play) — они сразу применяются и пишутся в Console.
public class ComfortSettings : MonoBehaviour
{
    public enum Scheme { Teleport, Smooth }

    [Header("Схема передвижения")]
    public Scheme scheme = Scheme.Teleport;   // A = Teleport, B = Smooth
    public bool smoothTurn = false;           // false = snap turn (рывками), true = плавный поворот

    [Header("Комфорт")]
    [Range(0.5f, 4f)]   public float moveSpeed = 2f;          // м/с
    [Range(15f, 90f)]   public float snapTurnAngle = 45f;     // градусы за рывок
    [Range(30f, 180f)]  public float smoothTurnSpeed = 60f;   // градусы в секунду
    [Range(0f, 1f)]     public float vignetteAperture = 0.7f; // 1 = без виньетки, меньше = уже обзор

    [Header("Ссылки (если пусто — найдутся сами)")]
    public ControllerInputActionManager leftController;
    public ControllerInputActionManager rightController;
    public ContinuousMoveProvider moveProvider;
    public SnapTurnProvider snapTurnProvider;
    public ContinuousTurnProvider continuousTurnProvider;
    public TunnelingVignetteController vignette;

    bool changed;

    void Start()
    {
        AutoFind();
        Apply();
    }

    // Unity вызывает это, когда меняешь значение в Inspector
    void OnValidate() { changed = true; }

    void Update()
    {
        if (!changed) return;
        changed = false;
        Apply();
    }

    void AutoFind()
    {
        foreach (var m in FindObjectsByType<ControllerInputActionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = m.name.ToLower();
            if (n.Contains("left") && leftController == null) leftController = m;
            if (n.Contains("right") && rightController == null) rightController = m;
        }
        if (moveProvider == null) moveProvider = FindAnyObjectByType<ContinuousMoveProvider>(FindObjectsInactive.Include);
        if (snapTurnProvider == null) snapTurnProvider = FindAnyObjectByType<SnapTurnProvider>(FindObjectsInactive.Include);
        if (continuousTurnProvider == null) continuousTurnProvider = FindAnyObjectByType<ContinuousTurnProvider>(FindObjectsInactive.Include);
        if (vignette == null) vignette = FindAnyObjectByType<TunnelingVignetteController>(FindObjectsInactive.Include);
    }

    public void Apply()
    {
        bool smooth = scheme == Scheme.Smooth;
        // Smooth Motion выключен = работает телепорт; Smooth Turn выключен = snap turn
        if (leftController != null)  { leftController.smoothMotionEnabled = smooth;  leftController.smoothTurnEnabled = smoothTurn; }
        if (rightController != null) { rightController.smoothMotionEnabled = smooth; rightController.smoothTurnEnabled = smoothTurn; }

        if (moveProvider != null) moveProvider.moveSpeed = moveSpeed;
        if (snapTurnProvider != null) snapTurnProvider.turnAmount = snapTurnAngle;
        if (continuousTurnProvider != null) continuousTurnProvider.turnSpeed = smoothTurnSpeed;
        if (vignette != null) vignette.defaultParameters.apertureSize = vignetteAperture;

        // Строка для листа записи результатов
        Debug.Log($"[Comfort] Scheme={scheme} | SmoothTurn={smoothTurn} | MoveSpeed={moveSpeed:0.0} m/s | SnapTurn={snapTurnAngle:0}° | TurnSpeed={smoothTurnSpeed:0}°/s | Vignette={vignetteAperture:0.00}");
    }
}
