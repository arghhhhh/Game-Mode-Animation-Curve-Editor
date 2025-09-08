using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class CurveVisualElement : VisualElement
{
    private Path currentPath;
    private Vector2 viewportSize = new Vector2(400, 200);
    private Vector2 curveRange = new Vector2(1, 1);
    private Color curveColor = Color.white;
    private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    private Color anchorColor = Color.cyan;
    private Color controlColor = Color.yellow;
    private float anchorRadius = 6f;
    private float controlRadius = 4f;
    private bool showControlPoints = true;
    private bool showGrid = true;
    
    public System.Action<Path> OnPathChanged;
    
    private int selectedPointIndex = -1;
    private bool isDragging = false;
    private Vector2 dragOffset;
    private Vector2 drawingOffset = new Vector2(100f, 100f);

    public Path CurrentPath
    {
        get { return currentPath; }
        set 
        { 
            currentPath = value;
            MarkDirtyRepaint();
        }
    }

    public Vector2 CurveRange
    {
        get { return curveRange; }
        set 
        { 
            curveRange = value;
            MarkDirtyRepaint();
        }
    }

    public bool ShowControlPoints
    {
        get { return showControlPoints; }
        set 
        { 
            showControlPoints = value;
            MarkDirtyRepaint();
        }
    }

    public bool IsDragging
    {
        get { return isDragging; }
    }

    public CurveVisualElement()
    {
        // Expand the interactive area beyond the visual area to catch off-screen clicks
        float expandedWidth = viewportSize.x + 200f; // 100px padding on each side
        float expandedHeight = viewportSize.y + 200f; // 100px padding on each side
        
        style.width = expandedWidth;
        style.height = expandedHeight;
        style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        
        generateVisualContent += OnGenerateVisualContent;
        RegisterCallback<MouseDownEvent>(OnMouseDown);
        RegisterCallback<MouseMoveEvent>(OnMouseMove);
        RegisterCallback<MouseUpEvent>(OnMouseUp);
        
        focusable = true;
        
        currentPath = new Path(Vector2.one * 0.5f);
        currentPath.AutoSetControlPoints = false;
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (currentPath == null) return;
        
        var painter2D = mgc.painter2D;
        
        DrawGrid(painter2D);
        DrawCurve(painter2D);
        
        if (showControlPoints)
        {
            DrawControlLines(painter2D);
            DrawPoints(painter2D);
        }
    }

    private void DrawGrid(Painter2D painter2D)
    {
        if (!showGrid) return;
        
        painter2D.strokeColor = gridColor;
        painter2D.lineWidth = 1f;
        
        int gridLines = 10;
        for (int i = 0; i <= gridLines; i++)
        {
            float t = (float)i / gridLines;
            float x = t * viewportSize.x + drawingOffset.x;
            float y = t * viewportSize.y + drawingOffset.y;
            
            painter2D.BeginPath();
            painter2D.MoveTo(new Vector2(x, drawingOffset.y));
            painter2D.LineTo(new Vector2(x, viewportSize.y + drawingOffset.y));
            painter2D.Stroke();
            
            painter2D.BeginPath();
            painter2D.MoveTo(new Vector2(drawingOffset.x, y));
            painter2D.LineTo(new Vector2(viewportSize.x + drawingOffset.x, y));
            painter2D.Stroke();
        }
    }

    private void DrawCurve(Painter2D painter2D)
    {
        painter2D.strokeColor = curveColor;
        painter2D.lineWidth = 2f;
        
        for (int segmentIndex = 0; segmentIndex < currentPath.NumSegments; segmentIndex++)
        {
            Vector2[] points = currentPath.GetPointsInSegment(segmentIndex);
            
            Vector2 p0 = PathToScreen(points[0]);
            Vector2 p1 = PathToScreen(points[1]);
            Vector2 p2 = PathToScreen(points[2]);
            Vector2 p3 = PathToScreen(points[3]);
            
            painter2D.BeginPath();
            painter2D.MoveTo(p0);
            
            int resolution = 20;
            for (int i = 1; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                Vector2 pointOnCurve = Bezier.EvaluateCubic(points[0], points[1], points[2], points[3], t);
                Vector2 screenPoint = PathToScreen(pointOnCurve);
                painter2D.LineTo(screenPoint);
            }
            
            painter2D.Stroke();
        }
    }

    private void DrawControlLines(Painter2D painter2D)
    {
        painter2D.strokeColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        painter2D.lineWidth = 1f;
        
        for (int segmentIndex = 0; segmentIndex < currentPath.NumSegments; segmentIndex++)
        {
            Vector2[] points = currentPath.GetPointsInSegment(segmentIndex);
            
            Vector2 p0 = PathToScreen(points[0]);
            Vector2 p1 = PathToScreen(points[1]);
            Vector2 p2 = PathToScreen(points[2]);
            Vector2 p3 = PathToScreen(points[3]);
            
            painter2D.BeginPath();
            painter2D.MoveTo(p0);
            painter2D.LineTo(p1);
            painter2D.Stroke();
            
            painter2D.BeginPath();
            painter2D.MoveTo(p2);
            painter2D.LineTo(p3);
            painter2D.Stroke();
        }
    }

    private void DrawPoints(Painter2D painter2D)
    {
        for (int i = 0; i < currentPath.NumPoints; i++)
        {
            Vector2 screenPos = PathToScreen(currentPath[i]);
            bool isAnchor = i % 3 == 0;
            
            painter2D.fillColor = isAnchor ? anchorColor : controlColor;
            
            if (selectedPointIndex == i)
            {
                painter2D.fillColor = Color.white;
            }
            
            float radius = isAnchor ? anchorRadius : controlRadius;
            
            painter2D.BeginPath();
            painter2D.Arc(screenPos, radius, 0, 360);
            painter2D.Fill();
        }
    }

    private Vector2 PathToScreen(Vector2 pathPoint)
    {
        float x = (pathPoint.x / curveRange.x) * viewportSize.x;
        float y = viewportSize.y - (pathPoint.y / curveRange.y) * viewportSize.y;
        return new Vector2(x, y) + drawingOffset;
    }

    private Vector2 ScreenToPath(Vector2 screenPoint)
    {
        // Remove the drawing offset first
        Vector2 adjustedScreen = screenPoint - drawingOffset;
        float x = (adjustedScreen.x / viewportSize.x) * curveRange.x;
        float y = ((viewportSize.y - adjustedScreen.y) / viewportSize.y) * curveRange.y;
        return new Vector2(x, y);
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        Vector2 localMousePos = this.WorldToLocal(evt.mousePosition);
        
        selectedPointIndex = GetNearestPointIndex(localMousePos);
        
        Debug.Log($"Mouse click at: {localMousePos}, Found point index: {selectedPointIndex}");
        
        // Debug all points in the path
        Debug.Log($"Current path has {currentPath.NumPoints} points:");
        for (int i = 0; i < currentPath.NumPoints; i++)
        {
            bool isAnchor = i % 3 == 0;
            Debug.Log($"  Point {i} ({(isAnchor ? "ANCHOR" : "CONTROL")}): {currentPath[i]}");
        }
        
        if (selectedPointIndex != -1)
        {
            Vector2 pointScreenPos = PathToScreen(currentPath[selectedPointIndex]);
            Debug.Log($"Selected point {selectedPointIndex} at path pos: {currentPath[selectedPointIndex]}, screen pos: {pointScreenPos}");
            dragOffset = localMousePos - pointScreenPos;
            isDragging = true;
            this.CaptureMouse();
        }
        else
        {
            Debug.Log("No point found to select");
        }
        
        MarkDirtyRepaint();
        evt.StopPropagation();
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (isDragging && selectedPointIndex != -1)
        {
            Vector2 localMousePos = this.WorldToLocal(evt.mousePosition);
            Vector2 newPathPos = ScreenToPath(localMousePos - dragOffset);
            
            Debug.Log($"Moving point {selectedPointIndex}: oldPos={currentPath[selectedPointIndex]}, newPos={newPathPos}");
            
            currentPath.MovePoint(selectedPointIndex, newPathPos);
            
            Debug.Log($"After move: point {selectedPointIndex} is now at {currentPath[selectedPointIndex]}");
            
            // Don't invoke OnPathChanged during dragging to avoid feedback loops
            MarkDirtyRepaint();
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (isDragging)
        {
            Debug.Log($"Drag ended for point {selectedPointIndex}, final position: {currentPath[selectedPointIndex]}");
            
            isDragging = false;
            this.ReleaseMouse();
            
            // Debug all points before conversion
            Debug.Log("Points before OnPathChanged:");
            for (int i = 0; i < currentPath.NumPoints; i++)
            {
                Debug.Log($"  Point {i}: {currentPath[i]}");
            }
            
            // Now that dragging is done, update the curve
            OnPathChanged?.Invoke(currentPath);
        }
    }

    private int GetNearestPointIndex(Vector2 screenPos)
    {
        int nearestIndex = -1;
        float minDistance = float.MaxValue;
        
        for (int i = 0; i < currentPath.NumPoints; i++)
        {
            Vector2 pointScreenPos = PathToScreen(currentPath[i]);
            float distance = Vector2.Distance(screenPos, pointScreenPos);
            
            bool isAnchor = i % 3 == 0;
            float threshold = isAnchor ? anchorRadius : controlRadius;
            
            // For off-screen points, use a much larger threshold and also check if we're clicking near the viewport edge
            bool isOffScreen = pointScreenPos.x < -50 || pointScreenPos.x > viewportSize.x + 50 || 
                              pointScreenPos.y < -50 || pointScreenPos.y > viewportSize.y + 50;
            
            if (isOffScreen)
            {
                // For off-screen points, use a very large threshold and check if clicking near viewport edges
                threshold = 50f; // Much larger threshold
                
                // Also check if we're clicking near the edge where the off-screen point would be
                Vector2 clampedPos = new Vector2(
                    Mathf.Clamp(pointScreenPos.x, 0, viewportSize.x),
                    Mathf.Clamp(pointScreenPos.y, 0, viewportSize.y)
                );
                float edgeDistance = Vector2.Distance(screenPos, clampedPos);
                distance = Mathf.Min(distance, edgeDistance);
            }
            
            if (distance < threshold && distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }
        
        return nearestIndex;
    }
}