using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Окно для расстановки финала резки: камера берётся из текущего Scene-вида,
// точки мамы и двери ставятся кликом по полу в Scene-виде.
// Открыть: меню Donor > Финал резки.
public class CuttingFinaleSetupWindow : EditorWindow
{
    private enum PickMode { None, MomWatch, MomVia, MomHide, GGPoint }

    private int ggPointIndex = 0;

    private PickMode mode = PickMode.None;

    [MenuItem("Donor/Финал резки")]
    private static void Open()
    {
        GetWindow<CuttingFinaleSetupWindow>("Финал резки");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        CuttingFinale finale = FindObjectOfType<CuttingFinale>();
        if (finale == null)
        {
            EditorGUILayout.HelpBox("В открытой сцене нет компонента CuttingFinale. Открой HomeScene.", MessageType.Warning);
            return;
        }

        var so = new SerializedObject(finale);
        Transform reveal = so.FindProperty("revealAnchor").objectReferenceValue as Transform;
        Transform watch = so.FindProperty("momWatchPoint").objectReferenceValue as Transform;
        Transform hide = so.FindProperty("momHidePoint").objectReferenceValue as Transform;
        Transform via = GetVia(so);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1. Камера", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Настрой Scene-вид так, как должен выглядеть кадр, и нажми:", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(reveal == null))
        {
            if (GUILayout.Button("Камера — как сейчас в Scene-виде", GUILayout.Height(28)))
                SetCameraFromSceneView(finale, reveal);
            if (GUILayout.Button("Посмотреть из камеры"))
                LookThrough(reveal);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Где стоит испуганная мама", EditorStyles.boldLabel);
        PickButton(PickMode.MomWatch, watch, "Указать кликом в Scene-виде (под аркой)");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. Промежуточная точка по пути к двери", EditorStyles.boldLabel);
        PickButton(PickMode.MomVia, via, "Указать кликом в Scene-виде");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("4. Дверь, за которую уходит мама", EditorStyles.boldLabel);
        PickButton(PickMode.MomHide, hide, "Указать кликом в Scene-виде (у двери)");

        DoorWalkSequence walk = FindObjectOfType<DoorWalkSequence>();
        if (walk != null)
        {
            SerializedProperty walkPath = new SerializedObject(walk).FindProperty("walkPath");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("5. Путь ГГ к двери (по порядку)", EditorStyles.boldLabel);
            for (int i = 0; walkPath != null && i < walkPath.arraySize; i++)
            {
                Transform point = walkPath.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                bool active = mode == PickMode.GGPoint && ggPointIndex == i;
                using (new EditorGUI.DisabledScope(point == null))
                {
                    GUI.backgroundColor = active ? new Color(1f, 0.85f, 0.4f) : Color.white;
                    if (GUILayout.Button(active ? "…кликни по полу в Scene-виде" : $"Точка {i + 1}: указать кликом", GUILayout.Height(24)))
                    {
                        mode = active ? PickMode.None : PickMode.GGPoint;
                        ggPointIndex = i;
                        SceneView.lastActiveSceneView?.Focus();
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
        }

        EditorGUILayout.Space();
        if (mode != PickMode.None)
            EditorGUILayout.HelpBox("Кликни левой кнопкой по полу в Scene-виде. Esc — отмена.", MessageType.Info);

        EditorGUILayout.HelpBox("После расстановки сохрани сцену (Ctrl+S).", MessageType.None);
    }

    private void PickButton(PickMode target, Transform marker, string label)
    {
        using (new EditorGUI.DisabledScope(marker == null))
        {
            bool active = mode == target;
            GUI.backgroundColor = active ? new Color(1f, 0.85f, 0.4f) : Color.white;
            if (GUILayout.Button(active ? "…кликни по полу в Scene-виде" : label, GUILayout.Height(28)))
            {
                mode = active ? PickMode.None : target;
                SceneView.lastActiveSceneView?.Focus();
            }
            GUI.backgroundColor = Color.white;

            if (marker != null && GUILayout.Button("Показать точку"))
            {
                Selection.activeTransform = marker;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }
    }

    private void OnSceneGUI(SceneView view)
    {
        if (mode == PickMode.None) return;

        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            mode = PickMode.None;
            e.Use();
            Repaint();
            return;
        }

        if (e.type != EventType.MouseDown || e.button != 0) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!TryGetFloorPoint(ray, out Vector3 point)) return;

        CuttingFinale finale = FindObjectOfType<CuttingFinale>();
        var so = new SerializedObject(finale);
        Transform marker;
        if (mode == PickMode.GGPoint)
        {
            var walkSo = new SerializedObject(FindObjectOfType<DoorWalkSequence>());
            marker = walkSo.FindProperty("walkPath").GetArrayElementAtIndex(ggPointIndex).objectReferenceValue as Transform;
        }
        else
        {
            marker = mode == PickMode.MomWatch ? so.FindProperty("momWatchPoint").objectReferenceValue as Transform
                   : mode == PickMode.MomVia ? GetVia(so)
                   : so.FindProperty("momHidePoint").objectReferenceValue as Transform;
        }

        if (marker != null)
        {
            Undo.RecordObject(marker, "Поставить точку финала");
            marker.position = point;

            // Мама смотрит на ГГ
            Transform gg = (so.FindProperty("ggAnimator").objectReferenceValue as Animator)?.transform;
            if (gg != null)
            {
                Vector3 dir = gg.position - point;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f) marker.rotation = Quaternion.LookRotation(dir);
            }

            EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
            Selection.activeTransform = marker;
        }

        mode = PickMode.None;
        e.Use();
        Repaint();
    }

    private static Transform GetVia(SerializedObject so)
    {
        SerializedProperty path = so.FindProperty("retreatPath");
        if (path == null || path.arraySize == 0) return null;
        return path.GetArrayElementAtIndex(0).objectReferenceValue as Transform;
    }

    // Точка на полу: по коллайдерам сцены, если не попали — плоскость y = 0
    private static bool TryGetFloorPoint(Ray ray, out Vector3 point)
    {
        Physics.SyncTransforms();
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        var floor = new Plane(Vector3.up, Vector3.zero);
        if (floor.Raycast(ray, out float dist))
        {
            point = ray.GetPoint(dist);
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private static void SetCameraFromSceneView(CuttingFinale finale, Transform reveal)
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null || reveal == null) return;

        Undo.RecordObject(reveal, "Камера финала из Scene-вида");
        Transform cam = view.camera.transform;
        reveal.SetPositionAndRotation(cam.position, cam.rotation);

        // Угол обзора тоже как в Scene-виде, чтобы кадр совпал
        var so = new SerializedObject(finale);
        so.FindProperty("revealFieldOfView").floatValue = view.camera.fieldOfView;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(reveal.gameObject.scene);
        Selection.activeTransform = reveal;
    }

    private static void LookThrough(Transform reveal)
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null || reveal == null) return;
        view.AlignViewToObject(reveal);
    }
}
