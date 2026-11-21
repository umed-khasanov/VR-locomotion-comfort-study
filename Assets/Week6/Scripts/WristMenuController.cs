using UnityEngine;

// Lab Part 3: меню на запястье левой руки.
// Скрипт висит на WristMenuAnchor (дочерний объект левого контроллера). Меню (menuRoot) показывается,
// когда игрок смотрит на запястье и оно повёрнуто лицом к нему, и прячется, когда он отводит взгляд.
public class WristMenuController : MonoBehaviour
{
    public GameObject menuRoot;
    public Transform head;
    [Range(10f, 80f)] public float lookAngle = 35f;   // угол между взглядом и направлением на запястье
    public float maxDistance = 0.7f;                  // м от головы до запястья

    void Start()
    {
        if (head == null)
        {
            Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (cam != null) head = cam.transform;
        }
        if (menuRoot != null) menuRoot.SetActive(false);
    }

    void Update()
    {
        if (menuRoot == null || head == null) return;

        Vector3 to = transform.position - head.position;
        float dist = to.magnitude;
        bool visible = menuRoot.activeSelf;

        // небольшой гистерезис, чтобы меню не мигало на границе
        float angleLimit = visible ? lookAngle + 15f : lookAngle;
        float distLimit  = visible ? maxDistance + 0.15f : maxDistance;

        bool near    = dist < distLimit;
        bool looking = dist > 0.001f && Vector3.Angle(head.forward, to) < angleLimit;
        bool facing  = Vector3.Dot(transform.forward, to.normalized) > 0f;   // видимая сторона канваса смотрит на голову

        bool show = near && looking && facing;
        if (show != visible) menuRoot.SetActive(show);
    }
}
