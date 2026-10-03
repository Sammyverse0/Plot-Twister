using UnityEngine;

public enum PipeShape { Straight, Elbow }
public class Plot : MonoBehaviour
{
    public PipeShape shape;
    [HideInInspector] public int rotationState;
    [HideInInspector] public PuzzleManager manager; 
    [SerializeField] private float worldYawOffset = 0f;

    public void Twist()
    {
        rotationState = (rotationState + 1) % 4;
        ApplyVisualRotation();
        manager?.OnPlotTwisted();
    }

    
    public void SetInitialRotation(int state)
    {
        rotationState = state;
        ApplyVisualRotation();
    }

    private void ApplyVisualRotation()
    {
        transform.localRotation = Quaternion.Euler(0f, rotationState * 90f + worldYawOffset, 0f);
    }
}