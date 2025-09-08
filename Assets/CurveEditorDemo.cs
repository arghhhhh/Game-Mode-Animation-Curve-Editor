using UnityEngine;

public class CurveEditorDemo : MonoBehaviour
{
    [SerializeField] private RuntimeCurveEditor curveEditor;
    [SerializeField] private Transform testObject;
    [SerializeField] private float animationDuration = 2f;
    [SerializeField] private bool playAnimation = false;
    
    private float animationTime = 0f;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    
    void Start()
    {
        if (curveEditor == null)
        {
            curveEditor = FindFirstObjectByType<RuntimeCurveEditor>();
        }
        
        if (testObject == null && curveEditor != null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Test Object";
            cube.transform.position = new Vector3(-3, 0, 0);
            testObject = cube.transform;
        }
        
        if (testObject != null)
        {
            startPosition = testObject.position;
            targetPosition = startPosition + Vector3.right * 6f;
        }
        
        if (curveEditor != null)
        {
            curveEditor.OnCurveChanged += OnCurveChanged;
        }
    }
    
    void Update()
    {
        if (playAnimation && testObject != null && curveEditor != null)
        {
            animationTime += Time.deltaTime;
            float normalizedTime = (animationTime % animationDuration) / animationDuration;
            
            float curveValue = curveEditor.CurrentCurve.Evaluate(normalizedTime);
            
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, normalizedTime);
            position.y = startPosition.y + curveValue * 3f;
            
            testObject.position = position;
        }
    }
    
    private void OnCurveChanged(AnimationCurve curve)
    {
        Debug.Log($"Curve changed! New curve has {curve.length} keyframes");
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        
        GUILayout.Label("Curve Editor Demo");
        
        if (GUILayout.Button(playAnimation ? "Stop Animation" : "Play Animation"))
        {
            playAnimation = !playAnimation;
            animationTime = 0f;
        }
        
        if (GUILayout.Button("Reset Object Position") && testObject != null)
        {
            testObject.position = startPosition;
            animationTime = 0f;
        }
        
        GUILayout.EndArea();
    }
}