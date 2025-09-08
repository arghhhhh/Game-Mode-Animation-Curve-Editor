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
        
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.width = Length.Percent(100);
        container.style.height = Length.Percent(100);
        
        SetupControls(container);
        SetupCurveDisplay(container);
        
        root.Add(container);
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
        curveVisualElement.style.marginTop = 50; // Move further from top for better visibility
        curveVisualElement.style.marginBottom = 20;
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
            CurrentCurve = AnimationCurveAdapter.PathToAnimationCurve(path);
            
            // Debug the resulting AnimationCurve
            Debug.Log($"AnimationCurve after conversion has {CurrentCurve.length} keyframes:");
            for (int i = 0; i < CurrentCurve.length; i++)
            {
                var key = CurrentCurve[i];
                Debug.Log($"  Keyframe {i}: time={key.time}, value={key.value}, inTangent={key.inTangent}, outTangent={key.outTangent}");
            }
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