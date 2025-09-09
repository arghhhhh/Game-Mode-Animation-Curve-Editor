using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class CurveVisualElement : VisualElement
{
    private Path currentPath;
    private AnimationCurve cachedAnimationCurve;
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
    
    // Conversion state management
    private bool isConverting = false;
    private Color greyedOutColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    private Color greyedOutAnchorColor = new Color(0.4f, 0.6f, 0.6f, 0.5f);
    private Color greyedOutControlColor = new Color(0.6f, 0.6f, 0.4f, 0.5f);
    
    public System.Action<Path> OnPathChanged;
    
    private int selectedPointIndex = -1;
    private int hoveredPointIndex = -1;
    private bool isHoveringCurve = false;
    private Vector2 hoveredCurvePosition = Vector2.zero;
    private bool isDragging = false;
    private Vector2 dragOffset;
    private Vector2 drawingOffset = new Vector2(100f, 100f);

    public Path CurrentPath
    {
        get { return currentPath; }
        set 
        { 
            currentPath = value;
            StartConversion();
        }
    }
    
    public bool IsConverting
    {
        get { return isConverting; }
    }
    
    private void StartConversion()
    {
        isConverting = true;
        MarkDirtyRepaint();
        
        // Use Unity's main thread scheduler to perform conversion
        schedule.Execute(() => {
            // Perform the conversion
            cachedAnimationCurve = AnimationCurveAdapter.PathToAnimationCurve(currentPath);
            
            // Mark conversion as complete
            isConverting = false;
            MarkDirtyRepaint();
        });
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
        RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        
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
        // Use greyed-out color during conversion
        painter2D.strokeColor = isConverting ? greyedOutColor : curveColor;
        painter2D.lineWidth = 2f;
        
        if (isDragging)
        {
            // During dragging, use direct Bézier rendering for immediate feedback
            DrawCurveBezier(painter2D);
        }
        else if (isConverting)
        {
            // During conversion, show Bézier version in grey
            DrawCurveBezier(painter2D);
        }
        else
        {
            // When not dragging and not converting, use AnimationCurve evaluation for accuracy
            DrawCurveAnimationCurve(painter2D);
        }
    }

    private void DrawCurveAnimationCurve(Painter2D painter2D)
    {
        // Use cached AnimationCurve for accurate evaluation
        AnimationCurve animCurve = cachedAnimationCurve;
        
        if (animCurve == null || animCurve.length < 2)
            return;
        
        painter2D.BeginPath();
        
        // Get the time range from the AnimationCurve
        float minTime = animCurve.keys[0].time;
        float maxTime = animCurve.keys[animCurve.length - 1].time;
        
        int resolution = 50; // Higher resolution for smooth curves
        bool firstPoint = true;
        
        for (int i = 0; i <= resolution; i++)
        {
            float t = minTime + (maxTime - minTime) * (i / (float)resolution);
            float value = animCurve.Evaluate(t);
            
            // Create path point from time and evaluated value
            Vector2 pathPoint = new Vector2(t, value);
            Vector2 screenPoint = PathToScreen(pathPoint);
            
            if (firstPoint)
            {
                painter2D.MoveTo(screenPoint);
                firstPoint = false;
            }
            else
            {
                painter2D.LineTo(screenPoint);
            }
        }
        
        painter2D.Stroke();
    }

    private void DrawCurveBezier(Painter2D painter2D)
    {
        // Original Bézier rendering for immediate feedback during dragging
        for (int segmentIndex = 0; segmentIndex < currentPath.NumSegments; segmentIndex++)
        {
            Vector2[] points = currentPath.GetPointsInSegment(segmentIndex);
            
            painter2D.BeginPath();
            Vector2 p0 = PathToScreen(points[0]);
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
            
            // Use greyed-out colors during conversion
            if (isConverting)
            {
                painter2D.fillColor = isAnchor ? greyedOutAnchorColor : greyedOutControlColor;
            }
            else
            {
                painter2D.fillColor = isAnchor ? anchorColor : controlColor;
                
                // Highlight selected point
                if (selectedPointIndex == i)
                {
                    painter2D.fillColor = Color.white;
                }
                // Highlight hovered point
                else if (hoveredPointIndex == i)
                {
                    painter2D.fillColor = isAnchor ? Color.yellow : new Color(1f, 1f, 0.5f);
                }
            }
            
            float radius = isAnchor ? anchorRadius : controlRadius;
            
            // Make hovered points slightly larger
            if (hoveredPointIndex == i && !isConverting)
            {
                radius *= 1.3f;
            }
            
            painter2D.BeginPath();
            painter2D.Arc(screenPos, radius, 0, 360);
            painter2D.Fill();
        }
        
        // Draw hover indicator for curve (shift+click preview)
        if (isHoveringCurve && !isConverting)
        {
            Vector2 screenPos = PathToScreen(hoveredCurvePosition);
            painter2D.fillColor = new Color(0.5f, 0.8f, 1f, 0.8f); // Light blue
            painter2D.BeginPath();
            painter2D.Arc(screenPos, anchorRadius * 0.8f, 0, 360);
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
        // Block all interaction during conversion
        if (isConverting)
        {
            evt.StopPropagation();
            return;
        }
        
        Vector2 localMousePos = this.WorldToLocal(evt.mousePosition);
        
        // Handle right-click for deletion
        if (evt.button == 1) // Right mouse button
        {
            HandleRightClick(localMousePos);
            evt.StopPropagation();
            return;
        }
        
        // Handle shift+click for adding points
        if (evt.shiftKey && evt.button == 0) // Shift + left click
        {
            HandleShiftClick(localMousePos);
            evt.StopPropagation();
            return;
        }
        
        // Normal left-click behavior
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
        // Block all interaction during conversion
        if (isConverting)
            return;
            
        Vector2 localMousePos = this.WorldToLocal(evt.mousePosition);
            
        if (isDragging && selectedPointIndex != -1)
        {
            Vector2 newPathPos = ScreenToPath(localMousePos - dragOffset);
            
            Debug.Log($"Moving point {selectedPointIndex}: oldPos={currentPath[selectedPointIndex]}, newPos={newPathPos}");
            
            currentPath.MovePoint(selectedPointIndex, newPathPos);
            
            Debug.Log($"After move: point {selectedPointIndex} is now at {currentPath[selectedPointIndex]}");
            
            // Don't invoke OnPathChanged during dragging to avoid feedback loops
            MarkDirtyRepaint();
        }
        else
        {
            // Update hover state when not dragging
            UpdateHoverState(localMousePos);
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        // Block all interaction during conversion
        if (isConverting)
            return;
            
        if (isDragging)
        {
            Debug.Log($"Drag ended for point {selectedPointIndex}, final position: {currentPath[selectedPointIndex]}");
            
            isDragging = false;
            this.ReleaseMouse();
            
            // Start conversion process after drag ends
            StartConversion();
            
            // Debug all points before conversion
            Debug.Log("Points before OnPathChanged:");
            for (int i = 0; i < currentPath.NumPoints; i++)
            {
                Debug.Log($"  Point {i}: {currentPath[i]}");
            }
            
            // Now that dragging is done, update the curve
            // Pass information about which point was modified
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
            // Increased thresholds for better usability
            float threshold = isAnchor ? anchorRadius * 2f : controlRadius * 2f;
            
            // For off-screen points, use a much larger threshold and also check if we're clicking near the viewport edge
            bool isOffScreen = pointScreenPos.x < -50 || pointScreenPos.x > viewportSize.x + 50 || 
                              pointScreenPos.y < -50 || pointScreenPos.y > viewportSize.y + 50;
            
            if (isOffScreen)
            {
                // For off-screen points, use a very large threshold and check if clicking near viewport edges
                threshold = 70f; // Increased from 50f
                
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
    
    private int GetNearestPointIndexWithValidation(Vector2 screenPos)
    {
        int nearestIndex = -1;
        float minDistance = float.MaxValue;
        
        for (int i = 0; i < currentPath.NumPoints; i++)
        {
            Vector2 pointScreenPos = PathToScreen(currentPath[i]);
            float distance = Vector2.Distance(screenPos, pointScreenPos);
            
            bool isAnchor = i % 3 == 0;
            // Use smaller hover thresholds than click thresholds for more precise hover
            float hoverThreshold = isAnchor ? anchorRadius * 1.5f : controlRadius * 1.5f;
            
            // For off-screen points, use a smaller threshold for hover than for clicking
            bool isOffScreen = pointScreenPos.x < -20 || pointScreenPos.x > viewportSize.x + 20 || 
                              pointScreenPos.y < -20 || pointScreenPos.y > viewportSize.y + 20;
            
            if (isOffScreen)
            {
                // Much smaller hover threshold for off-screen points
                hoverThreshold = 30f;
                
                // Check if we're near the edge where the off-screen point would be
                Vector2 clampedPos = new Vector2(
                    Mathf.Clamp(pointScreenPos.x, drawingOffset.x, viewportSize.x + drawingOffset.x),
                    Mathf.Clamp(pointScreenPos.y, drawingOffset.y, viewportSize.y + drawingOffset.y)
                );
                float edgeDistance = Vector2.Distance(screenPos, clampedPos);
                distance = Mathf.Min(distance, edgeDistance);
            }
            
            // Additional validation: make sure we're actually reasonably close
            if (distance < hoverThreshold && distance < minDistance)
            {
                // Extra check: don't hover if we're more than 50 pixels away (regardless of threshold)
                if (distance <= 50f)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }
        }
        
        return nearestIndex;
    }
    
    private void UpdateHoverState(Vector2 screenPos)
    {
        bool needsRepaint = false;
        
        // Check for point hover with more strict validation
        int newHoveredPoint = GetNearestPointIndexWithValidation(screenPos);
        if (newHoveredPoint != hoveredPointIndex)
        {
            hoveredPointIndex = newHoveredPoint;
            needsRepaint = true;
        }
        
        // Check for curve hover (for shift+click preview)
        bool newHoveringCurve = false;
        Vector2 newHoveredCurvePosition = Vector2.zero;
        
        if (hoveredPointIndex == -1) // Only show curve hover when not hovering a point
        {
            Vector2 pathPos = ScreenToPath(screenPos);
            
            // Use the same AnimationCurve evaluation as the visual display for accuracy
            if (cachedAnimationCurve != null && cachedAnimationCurve.length >= 2)
            {
                float minTime = cachedAnimationCurve.keys[0].time;
                float maxTime = cachedAnimationCurve.keys[cachedAnimationCurve.length - 1].time;
                
                // Find the closest point on the actual displayed curve
                float closestDistance = float.MaxValue;
                int samples = 50; // Higher resolution for better accuracy
                
                for (int i = 0; i <= samples; i++)
                {
                    float t = minTime + (maxTime - minTime) * (i / (float)samples);
                    float value = cachedAnimationCurve.Evaluate(t);
                    Vector2 pointOnCurve = new Vector2(t, value);
                    
                    float distance = Vector2.Distance(pathPos, pointOnCurve);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        newHoveredCurvePosition = pointOnCurve;
                    }
                }
                
                // Show curve hover if close enough
                float curveHoverThreshold = 0.05f; // Smaller threshold for curve
                if (closestDistance <= curveHoverThreshold)
                {
                    newHoveringCurve = true;
                }
            }
            else
            {
                // Fallback to Bézier evaluation if no cached curve (shouldn't happen in normal use)
                Vector2 pathPos2 = ScreenToPath(screenPos);
                
                float closestDistance = float.MaxValue;
                for (int segmentIndex = 0; segmentIndex < currentPath.NumSegments; segmentIndex++)
                {
                    Vector2[] segmentPoints = currentPath.GetPointsInSegment(segmentIndex);
                    
                    int samples = 20;
                    for (int i = 0; i <= samples; i++)
                    {
                        float t = (float)i / samples;
                        Vector2 pointOnCurve = Bezier.EvaluateCubic(segmentPoints[0], segmentPoints[1], segmentPoints[2], segmentPoints[3], t);
                        
                        float distance = Vector2.Distance(pathPos2, pointOnCurve);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            newHoveredCurvePosition = pointOnCurve;
                        }
                    }
                }
                
                float curveHoverThreshold = 0.05f;
                if (closestDistance <= curveHoverThreshold)
                {
                    newHoveringCurve = true;
                }
            }
        }
        
        // Update curve hover state
        if (newHoveringCurve != isHoveringCurve || Vector2.Distance(newHoveredCurvePosition, hoveredCurvePosition) > 0.001f)
        {
            isHoveringCurve = newHoveringCurve;
            hoveredCurvePosition = newHoveredCurvePosition;
            needsRepaint = true;
        }
        
        if (needsRepaint)
        {
            MarkDirtyRepaint();
        }
    }
    
    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        // Clear all hover states when mouse leaves
        if (hoveredPointIndex != -1 || isHoveringCurve)
        {
            hoveredPointIndex = -1;
            isHoveringCurve = false;
            MarkDirtyRepaint();
        }
    }
    
    private void HandleShiftClick(Vector2 screenPos)
    {
        Debug.Log($"Shift+click at: {screenPos}");
        
        // Convert screen position to path coordinates
        Vector2 pathPos = ScreenToPath(screenPos);
        Debug.Log($"Path coordinates: {pathPos}");
        
        Vector2 closestPoint = Vector2.zero;
        float closestDistance = float.MaxValue;
        int closestSegment = 0;
        
        // Use the same AnimationCurve evaluation as the visual display
        if (cachedAnimationCurve != null && cachedAnimationCurve.length >= 2)
        {
            float minTime = cachedAnimationCurve.keys[0].time;
            float maxTime = cachedAnimationCurve.keys[cachedAnimationCurve.length - 1].time;
            
            // Find the closest point on the actual displayed curve
            int samples = 50;
            float bestT = 0;
            
            for (int i = 0; i <= samples; i++)
            {
                float t = minTime + (maxTime - minTime) * (i / (float)samples);
                float value = cachedAnimationCurve.Evaluate(t);
                Vector2 pointOnCurve = new Vector2(t, value);
                
                float distance = Vector2.Distance(pathPos, pointOnCurve);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = pointOnCurve;
                    bestT = t;
                }
            }
            
            // Find which segment this time falls into for proper splitting
            for (int segmentIndex = 0; segmentIndex < currentPath.NumSegments; segmentIndex++)
            {
                float segmentStartTime = segmentIndex == 0 ? minTime : cachedAnimationCurve.keys[segmentIndex].time;
                float segmentEndTime = cachedAnimationCurve.keys[segmentIndex + 1].time;
                
                if (bestT >= segmentStartTime && bestT <= segmentEndTime)
                {
                    closestSegment = segmentIndex;
                    break;
                }
            }
        }
        else
        {
            // Fallback to Bézier evaluation if no cached curve
            for (int segmentIndex = 0; segmentIndex < currentPath.NumSegments; segmentIndex++)
            {
                Vector2[] segmentPoints = currentPath.GetPointsInSegment(segmentIndex);
                
                int samples = 20;
                for (int i = 0; i <= samples; i++)
                {
                    float t = (float)i / samples;
                    Vector2 pointOnCurve = Bezier.EvaluateCubic(segmentPoints[0], segmentPoints[1], segmentPoints[2], segmentPoints[3], t);
                    
                    float distance = Vector2.Distance(pathPos, pointOnCurve);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestSegment = segmentIndex;
                        closestPoint = pointOnCurve;
                    }
                }
            }
        }
        
        Debug.Log($"Closest point on curve: {closestPoint} in segment {closestSegment}, distance: {closestDistance}");
        
        // Only add point if click is reasonably close to the curve
        float maxDistance = 0.1f; // Maximum distance in path coordinates
        if (closestDistance <= maxDistance)
        {
            // Split the segment at the closest point
            Debug.Log($"Adding new point at: {closestPoint}");
            currentPath.SplitSegment(closestPoint, closestSegment);
            
            // Trigger conversion and update
            StartConversion();
            OnPathChanged?.Invoke(currentPath);
            MarkDirtyRepaint();
        }
        else
        {
            Debug.Log($"Click too far from curve (distance: {closestDistance}), not adding point");
        }
    }
    
    private void HandleRightClick(Vector2 screenPos)
    {
        Debug.Log($"Right-click at: {screenPos}");
        
        // Find the nearest point (same as regular click detection)
        int pointIndex = GetNearestPointIndex(screenPos);
        
        if (pointIndex != -1)
        {
            bool isAnchor = pointIndex % 3 == 0;
            Debug.Log($"Right-clicked on point {pointIndex} ({(isAnchor ? "ANCHOR" : "CONTROL")})");
            
            if (isAnchor)
            {
                // Delete the segment containing this anchor
                // pointIndex is already the correct anchor index in the points array
                Debug.Log($"Deleting anchor at point index {pointIndex}");
                
                // Only delete if we have more than one segment (to prevent empty curves)
                if (currentPath.NumSegments > 1)
                {
                    currentPath.DeleteSegment(pointIndex);
                    
                    // Trigger conversion and update
                    StartConversion();
                    OnPathChanged?.Invoke(currentPath);
                    MarkDirtyRepaint();
                }
                else
                {
                    Debug.Log("Cannot delete last segment");
                }
            }
            else
            {
                Debug.Log("Cannot delete control points directly - delete their anchor instead");
            }
        }
        else
        {
            Debug.Log("Right-click did not hit any point");
        }
    }
}