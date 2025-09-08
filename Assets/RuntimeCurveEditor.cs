using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class RuntimeCurveEditor : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private List<AnimationCurvePreset> availablePresets = new List<AnimationCurvePreset>();
    [SerializeField] private AnimationCurve currentCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    private CurveVisualElement curveVisualElement;
    private DropdownField presetDropdown;
    private Button saveButton;
    private Button newButton;
    private Toggle showControlsToggle;
    private FloatField rangeXField;
    private FloatField rangeYField;
    
    private VisualElement curveEditorPopup;
    private Button openEditorButton;
    private Button closeEditorButton;
    private bool isPopupOpen = false;
    
    public AnimationCurve CurrentCurve
    {
        get { return currentCurve; }
        private set
        {
            currentCurve = value;
            UpdateCurveDisplay();
            OnCurveChanged?.Invoke(currentCurve);
        }
    }
    
    public System.Action<AnimationCurve> OnCurveChanged;

    void Start()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument not found on RuntimeCurveEditor");
            return;
        }
        
        SetupUI();
        LoadAvailablePresets();
        UpdateCurveDisplay();
    }

    private void SetupUI()
    {
        var root = uiDocument.rootVisualElement;
        
        // Create main UI with just the open button
        var mainUI = new VisualElement();
        mainUI.style.position = Position.Absolute;
        mainUI.style.top = 10;
        mainUI.style.right = 10; // Move to top-right instead
        
        openEditorButton = new Button(() => TogglePopup(true)) { text = "Open Curve Editor" };
        openEditorButton.style.width = 150;
        openEditorButton.style.height = 30;
        mainUI.Add(openEditorButton);
        
        // Create popup window (initially hidden)
        CreatePopupWindow(root);
        
        root.Add(mainUI);
    }
    
    private void CreatePopupWindow(VisualElement root)
    {
        // Create full-screen overlay for modal behavior
        curveEditorPopup = new VisualElement();
        curveEditorPopup.style.position = Position.Absolute;
        curveEditorPopup.style.top = 0;
        curveEditorPopup.style.left = 0;
        curveEditorPopup.style.width = Length.Percent(100);
        curveEditorPopup.style.height = Length.Percent(100);
        curveEditorPopup.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f); // Semi-transparent overlay
        curveEditorPopup.style.display = DisplayStyle.None; // Hidden by default
        
        // Create the actual editor window
        var editorWindow = new VisualElement();
        editorWindow.style.position = Position.Absolute;
        editorWindow.style.width = 800;
        editorWindow.style.height = 600;
        editorWindow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        editorWindow.style.borderTopWidth = 2;
        editorWindow.style.borderBottomWidth = 2;
        editorWindow.style.borderLeftWidth = 2;
        editorWindow.style.borderRightWidth = 2;
        editorWindow.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        editorWindow.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        editorWindow.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        editorWindow.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        // Center the window
        editorWindow.style.left = Length.Percent(50);
        editorWindow.style.top = Length.Percent(50);
        editorWindow.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
        
        // Add title bar
        var titleBar = new VisualElement();
        titleBar.style.flexDirection = FlexDirection.Row;
        titleBar.style.height = 30;
        titleBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        titleBar.style.paddingLeft = 10;
        titleBar.style.paddingRight = 10;
        titleBar.style.justifyContent = Justify.SpaceBetween;
        titleBar.style.alignItems = Align.Center;
        
        var titleLabel = new Label("Animation Curve Editor");
        titleLabel.style.color = Color.white;
        titleBar.Add(titleLabel);
        
        closeEditorButton = new Button(() => TogglePopup(false)) { text = "✕" };
        closeEditorButton.style.width = 25;
        closeEditorButton.style.height = 25;
        closeEditorButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        titleBar.Add(closeEditorButton);
        
        editorWindow.Add(titleBar);
        
        // Add the curve editor content
        var contentArea = new VisualElement();
        contentArea.style.flexDirection = FlexDirection.Column;
        contentArea.style.flexGrow = 1;
        contentArea.style.paddingTop = 10;
        contentArea.style.paddingBottom = 10;
        contentArea.style.paddingLeft = 10;
        contentArea.style.paddingRight = 10;
        
        SetupControls(contentArea);
        SetupCurveDisplay(contentArea);
        
        editorWindow.Add(contentArea);
        curveEditorPopup.Add(editorWindow);
        root.Add(curveEditorPopup);
    }
    
    private void TogglePopup(bool open)
    {
        isPopupOpen = open;
        curveEditorPopup.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (open)
        {
            UpdateCurveDisplay(); // Refresh display when opening
        }
    }

    private void SetupControls(VisualElement parent)
    {
        var controlPanel = new VisualElement();
        controlPanel.style.flexDirection = FlexDirection.Row;
        controlPanel.style.height = 40;
        controlPanel.style.paddingBottom = 10;
        controlPanel.style.paddingTop = 10;
        controlPanel.style.paddingLeft = 10;
        controlPanel.style.paddingRight = 10;
        
        presetDropdown = new DropdownField("Preset:", new List<string> { "None" }, 0);
        presetDropdown.style.width = 200;
        presetDropdown.RegisterValueChangedCallback(OnPresetSelected);
        controlPanel.Add(presetDropdown);
        
        newButton = new Button(() => CreateNewCurve()) { text = "New" };
        newButton.style.width = 60;
        newButton.style.marginLeft = 10;
        controlPanel.Add(newButton);
        
        saveButton = new Button(() => SaveCurrentCurve()) { text = "Save" };
        saveButton.style.width = 60;
        saveButton.style.marginLeft = 10;
        controlPanel.Add(saveButton);
        
        showControlsToggle = new Toggle("Show Controls");
        showControlsToggle.value = true;
        showControlsToggle.style.marginLeft = 20;
        showControlsToggle.RegisterValueChangedCallback(OnShowControlsChanged);
        controlPanel.Add(showControlsToggle);
        
        var rangeLabel = new Label("Range:");
        rangeLabel.style.marginLeft = 20;
        controlPanel.Add(rangeLabel);
        
        rangeXField = new FloatField("X:");
        rangeXField.value = 1f;
        rangeXField.style.width = 80;
        rangeXField.style.marginLeft = 10;
        rangeXField.RegisterValueChangedCallback(OnRangeChanged);
        controlPanel.Add(rangeXField);
        
        rangeYField = new FloatField("Y:");
        rangeYField.value = 1f;
        rangeYField.style.width = 80;
        rangeYField.style.marginLeft = 10;
        rangeYField.RegisterValueChangedCallback(OnRangeChanged);
        controlPanel.Add(rangeYField);
        
        parent.Add(controlPanel);
    }

    private void SetupCurveDisplay(VisualElement parent)
    {
        curveVisualElement = new CurveVisualElement();
        curveVisualElement.style.alignSelf = Align.Center;
        curveVisualElement.style.marginTop = 20;
        curveVisualElement.style.marginBottom = 20;
        curveVisualElement.style.flexGrow = 1; // Take up remaining space
        curveVisualElement.OnPathChanged += OnPathChanged;
        
        parent.Add(curveVisualElement);
    }

    private void LoadAvailablePresets()
    {
        var presetNames = new List<string> { "None" };
        
        foreach (var preset in availablePresets)
        {
            if (preset != null)
            {
                presetNames.Add(preset.name);
            }
        }
        
        presetDropdown.choices = presetNames;
    }

    private void OnPresetSelected(ChangeEvent<string> evt)
    {
        if (evt.newValue == "None")
        {
            return;
        }
        
        var selectedPreset = availablePresets.Find(p => p != null && p.name == evt.newValue);
        if (selectedPreset != null)
        {
            CurrentCurve = selectedPreset.GetCurveCopy();
        }
    }

    private void CreateNewCurve()
    {
        CurrentCurve = AnimationCurve.Linear(0, 0, 1, 1);
        presetDropdown.value = "None";
    }

    private void SaveCurrentCurve()
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
            "Save Animation Curve Preset",
            "NewCurvePreset",
            "asset",
            "Save curve as preset");
        
        if (!string.IsNullOrEmpty(path))
        {
            var preset = ScriptableObject.CreateInstance<AnimationCurvePreset>();
            preset.SetCurve(currentCurve);
            preset.Description = "Runtime created curve";
            
            UnityEditor.AssetDatabase.CreateAsset(preset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            availablePresets.Add(preset);
            LoadAvailablePresets();
            
            Debug.Log($"Curve preset saved to {path}");
        }
#else
        Debug.Log("Saving curves at runtime requires custom implementation");
#endif
    }

    private void OnShowControlsChanged(ChangeEvent<bool> evt)
    {
        curveVisualElement.ShowControlPoints = evt.newValue;
    }

    private void OnRangeChanged(ChangeEvent<float> evt)
    {
        Vector2 newRange = new Vector2(rangeXField.value, rangeYField.value);
        curveVisualElement.CurveRange = newRange;
    }

    private void OnPathChanged(Path path)
    {
        // Only update the curve if we're not currently dragging to avoid feedback loops
        if (curveVisualElement == null || !curveVisualElement.IsDragging)
        {
            // TEMPORARY SOLUTION: Skip the conversion entirely to prevent control point corruption
            // Just update the internal curve reference without triggering display update
            Debug.Log("Skipping Path->Curve->Path conversion to prevent control point corruption");
            currentCurve = AnimationCurveAdapter.PathToAnimationCurve(path);
            OnCurveChanged?.Invoke(currentCurve);
            
            // DO NOT call UpdateCurveDisplay() here to avoid the roundtrip conversion
            // The visual is already correct from the user's drag operation
        }
    }

    private void UpdateCurveDisplay()
    {
        if (curveVisualElement != null)
        {
            Debug.Log("UpdateCurveDisplay: Converting curve back to path");
            Path path = AnimationCurveAdapter.AnimationCurveToPath(currentCurve);
            
            Debug.Log("Points after conversion back to path:");
            for (int i = 0; i < path.NumPoints; i++)
            {
                Debug.Log($"  Point {i}: {path[i]}");
            }
            
            curveVisualElement.CurrentPath = path;
        }
    }

    public void SetCurve(AnimationCurve curve)
    {
        CurrentCurve = curve;
    }

    public void LoadPreset(AnimationCurvePreset preset)
    {
        if (preset != null)
        {
            CurrentCurve = preset.GetCurveCopy();
            
            if (availablePresets.Contains(preset))
            {
                presetDropdown.value = preset.name;
            }
        }
    }
}