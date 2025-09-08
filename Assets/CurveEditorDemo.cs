using UnityEngine;

public class CurveEditorDemo : MonoBehaviour
{
    [SerializeField] private RuntimeCurveEditor curveEditor;
    [SerializeField] private Transform testObject;
    [SerializeField] private Transform comparisonObject;
    [SerializeField] private float animationDuration = 2f;
    [SerializeField] private bool playAnimation = false;
    
    private float animationTime = 0f;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 comparisonStartPosition;
    private Vector3 comparisonTargetPosition;
    private AnimationCurve storedCurve;
    
    void Start()
    {
        if (curveEditor == null)
        {
            curveEditor = FindFirstObjectByType<RuntimeCurveEditor>();
        }
        
        if (testObject == null && curveEditor != null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Test Object (Runtime Curve)";
            cube.transform.position = new Vector3(-3, 0, 0);
            cube.GetComponent<Renderer>().material.color = Color.cyan;
            testObject = cube.transform;
        }
        
        if (comparisonObject == null && curveEditor != null)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Comparison Object (Stored Curve)";
            sphere.transform.position = new Vector3(-3, -2, 0);
            sphere.GetComponent<Renderer>().material.color = Color.yellow;
            comparisonObject = sphere.transform;
        }
        
        if (testObject != null)
        {
            startPosition = testObject.position;
            targetPosition = startPosition + Vector3.right * 6f;
        }
        
        if (comparisonObject != null)
        {
            comparisonStartPosition = comparisonObject.position;
            comparisonTargetPosition = comparisonStartPosition + Vector3.right * 6f;
        }
        
        if (curveEditor != null)
        {
            curveEditor.OnCurveChanged += OnCurveChanged;
            // Store the initial curve for comparison
            storedCurve = new AnimationCurve(curveEditor.CurrentCurve.keys);
        }
    }
    
    void Update()
    {
        if (playAnimation && testObject != null && curveEditor != null)
        {
            animationTime += Time.deltaTime;
            float normalizedTime = (animationTime % animationDuration) / animationDuration;
            
            // Animate the first object with the live runtime curve
            float runtimeCurveValue = curveEditor.CurrentCurve.Evaluate(normalizedTime);
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, normalizedTime);
            position.y = startPosition.y + runtimeCurveValue * 3f;
            testObject.position = position;
            
            // Animate the comparison object with the stored curve
            if (comparisonObject != null && storedCurve != null)
            {
                float storedCurveValue = storedCurve.Evaluate(normalizedTime);
                Vector3 comparisonPosition = Vector3.Lerp(comparisonStartPosition, comparisonTargetPosition, normalizedTime);
                comparisonPosition.y = comparisonStartPosition.y + storedCurveValue * 3f;
                comparisonObject.position = comparisonPosition;
            }
        }
    }
    
    private void OnCurveChanged(AnimationCurve curve)
    {
        Debug.Log($"Curve changed! New curve has {curve.length} keyframes");
        // Update the stored curve for comparison
        storedCurve = new AnimationCurve(curve.keys);
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 280, 140));
        
        GUILayout.Label("Curve Editor Demo");
        GUILayout.Label("Cyan Cube: Live Runtime Curve");
        GUILayout.Label("Yellow Sphere: Stored AnimationCurve");
        
        if (GUILayout.Button(playAnimation ? "Stop Animation" : "Play Animation"))
        {
            playAnimation = !playAnimation;
            animationTime = 0f;
        }
        
        if (GUILayout.Button("Reset Object Positions"))
        {
            if (testObject != null)
                testObject.position = startPosition;
            if (comparisonObject != null)
                comparisonObject.position = comparisonStartPosition;
            animationTime = 0f;
        }
        
        GUILayout.EndArea();
    }
}