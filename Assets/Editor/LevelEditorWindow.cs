using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Edits a level by drawing it as the corridor seen from the side, over its whole length.
// The horizontal axis is the level's distance: something at distance 10 reaches the player
// once the level has scrolled 10 units. Open it from Window > Fuck Gravity > Level Editor.
public class LevelEditorWindow : EditorWindow
{
    enum Kind { None, Obstacle, Collectible, End }

    const int SelectTool = -1;          // other tools: an index in prefabs, or prefabs.Count for collectibles
    const string LevelsFolder = "Assets/Resources/Levels";
    const float SidebarWidth = 150f;
    const float InspectorWidth = 300f;
    const float ViewMargin = 0.5f;      // world units shown beyond each platform
    const float HitSlop = 4f;           // pixels
    const float DuplicateOffset = 2f;
    const float StartScroll = -3f;
    // Asset path of the level played from this window, to come back to when Play mode stops.
    const string ReturnKey = "FuckGravity.ReturnToLevelEditor";

    static readonly Color Background = new Color(0.08f, 0.09f, 0.12f);
    static readonly Color MinorGrid = new Color(1f, 1f, 1f, 0.04f);
    static readonly Color MajorGrid = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color PlatformColor = new Color(0.35f, 0.38f, 0.45f);
    static readonly Color PastEndColor = new Color(0f, 0f, 0f, 0.4f);
    static readonly Color EndColor = new Color(0.4f, 1f, 0.5f);
    static readonly Color PlayerColor = new Color(0.2f, 0.8f, 1f, 0.6f);
    static readonly Color ObstacleColor = new Color(0.85f, 0.3f, 0.3f);
    static readonly Color ObstacleOutline = new Color(0.45f, 0.1f, 0.1f);
    static readonly Color ReachColor = new Color(0.85f, 0.3f, 0.3f, 0.18f);
    static readonly Color BrokenColor = new Color(1f, 0f, 1f, 0.8f);
    static readonly Color CollectibleColor = new Color(1f, 0.8f, 0.1f);
    static readonly Color SelectedColor = Color.white;
    const float GhostAlpha = 0.35f;

    [SerializeField] LevelData level;
    [SerializeField] float scroll = StartScroll;   // distance at the left edge of the canvas
    [SerializeField] float zoom = 1f;              // horizontal stretch; 1 = same scale as vertically
    [SerializeField] float snap = 0.5f;
    [SerializeField] int tool = SelectTool;

    List<LevelObstacle> prefabs;
    // Per obstacle type, the data of the last one selected: new ones are copies of it.
    readonly Dictionary<LevelObstacle, ObstacleTypeData> templates = new Dictionary<LevelObstacle, ObstacleTypeData>();
    SerializedObject serialized;
    Vector2 inspectorScroll;

    Kind selectedKind;
    int selectedIndex;
    bool dragging;
    bool panning;
    Vector2 dragOffset;
    int dragUndoGroup;

    Rect canvas;

    [MenuItem("Window/Fuck Gravity/Level Editor")]
    static void ShowWindow() => GetWindow<LevelEditorWindow>("Level Editor");

    public static void Open(LevelData level)
    {
        var window = GetWindow<LevelEditorWindow>("Level Editor");
        if (window.level != level)
            window.SetLevel(level);
    }

    // Entering Play mode puts the Game view in front of this window; bring it back once Play stops.
    [InitializeOnLoadMethod]
    static void ReturnAfterPlay()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            string path = SessionState.GetString(ReturnKey, "");
            if (state != PlayModeStateChange.EnteredEditMode || path == "")
                return;

            SessionState.EraseString(ReturnKey);
            var played = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (played != null)
                Open(played);
            else
                ShowWindow();
        };
    }

    void OnEnable()
    {
        wantsMouseMove = true;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    void OnUndoRedo()
    {
        ClampSelection();
        Repaint();
    }

    void OnProjectChange()
    {
        prefabs = null;
        Repaint();
    }

    void OnSelectionChange()
    {
        if (Selection.activeObject is LevelData selected && selected != level)
            SetLevel(selected);
    }

    // Keeps the drawing in sync when the level is edited from the Inspector.
    void OnInspectorUpdate() => Repaint();

    void SetLevel(LevelData newLevel)
    {
        level = newLevel;
        serialized = null;
        Select(Kind.None, 0);
        scroll = StartScroll;
        Repaint();
    }

    void OnGUI()
    {
        prefabs ??= FindObstaclePrefabs();
        if (level != null && (serialized == null || serialized.targetObject != level))
            serialized = new SerializedObject(level);
        ClampSelection();
        HandleKeys();

        DrawToolbar();
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSidebar();
            if (level == null)
            {
                EditorGUILayout.HelpBox("Pick a level on the left, or create one.", MessageType.Info);
            }
            else
            {
                DrawCanvas(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)));
                DrawInspector();
            }
        }
    }

    // ---- Panels ----

    void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            var picked = (LevelData)EditorGUILayout.ObjectField(level, typeof(LevelData), false, GUILayout.Width(180f));
            if (picked != level)
                SetLevel(picked);

            GUILayout.Space(8f);
            ToolButton(SelectTool, "Select");
            for (int i = 0; i < prefabs.Count; i++)
                ToolButton(i, prefabs[i].name);
            ToolButton(prefabs.Count, "Collectible");

            GUILayout.FlexibleSpace();
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 32f;
            snap = Mathf.Max(0f, EditorGUILayout.FloatField("Snap", snap, GUILayout.Width(80f)));
            EditorGUIUtility.labelWidth = labelWidth;

            using (new EditorGUI.DisabledScope(level == null))
            {
                if (GUILayout.Button("Fit", EditorStyles.toolbarButton))
                    Fit();
                using (new EditorGUI.DisabledScope(level == null || !EditorUtility.IsDirty(level)))
                {
                    if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                        AssetDatabase.SaveAssetIfDirty(level);
                }
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("▶ Play level", EditorStyles.toolbarButton))
                        Play(0f);
                }
            }
        }
    }

    void ToolButton(int value, string label)
    {
        if (GUILayout.Toggle(tool == value, label, EditorStyles.toolbarButton) && tool != value)
            tool = value;
    }

    void DrawSidebar()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(SidebarWidth)))
        {
            GUILayout.Label("Levels", EditorStyles.boldLabel);
            LevelCatalog catalog = Catalog;
            if (catalog == null)
            {
                EditorGUILayout.HelpBox($"No catalog at Resources/{Game.CatalogPath}", MessageType.Warning);
            }
            else
            {
                for (int i = 0; i < catalog.levels.Count; i++)
                {
                    LevelData listed = catalog.levels[i];
                    if (listed != null && GUILayout.Toggle(listed == level, $"{i + 1}. {listed.name}", GUI.skin.button) && listed != level)
                        SetLevel(listed);
                }

                if (level != null && !catalog.levels.Contains(level) && GUILayout.Button("Add to catalog"))
                    AddToCatalog(catalog, level);
            }

            GUILayout.Space(4f);
            if (GUILayout.Button("New level…"))
                CreateLevel();
            GUILayout.FlexibleSpace();
        }
    }

    void DrawInspector()
    {
        using var scope = new EditorGUILayout.ScrollViewScope(inspectorScroll, GUILayout.Width(InspectorWidth));
        inspectorScroll = scope.scrollPosition;
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 110f;

        serialized.Update();
        GUILayout.Label(level.name, EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serialized.FindProperty(nameof(LevelData.scrollSpeed)));
        EditorGUILayout.PropertyField(serialized.FindProperty(nameof(LevelData.length)));
        if (level.scrollSpeed > 0f)
            EditorGUILayout.LabelField("Duration", $"{level.length / level.scrollSpeed:0.0} s");
        EditorGUILayout.LabelField("Contents", $"{level.obstacles.Count} obstacles, {level.collectibles.Count} collectibles");

        EditorGUILayout.Space();
        bool duplicate = false;
        bool delete = false;
        SerializedProperty selected = SelectedProperty();
        if (selected != null)
        {
            GUILayout.Label("Selected", EditorStyles.boldLabel);
            selected.isExpanded = true;
            EditorGUILayout.PropertyField(selected, new GUIContent(selectedKind.ToString()), true);
            using (new EditorGUILayout.HorizontalScope())
            {
                duplicate = GUILayout.Button("Duplicate");
                delete = GUILayout.Button("Delete");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Click an obstacle or a collectible to edit it.", MessageType.None);
        }
        serialized.ApplyModifiedProperties();

        // After applying, so these direct edits are not overwritten by the serialized copy.
        if (duplicate)
            DuplicateSelected();
        if (delete)
            DeleteSelected();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Pick a tool and click empty space to add; with Select, click to select.\n" +
            "Drag to move. Blocks switch to the ceiling when dragged into the top half; doors move their gap.\n" +
            "Delete / Backspace: delete. Ctrl/Cmd+D: duplicate. Esc: Select tool.\n" +
            "Wheel: scroll. Ctrl/Cmd+wheel: zoom. Middle or Alt+drag: pan.\n" +
            "Drag the green End line to change the length.\n" +
            "Right-click: play from that point.",
            MessageType.None);

        EditorGUIUtility.labelWidth = labelWidth;
    }

    // ---- Canvas ----

    float PixelsPerUnitY => canvas.height / (2f * (Game.PlatformSurfaceY + Game.PlatformThickness + ViewMargin));
    float PixelsPerUnitX => PixelsPerUnitY * zoom;

    // Canvas-local pixels <-> (distance, height).
    Vector2 ToScreen(float distance, float y) =>
        new Vector2((distance - scroll) * PixelsPerUnitX, canvas.height / 2f - y * PixelsPerUnitY);

    Vector2 ToWorld(Vector2 point) =>
        new Vector2(point.x / PixelsPerUnitX + scroll, (canvas.height / 2f - point.y) / PixelsPerUnitY);

    Rect ToScreen(Rect rect, float distance)
    {
        Vector2 topLeft = ToScreen(distance + rect.xMin, rect.yMax);
        Vector2 bottomRight = ToScreen(distance + rect.xMax, rect.yMin);
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    void DrawCanvas(Rect rect)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        if (Event.current.type == EventType.Layout)
            return;

        canvas = rect;
        GUI.BeginClip(rect);
        HandleMouse(id);
        if (Event.current.type == EventType.Repaint)
            Paint();
        GUI.EndClip();
    }

    void HandleMouse(int id)
    {
        Event e = Event.current;
        bool inside = new Rect(Vector2.zero, canvas.size).Contains(e.mousePosition);
        Vector2 world = ToWorld(e.mousePosition);

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown when inside && (e.button == 0 || e.button == 2):
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = id;
                if (e.button == 2 || e.alt)
                    panning = true;
                else
                    PressAt(e.mousePosition, world);
                e.Use();
                break;

            case EventType.MouseDrag when GUIUtility.hotControl == id:
                if (panning)
                    scroll -= e.delta.x / PixelsPerUnitX;
                else if (dragging)
                    DragTo(world);
                e.Use();
                Repaint();
                break;

            case EventType.MouseUp when GUIUtility.hotControl == id:
                if (dragging)
                    EndDrag();
                panning = false;
                GUIUtility.hotControl = 0;
                e.Use();
                break;

            case EventType.ScrollWheel when inside:
                if (e.control || e.command)
                {
                    zoom = Mathf.Clamp(zoom * (1f - e.delta.y * 0.03f), 0.02f, 5f);
                    scroll = world.x - e.mousePosition.x / PixelsPerUnitX;   // keep the point under the mouse
                }
                else
                {
                    scroll += (e.delta.x + e.delta.y) * 10f / PixelsPerUnitX;
                }
                e.Use();
                Repaint();
                break;

            case EventType.ContextClick when inside:
                ShowContextMenu(e.mousePosition, world);
                e.Use();
                break;

            case EventType.MouseMove when inside:
                Repaint();
                break;
        }
    }

    void PressAt(Vector2 mouse, Vector2 world)
    {
        Undo.IncrementCurrentGroup();
        dragUndoGroup = Undo.GetCurrentGroup();

        (Kind kind, int index) = HitTest(mouse);
        if (kind == Kind.None && tool != SelectTool)
            (kind, index) = Add(world);

        Select(kind, index);
        dragging = kind != Kind.None;
        if (dragging)
            dragOffset = Anchor() - world;
    }

    // The point of the selection that follows the mouse while dragging.
    Vector2 Anchor()
    {
        switch (selectedKind)
        {
            case Kind.Obstacle:
                ObstacleData obstacle = level.obstacles[selectedIndex];
                return new Vector2(obstacle.distance, Height(obstacle.typeData));
            case Kind.Collectible:
                CollectibleData collectible = level.collectibles[selectedIndex];
                return new Vector2(collectible.distance, collectible.y);
            case Kind.End:
                return new Vector2(level.length, 0f);
            default:
                return Vector2.zero;
        }
    }

    void DragTo(Vector2 world)
    {
        Vector2 target = world + dragOffset;
        Undo.RecordObject(level, "Move in level");
        switch (selectedKind)
        {
            case Kind.Obstacle:
                ObstacleData obstacle = level.obstacles[selectedIndex];
                obstacle.distance = Snap(target.x);
                SetHeight(obstacle.typeData, target.y);
                break;
            case Kind.Collectible:
                CollectibleData collectible = level.collectibles[selectedIndex];
                collectible.distance = Snap(target.x);
                collectible.y = CollectibleY(target.y);
                break;
            case Kind.End:
                level.length = Mathf.Max(1f, Snap(target.x));
                break;
        }
        EditorUtility.SetDirty(level);
    }

    void EndDrag()
    {
        dragging = false;
        SortByDistance();
        Undo.CollapseUndoOperations(dragUndoGroup);
    }

    // The height an obstacle type lets the mouse move: which platform a block sits on, where a
    // door's gap is. Other types only move along the level.
    static float Height(ObstacleTypeData data)
    {
        switch (data)
        {
            case BlockData block:
                return (block.side == PlatformSide.Floor ? -1f : 1f) * Game.PlatformSurfaceY / 2f;
            case DoorData door:
                return door.gapCenter;
            default:
                return 0f;
        }
    }

    void SetHeight(ObstacleTypeData data, float y)
    {
        switch (data)
        {
            case BlockData block:
                block.side = y > 0f ? PlatformSide.Ceiling : PlatformSide.Floor;
                break;
            case DoorData door:
                door.gapCenter = Mathf.Clamp(Snap(y), -Game.PlatformSurfaceY, Game.PlatformSurfaceY);
                break;
        }
    }

    float CollectibleY(float y)
    {
        float limit = Game.PlatformSurfaceY - Game.CollectibleSize / 2f;
        return Mathf.Clamp(Snap(y), -limit, limit);
    }

    float Snap(float value) => snap > 0f ? Mathf.Round(value / snap) * snap : value;

    (Kind, int) HitTest(Vector2 mouse)
    {
        for (int i = level.collectibles.Count - 1; i >= 0; i--)
        {
            CollectibleData collectible = level.collectibles[i];
            float radius = Game.CollectibleSize / 2f * PixelsPerUnitY;
            if (Vector2.Distance(mouse, ToScreen(collectible.distance, collectible.y)) <= radius + HitSlop)
                return (Kind.Collectible, i);
        }

        for (int i = level.obstacles.Count - 1; i >= 0; i--)
        {
            ObstacleData obstacle = level.obstacles[i];
            if (obstacle.prefab == null || obstacle.typeData == null)
            {
                if (Mathf.Abs(mouse.x - ToScreen(obstacle.distance, 0f).x) <= HitSlop)
                    return (Kind.Obstacle, i);
                continue;
            }

            foreach (Rect rect in obstacle.prefab.PreviewParts(obstacle.typeData).Concat(obstacle.prefab.PreviewReach(obstacle.typeData)))
            {
                if (rect.height > 0f && Grow(ToScreen(rect, obstacle.distance), HitSlop).Contains(mouse))
                    return (Kind.Obstacle, i);
            }
        }

        if (Mathf.Abs(mouse.x - ToScreen(level.length, 0f).x) <= HitSlop)
            return (Kind.End, 0);

        return (Kind.None, 0);
    }

    static Rect Grow(Rect rect, float margin) =>
        new Rect(rect.x - margin, rect.y - margin, rect.width + 2f * margin, rect.height + 2f * margin);

    void ShowContextMenu(Vector2 mouse, Vector2 world)
    {
        var menu = new GenericMenu();
        (Kind kind, int index) = HitTest(mouse);
        if (kind == Kind.Obstacle || kind == Kind.Collectible)
        {
            Select(kind, index);
            menu.AddItem(new GUIContent("Duplicate"), false, DuplicateSelected);
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelected);
            menu.AddSeparator("");
        }

        float from = Mathf.Clamp(Snap(world.x), 0f, level.length);
        var play = new GUIContent($"Play from distance {from:0.##}");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            menu.AddDisabledItem(play);
        else
            menu.AddItem(play, false, () => Play(from));
        menu.ShowAsContext();
    }

    void Paint()
    {
        var area = new Rect(Vector2.zero, canvas.size);
        EditorGUI.DrawRect(area, Background);
        PaintGrid();

        float surface = Game.PlatformSurfaceY;
        float thickness = Game.PlatformThickness;
        EditorGUI.DrawRect(Rect.MinMaxRect(0f, ToScreen(0f, surface + thickness).y, area.width, ToScreen(0f, surface).y), PlatformColor);
        EditorGUI.DrawRect(Rect.MinMaxRect(0f, ToScreen(0f, -surface).y, area.width, ToScreen(0f, -surface - thickness).y), PlatformColor);

        // The player's starting spot: everything at distance 0 is level with it.
        EditorGUI.DrawRect(ToScreen(new Rect(-0.5f, -surface, 1f, 1f), 0f), PlayerColor);

        for (int i = 0; i < level.obstacles.Count; i++)
            PaintObstacle(level.obstacles[i], selectedKind == Kind.Obstacle && i == selectedIndex, 1f);
        for (int i = 0; i < level.collectibles.Count; i++)
            PaintCollectible(level.collectibles[i], selectedKind == Kind.Collectible && i == selectedIndex, 1f);

        float endX = ToScreen(level.length, 0f).x;
        if (endX < area.width)
            EditorGUI.DrawRect(Rect.MinMaxRect(Mathf.Max(endX, 0f), 0f, area.width, area.height), PastEndColor);
        EditorGUI.DrawRect(new Rect(endX - 1f, 0f, 2f, area.height), selectedKind == Kind.End ? SelectedColor : EndColor);
        GUI.Label(new Rect(endX + 4f, 2f, 80f, 16f), "End", EditorStyles.whiteMiniLabel);

        Vector2 mouse = Event.current.mousePosition;
        if (mouseOverWindow == this && area.Contains(mouse))
        {
            if (tool != SelectTool && GUIUtility.hotControl == 0 && HitTest(mouse).Item1 == Kind.None)
                PaintGhost(ToWorld(mouse));

            Vector2 world = ToWorld(mouse);
            GUI.Label(new Rect(6f, area.height - 18f, 300f, 16f), $"distance {Snap(world.x):0.##}   height {Snap(world.y):0.##}", EditorStyles.whiteMiniLabel);
        }
    }

    void PaintGrid()
    {
        float right = ToWorld(new Vector2(canvas.width, 0f)).x;
        int minor = PixelsPerUnitX >= 8f ? 1 : PixelsPerUnitX >= 1.6f ? 5 : 10;
        int major = minor * 10;
        for (int d = Mathf.FloorToInt(scroll / minor) * minor; d <= right; d += minor)
        {
            float x = ToScreen(d, 0f).x;
            bool isMajor = d % major == 0;
            EditorGUI.DrawRect(new Rect(x, 0f, 1f, canvas.height), isMajor ? MajorGrid : MinorGrid);
            if (isMajor)
                GUI.Label(new Rect(x + 3f, 2f, 60f, 16f), d.ToString(), EditorStyles.whiteMiniLabel);
        }

        for (int y = -(int)Game.PlatformSurfaceY + 1; y < Game.PlatformSurfaceY; y++)
            EditorGUI.DrawRect(new Rect(0f, ToScreen(0f, y).y, canvas.width, 1f), y == 0 ? MajorGrid : MinorGrid);
    }

    void PaintObstacle(ObstacleData obstacle, bool selected, float alpha)
    {
        if (obstacle.prefab == null || obstacle.typeData == null)
        {
            EditorGUI.DrawRect(new Rect(ToScreen(obstacle.distance, 0f).x - 1f, 0f, 2f, canvas.height), Fade(selected ? SelectedColor : BrokenColor, alpha));
            return;
        }

        foreach (Rect reach in obstacle.prefab.PreviewReach(obstacle.typeData))
            EditorGUI.DrawRect(ToScreen(reach, obstacle.distance), Fade(ReachColor, alpha));
        foreach (Rect part in obstacle.prefab.PreviewParts(obstacle.typeData))
        {
            if (part.height > 0f)
                Handles.DrawSolidRectangleWithOutline(ToScreen(part, obstacle.distance), Fade(ObstacleColor, alpha), Fade(selected ? SelectedColor : ObstacleOutline, alpha));
        }
    }

    void PaintCollectible(CollectibleData collectible, bool selected, float alpha)
    {
        Vector2 centre = ToScreen(collectible.distance, collectible.y);
        float radius = Game.CollectibleSize / 2f * PixelsPerUnitY;
        Handles.color = Fade(CollectibleColor, alpha);
        Handles.DrawSolidDisc(centre, Vector3.forward, radius);
        if (selected)
        {
            Handles.color = SelectedColor;
            Handles.DrawWireDisc(centre, Vector3.forward, radius + 2f);
        }
        Handles.color = Color.white;
    }

    // What a click would add, under the mouse.
    void PaintGhost(Vector2 world)
    {
        if (tool == prefabs.Count)
            PaintCollectible(new CollectibleData { distance = Snap(world.x), y = CollectibleY(world.y) }, false, GhostAlpha);
        else if (tool >= 0 && tool < prefabs.Count)
            PaintObstacle(NewObstacle(prefabs[tool], world), false, GhostAlpha);
    }

    static Color Fade(Color color, float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);

    // ---- Editing ----

    (Kind, int) Add(Vector2 world)
    {
        Undo.RecordObject(level, "Add to level");
        if (tool == prefabs.Count)
        {
            level.collectibles.Add(new CollectibleData { distance = Snap(world.x), y = CollectibleY(world.y) });
            EditorUtility.SetDirty(level);
            return (Kind.Collectible, level.collectibles.Count - 1);
        }

        level.obstacles.Add(NewObstacle(prefabs[tool], world));
        EditorUtility.SetDirty(level);
        return (Kind.Obstacle, level.obstacles.Count - 1);
    }

    ObstacleData NewObstacle(LevelObstacle prefab, Vector2 world)
    {
        ObstacleTypeData data = templates.TryGetValue(prefab, out ObstacleTypeData template) && template.GetType() == prefab.DataType
            ? template.Clone()
            : prefab.CreateData();
        SetHeight(data, world.y);
        return new ObstacleData { distance = Snap(world.x), prefab = prefab, typeData = data };
    }

    void DuplicateSelected()
    {
        Undo.RecordObject(level, "Duplicate in level");
        if (selectedKind == Kind.Obstacle)
        {
            ObstacleData source = level.obstacles[selectedIndex];
            level.obstacles.Add(new ObstacleData
            {
                distance = source.distance + DuplicateOffset,
                prefab = source.prefab,
                typeData = source.typeData?.Clone(),
            });
            Select(Kind.Obstacle, level.obstacles.Count - 1);
        }
        else if (selectedKind == Kind.Collectible)
        {
            CollectibleData source = level.collectibles[selectedIndex];
            level.collectibles.Add(new CollectibleData { distance = source.distance + DuplicateOffset, y = source.y });
            Select(Kind.Collectible, level.collectibles.Count - 1);
        }
        SortByDistance();
        EditorUtility.SetDirty(level);
        Repaint();
    }

    void DeleteSelected()
    {
        Undo.RecordObject(level, "Delete from level");
        if (selectedKind == Kind.Obstacle)
            level.obstacles.RemoveAt(selectedIndex);
        else if (selectedKind == Kind.Collectible)
            level.collectibles.RemoveAt(selectedIndex);
        Select(Kind.None, 0);
        EditorUtility.SetDirty(level);
        Repaint();
    }

    // Keeps the lists in level order, so the Inspector reads like the level; the selection follows.
    void SortByDistance()
    {
        Undo.RecordObject(level, "Sort level");
        ObstacleData selectedObstacle = selectedKind == Kind.Obstacle ? level.obstacles[selectedIndex] : null;
        CollectibleData selectedCollectible = selectedKind == Kind.Collectible ? level.collectibles[selectedIndex] : null;

        level.obstacles = level.obstacles.OrderBy(o => o.distance).ToList();
        level.collectibles = level.collectibles.OrderBy(c => c.distance).ToList();

        if (selectedObstacle != null)
            selectedIndex = level.obstacles.IndexOf(selectedObstacle);
        else if (selectedCollectible != null)
            selectedIndex = level.collectibles.IndexOf(selectedCollectible);
        EditorUtility.SetDirty(level);
    }

    void HandleKeys()
    {
        Event e = Event.current;
        if (level == null || GUIUtility.keyboardControl != 0)
            return;

        bool hasItem = selectedKind == Kind.Obstacle || selectedKind == Kind.Collectible;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            tool = SelectTool;
            Select(Kind.None, 0);
            e.Use();
        }
        else if (hasItem && e.type == EventType.KeyDown && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
        {
            DeleteSelected();
            e.Use();
        }
        else if (hasItem && (e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand)
                 && (e.commandName == "Duplicate" || e.commandName == "Delete" || e.commandName == "SoftDelete"))
        {
            if (e.type == EventType.ExecuteCommand)
            {
                if (e.commandName == "Duplicate")
                    DuplicateSelected();
                else
                    DeleteSelected();
            }
            e.Use();
        }
    }

    // ---- Selection ----

    void Select(Kind kind, int index)
    {
        selectedKind = kind;
        selectedIndex = index;
        if (kind == Kind.Obstacle)
        {
            ObstacleData obstacle = level.obstacles[index];
            if (obstacle.prefab != null && obstacle.typeData != null)
                templates[obstacle.prefab] = obstacle.typeData;
        }
        Repaint();
    }

    void ClampSelection()
    {
        if (level == null
            || (selectedKind == Kind.Obstacle && selectedIndex >= level.obstacles.Count)
            || (selectedKind == Kind.Collectible && selectedIndex >= level.collectibles.Count))
        {
            selectedKind = Kind.None;
            dragging = false;
        }
    }

    SerializedProperty SelectedProperty()
    {
        switch (selectedKind)
        {
            case Kind.Obstacle:
                return serialized.FindProperty(nameof(LevelData.obstacles)).GetArrayElementAtIndex(selectedIndex);
            case Kind.Collectible:
                return serialized.FindProperty(nameof(LevelData.collectibles)).GetArrayElementAtIndex(selectedIndex);
            default:
                return null;
        }
    }

    // ---- Levels ----

    static LevelCatalog Catalog => Resources.Load<LevelCatalog>(Game.CatalogPath);

    static List<LevelObstacle> FindObstaclePrefabs()
    {
        return AssetDatabase.FindAssets("t:Prefab")
            .Select(guid => AssetDatabase.LoadAssetAtPath<LevelObstacle>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(prefab => prefab != null)
            .OrderBy(prefab => prefab.name)
            .ToList();
    }

    void CreateLevel()
    {
        LevelCatalog catalog = Catalog;
        string defaultName = $"Level{(catalog != null ? catalog.levels.Count + 1 : 1)}";
        string path = EditorUtility.SaveFilePanelInProject("New level", defaultName, "asset", "Where to save the new level.", LevelsFolder);
        if (string.IsNullOrEmpty(path))
            return;

        var created = CreateInstance<LevelData>();
        AssetDatabase.CreateAsset(created, path);
        if (catalog != null)
            AddToCatalog(catalog, created);
        SetLevel(created);
    }

    static void AddToCatalog(LevelCatalog catalog, LevelData added)
    {
        Undo.RecordObject(catalog, "Add level to catalog");
        catalog.levels.Add(added);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);
    }

    void Fit()
    {
        if (canvas.width <= 0f)
            return;
        scroll = StartScroll;
        zoom = canvas.width / ((level.length - 2f * StartScroll) * PixelsPerUnitY);
    }

    // Plays this level only, each attempt starting at a distance, until Play mode is stopped.
    void Play(float from)
    {
        SessionState.SetString(Game.TestLevelKey, AssetDatabase.GetAssetPath(level));
        SessionState.SetFloat(Game.TestStartKey, from);
        SessionState.SetString(ReturnKey, AssetDatabase.GetAssetPath(level));
        EditorApplication.isPlaying = true;
    }
}
