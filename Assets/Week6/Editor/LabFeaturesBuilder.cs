#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Меню: Week6 -> Add Lab Features
// Добавляет в УЖЕ существующую сцену Week5_Locomotion то, что просит Week 5 lab (не пересоздаёт сцену):
//  - ещё 3 Teleportation Anchor (всего 5);
//  - меню на запястье левой руки (World Space 0.2 x 0.15 м): Teleport Mode / Smooth Mode / Reset / Help;
//  - счёт в двух вариантах: HUD на камере (недиегетический) и табло в сцене (диегетический);
//  - ModeSwitcher (переключение режима в игре, также кнопка Y на левом контроллере) и ScoreTracker.
// Можно запускать повторно: старые объекты Lab* удаляются и создаются заново.
public static class LabFeaturesBuilder
{
    const string Dir = "Assets/Week6";
    const string ScenePath = Dir + "/Week5_Locomotion.unity";

    [MenuItem("Week6/Add Lab Features")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Camera cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError("Lab: в сцене нет камеры (XR Origin). Сначала Week6 > Build Locomotion Scene."); return; }

        Transform left = null;
        foreach (var m in Object.FindObjectsByType<ControllerInputActionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (m.name.ToLower().Contains("left")) { left = m.transform; break; }
        if (left == null) { Debug.LogError("Lab: не нашёл левый контроллер (ControllerInputActionManager с 'left' в имени)."); return; }

        ComfortSettings comfort = Object.FindFirstObjectByType<ComfortSettings>(FindObjectsInactive.Include);
        if (comfort == null) { Debug.LogError("Lab: в сцене нет ComfortSettings."); return; }

        GameObject env = GameObject.Find("Environment");

        // --- чистим то, что создали раньше (повторный запуск) ---
        DestroyByName("LabFeatures");
        var oldAnchor = left.Find("WristMenuAnchor"); if (oldAnchor != null) Object.DestroyImmediate(oldAnchor.gameObject);
        var oldHud = cam.transform.Find("ScoreHUD");   if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);
        foreach (var n in new[] { "Anchor_Start", "Anchor_1", "Anchor_3" }) DestroyByName(n);

        GameObject root = new GameObject("LabFeatures");
        int teleportMask = InteractionLayerMask.GetMask("Teleport");

        // --- 1. Якоря: к существующим Anchor_2 и Anchor_Finish добавляем ещё три (всего 5) ---
        Material anchorMat = Mat("W6_Anchor", new Color(0.1f, 0.85f, 0.85f));
        Anchor("Anchor_Start", new Vector3(0, 0, 1),   0f,   teleportMask, anchorMat, env);    // смотрит на +Z к маркеру 1
        Anchor("Anchor_1",     new Vector3(0, 0, 10),  90f,  teleportMask, anchorMat, env);    // смотрит на +X к маркеру 2
        Anchor("Anchor_3",     new Vector3(8, 0, 16),  180f, teleportMask, anchorMat, env);    // смотрит на -Z к Finish

        // --- 2. EventSystem с XR UI Input Module (нужен, чтобы нажимать кнопки лучом/пальцем) ---
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();
        if (es.GetComponent<XRUIInputModule>() == null)
        {
            foreach (var mod in es.GetComponents<BaseInputModule>()) Object.DestroyImmediate(mod);
            es.gameObject.AddComponent<XRUIInputModule>();
        }

        // --- 3. ModeSwitcher ---
        var switcherGo = new GameObject("ModeSwitcher");
        switcherGo.transform.SetParent(root.transform);
        var switcher = switcherGo.AddComponent<ModeSwitcher>();
        switcher.comfort = comfort;
        if (env != null)
        {
            var start = env.transform.Find("Marker_Start");
            if (start != null) switcher.startPoint = start;
        }

        // --- 4. Меню на запястье ---
        var anchorGo = new GameObject("WristMenuAnchor");
        anchorGo.transform.SetParent(left, false);
        anchorGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);       // ~5 см от начала контроллера
        anchorGo.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);    // лицом вверх/к игроку

        GameObject wrist = MakeCanvas("WristMenuCanvas", anchorGo.transform, new Vector2(200, 150), 0.001f, true);
        MakeBackground(wrist.transform, new Color(0.08f, 0.1f, 0.16f, 0.92f));
        var bTele  = MakeButton(wrist.transform, "Teleport Mode", new Vector2(0.03f, 0.52f), new Vector2(0.49f, 0.97f));
        var bSmooth= MakeButton(wrist.transform, "Smooth Mode",   new Vector2(0.51f, 0.52f), new Vector2(0.97f, 0.97f));
        var bReset = MakeButton(wrist.transform, "Reset",         new Vector2(0.03f, 0.03f), new Vector2(0.49f, 0.48f));
        var bHelp  = MakeButton(wrist.transform, "Help",          new Vector2(0.51f, 0.03f), new Vector2(0.97f, 0.48f));

        GameObject help = new GameObject("HelpPanel", typeof(RectTransform));
        help.transform.SetParent(wrist.transform, false);
        Stretch((RectTransform)help.transform);
        MakeBackground(help.transform, new Color(0.05f, 0.05f, 0.08f, 0.97f));
        MakeText(help.transform, "HelpText",
            "Y button: Teleport <-> Smooth\nTeleport: push stick forward, release\nSmooth: move with the stick\nReset: back to start\nTap Help to close",
            11f, Color.white, Vector2.zero, Vector2.one);
        help.SetActive(false);
        switcher.helpPanel = help;

        UnityEventTools.AddPersistentListener(bTele.onClick, switcher.SetTeleportMode);
        UnityEventTools.AddPersistentListener(bSmooth.onClick, switcher.SetSmoothMode);
        UnityEventTools.AddPersistentListener(bReset.onClick, switcher.ResetPosition);
        UnityEventTools.AddPersistentListener(bHelp.onClick, switcher.ToggleHelp);

        var wristCtrl = anchorGo.AddComponent<WristMenuController>();
        wristCtrl.menuRoot = wrist;
        wristCtrl.head = cam.transform;
        wrist.SetActive(false);

        // --- 5. Счёт ---
        var tracker = new GameObject("ScoreTracker").AddComponent<ScoreTracker>();
        tracker.transform.SetParent(root.transform);
        if (env != null)
        {
            foreach (var t in env.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Marker_") && t.name != "Marker_Start") tracker.targets.Add(t);
        }

        // 5a. Недиегетический: HUD, прикреплённый к камере (в VR экранный Canvas не виден, поэтому "head-locked")
        GameObject hud = MakeCanvas("ScoreHUD", cam.transform, new Vector2(260, 60), 0.001f, false);
        hud.transform.localPosition = new Vector3(0.28f, 0.17f, 0.65f);
        hud.transform.localRotation = Quaternion.identity;
        MakeBackground(hud.transform, new Color(0f, 0f, 0f, 0.55f));
        tracker.displays.Add(MakeText(hud.transform, "ScoreText", "Score: 0 / 0", 32f, Color.white, Vector2.zero, Vector2.one));

        // 5b. Диегетический: табло в сцене слева от старта
        Material boardMat = Mat("W6_Board", new Color(0.12f, 0.12f, 0.14f));
        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Scoreboard";
        board.transform.SetParent(root.transform);
        board.transform.position = new Vector3(-2.5f, 1.6f, 0.5f);
        board.transform.localScale = new Vector3(0.1f, 0.9f, 1.5f);
        board.GetComponent<Renderer>().sharedMaterial = boardMat;
        Object.DestroyImmediate(board.GetComponent<Collider>());
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "ScoreboardPost";
        post.transform.SetParent(root.transform);
        post.transform.position = new Vector3(-2.5f, 0.6f, 0.5f);
        post.transform.localScale = new Vector3(0.1f, 1.2f, 0.1f);
        post.GetComponent<Renderer>().sharedMaterial = boardMat;

        GameObject boardCanvas = MakeCanvas("ScoreboardCanvas", root.transform, new Vector2(700, 300), 0.002f, false);
        boardCanvas.transform.position = new Vector3(-2.5f + 0.06f, 1.6f, 0.5f);
        boardCanvas.transform.rotation = Quaternion.Euler(0f, -90f, 0f);     // лицом к игроку у старта (+X сторона табло)
        tracker.displays.Add(MakeText(boardCanvas.transform, "ScoreText", "Score: 0 / 0", 90f, Color.white, Vector2.zero, Vector2.one));

        EditorUtility.SetDirty(switcher);
        EditorUtility.SetDirty(tracker);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Lab: добавлено - 3 якоря (всего 5), меню на запястье, HUD и табло со счётом, ModeSwitcher. Сцена сохранена. Дальше: File > Build Profiles > Meta Quest > Build And Run.");
    }

    // ---------- helpers ----------

    static void DestroyByName(string name)
    {
        GameObject g;
        while ((g = GameObject.Find(name)) != null) Object.DestroyImmediate(g);
    }

    static Material Mat(string name, Color c)
    {
        string path = Dir + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh) { color = c };
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void Anchor(string name, Vector3 pos, float yaw, int mask, Material mat, GameObject parent)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = name;
        if (parent != null) g.transform.SetParent(parent.transform);
        g.transform.position = pos + Vector3.up * 0.03f;     // чуть выше маркера, чтобы не мерцало
        g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        g.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
        g.GetComponent<Renderer>().sharedMaterial = mat;
        var a = g.AddComponent<TeleportationAnchor>();
        a.interactionLayers = mask;
        a.matchOrientation = MatchOrientation.TargetUpAndForward;
        a.teleportAnchorTransform = g.transform;
    }

    static GameObject MakeCanvas(string name, Transform parent, Vector2 size, float scale, bool interactive)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        if (interactive) go.AddComponent<TrackedDeviceGraphicRaycaster>();
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one * scale;
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void MakeBackground(Transform parent, Color c)
    {
        var go = new GameObject("Background", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color col, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = col;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return t;
    }

    static Button MakeButton(Transform parent, string label, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.32f, 0.62f, 1f);
        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.highlightedColor = new Color(0.35f, 0.6f, 1f, 1f);
        colors.pressedColor = new Color(0.1f, 0.2f, 0.4f, 1f);
        b.colors = colors;
        MakeText(go.transform, "Text", label, 16f, Color.white, Vector2.zero, Vector2.one);
        return b;
    }
}
#endif
