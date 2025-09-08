using UnityEngine;

/// <summary>
/// Quick setup script to add LogFileWriter to any existing GameObject.
/// Just add this to any GameObject in your scene and it will automatically
/// add the LogFileWriter component and remove itself.
/// </summary>
public class QuickLogSetup : MonoBehaviour
{
    void Start()
    {
        // Check if LogFileWriter already exists on this GameObject
        if (GetComponent<LogFileWriter>() == null)
        {
            // Add the LogFileWriter component
            LogFileWriter logWriter = gameObject.AddComponent<LogFileWriter>();
            
            Debug.Log($"LogFileWriter automatically added to {gameObject.name}!");
            Debug.Log("You can now remove the QuickLogSetup component as it's no longer needed.");
        }
        else
        {
            Debug.Log("LogFileWriter already exists on this GameObject.");
        }
        
        // Remove this component as it's only needed once
        Destroy(this);
    }
}
