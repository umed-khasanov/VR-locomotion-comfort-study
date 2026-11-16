#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// Меню: Week6 → Build Locomotion Scene
// Строит сцену Week5_Locomotion по брифу Week 6 practical (Part 1):
// пол 40x40 с Teleportation Area, препятствия, маршрут ~34 м с 5 маркерами (поворот 90° и 180°),
// 2 Teleportation Anchor, XR Origin из Starter Assets, Tunneling Vignette и объект ComfortSettings.
public static class LocomotionSceneBuilder
{
    const string Dir = "Assets/Week6";
    const string ScenePath = Dir + "/Week5_Locomotion.unity";
    const string Starter = "Assets/Samples/XR Interaction Toolkit/3.3.2/Starter Assets";

    [MenuItem("Week6/Build Locomotion Scene")]
    public static void Build()
    {
        Directory.CreateDirectory(Dir + "/Materials");
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponent<Camera>() != null) Object.DestroyImmediate(root);   // бриф: удалить Main Camera

        // --- Rig ---
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Starter + "/Prefabs/XR Origin (XR Rig).prefab");
        if (rigPrefab == null) { Debug.LogError("Week6: не найден XR Origin (XR Rig).prefab в Starter Assets"); return; }
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rig.transform.position = new Vector3(0, 0, 0);

        Material grey  = Mat("W6_Floor",  new Color(0.55f, 0.6f, 0.65f));
        Material wall  = Mat("W6_Wall",   new Color(0.85f, 0.85f, 0.8f));
        Material green = Mat("W6_Start",  new Color(0.2f, 0.8f, 0.3f));
        Material blue  = Mat("W6_Marker", new Color(0.2f, 0.5f, 0.95f));
        Material red   = Mat("W6_Finish", new Color(0.9f, 0.2f, 0.2f));

        int teleportMask = InteractionLayerMask.GetMask("Teleport");
        if (teleportMask == 0) Debug.LogWarning("Week6: нет Interaction Layer 'Teleport' — Project Settings → XR Plug-in Management → Project Validation → Fix");

        GameObject env = new GameObject("Environment");

        // --- Пол 40x40 м = Teleportation Area ---
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(env.transform);
        floor.transform.localScale = new Vector3(4, 1, 4);
        floor.GetComponent<Renderer>().sharedMaterial = grey;
        var area = floor.AddComponent<TeleportationArea>();
        area.interactionLayers = teleportMask;

        // --- Препятствия (без Teleportation Area → луч над ними красный) ---
        Box("Wall_Middle", new Vector3(4, 1, 6),   new Vector3(5, 2, 7),  wall, env);  // нельзя срезать Start → 2
        Box("Wall_Lane",   new Vector3(10.5f, 1, 10), new Vector3(1, 2, 12), wall, env);
        Box("Pillar_1",    new Vector3(-2, 1, 13), new Vector3(1, 2, 1),  wall, env);
        Box("Pillar_2",    new Vector3(6, 1, 18),  new Vector3(1, 2, 1),  wall, env);

        // --- Маршрут: Start → 1 → 2 (90°) → 3 (90°) → Finish (180°), около 34 м ---
        Marker("Start",  new Vector3(0, 0, 1),   green, env);
        Marker("1",      new Vector3(0, 0, 10),  blue,  env);
        Marker("2",      new Vector3(8, 0, 10),  blue,  env);
        Marker("3",      new Vector3(8, 0, 16),  blue,  env);
        Marker("Finish", new Vector3(8, 0, 4),   red,   env);   // от 3 разворот на 180° и назад

        // --- Teleportation Anchors у маркера 2 и Finish ---
        Anchor("Anchor_2",      new Vector3(8, 0, 10), 0f,   teleportMask, env);   // смотрит на 3 (+Z)
        Anchor("Anchor_Finish", new Vector3(8, 0, 4),  180f, teleportMask, env);   // после разворота

        // --- Провайдеры на риге ---
        var move  = rig.GetComponentInChildren<DynamicMoveProvider>(true);
        var snap  = rig.GetComponentInChildren<SnapTurnProvider>(true);
        var cturn = rig.GetComponentInChildren<ContinuousTurnProvider>(true);
        var tele  = rig.GetComponentInChildren<TeleportationProvider>(true);
        if (move != null) { move.moveSpeed = 2f; move.enableStrafe = true; }
        if (snap != null) snap.turnAmount = 45f;
        if (cturn != null) cturn.turnSpeed = 60f;

        // --- Tunneling Vignette под Main Camera ---
        TunnelingVignetteController vig = null;
        Transform cam = null;
        foreach (var c in rig.GetComponentsInChildren<Camera>(true)) { cam = c.transform; break; }
        var vigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Starter + "/TunnelingVignette/TunnelingVignette.prefab");
        if (cam != null && vigPrefab != null)
        {
            var v = (GameObject)PrefabUtility.InstantiatePrefab(vigPrefab, cam);
            v.transform.localPosition = Vector3.zero;
            v.transform.localRotation = Quaternion.identity;
            vig = v.GetComponent<TunnelingVignetteController>();
            if (vig != null)
            {
                var p = vig.defaultParameters;
                p.apertureSize = 0.7f; p.featheringEffect = 0.2f; p.easeInTime = 0.3f; p.easeOutTime = 0.3f;
                vig.locomotionVignetteProviders.Clear();
                AddVig(vig, move);
                AddVig(vig, cturn);
                AddVig(vig, snap);
                if (tele != null)   // по желанию из брифа: "моргание" при телепорте
                {
                    var tp = new LocomotionVignetteProvider { locomotionProvider = tele, enabled = true, overrideDefaultParameters = true };
                    tp.overrideParameters.apertureSize = 0f;
                    vig.locomotionVignetteProviders.Add(tp);
                }
            }
        }
        else Debug.LogWarning("Week6: не нашёл камеру или TunnelingVignette.prefab");

        // --- ComfortSettings ---
        var cs = new GameObject("ComfortSettings").AddComponent<ComfortSettings>();
        foreach (var m in rig.GetComponentsInChildren<ControllerInputActionManager>(true))
        {
            string n = m.name.ToLower();
            if (n.Contains("left")) cs.leftController = m;
            if (n.Contains("right")) cs.rightController = m;
        }
        cs.moveProvider = move; cs.snapTurnProvider = snap; cs.continuousTurnProvider = cturn; cs.vignette = vig;

        EditorSceneManager.SaveScene(scene, ScenePath);

        // Сцена локомоции — первой в билде
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes) if (s.path != ScenePath) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();

        Debug.Log("Week6: сцена готова → " + ScenePath + ". Выбери ComfortSettings и меняй Scheme: Teleport / Smooth.");
    }

    static void AddVig(TunnelingVignetteController vig, LocomotionProvider p)
    {
        if (p == null) return;
        vig.locomotionVignetteProviders.Add(new LocomotionVignetteProvider { locomotionProvider = p, enabled = true });
    }

    static Material Mat(string name, Color c)
    {
        string path = $"{Dir}/Materials/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh) { color = c };
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void Box(string name, Vector3 pos, Vector3 scale, Material m, GameObject parent)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent.transform);
        g.transform.position = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
    }

    static void Marker(string label, Vector3 pos, Material m, GameObject parent)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = "Marker_" + label;
        g.transform.SetParent(parent.transform);
        g.transform.position = pos + Vector3.up * 0.01f;
        g.transform.localScale = new Vector3(1.2f, 0.01f, 1.2f);
        g.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(g.GetComponent<Collider>());   // не мешает телепорту

        var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);   // столбик, видно издалека
        pole.name = "Pole";
        pole.transform.SetParent(g.transform, true);
        pole.transform.position = pos + new Vector3(0.7f, 1f, 0);
        pole.transform.localScale = new Vector3(0.1f, 2f, 0.1f);
        pole.GetComponent<Renderer>().sharedMaterial = m;

        var t = new GameObject("Label");
        t.transform.SetParent(g.transform, true);
        t.transform.position = pos + new Vector3(0.7f, 2.3f, 0);
        var tm = t.AddComponent<TextMesh>();
        tm.text = label; tm.characterSize = 0.15f; tm.fontSize = 48;
        tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white;
    }

    static void Anchor(string name, Vector3 pos, float yaw, int mask, GameObject parent)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = name;
        g.transform.SetParent(parent.transform);
        g.transform.position = pos + Vector3.up * 0.02f;
        g.transform.rotation = Quaternion.Euler(0, yaw, 0);       // синяя стрелка (forward) — куда смотреть
        g.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
        var a = g.AddComponent<TeleportationAnchor>();
        a.interactionLayers = mask;
        a.matchOrientation = MatchOrientation.TargetUpAndForward;
        a.teleportAnchorTransform = g.transform;
    }
}
#endif
