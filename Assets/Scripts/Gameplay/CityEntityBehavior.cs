using UnityEngine;

/// <summary>
/// Simple movement script for bustling city entities (vehicles, pedestrians).
/// Moves the entity forward along its local Z axis.
/// Vehicles loop back to start position when exceeding travel distance.
/// Pedestrians rotate 180 degrees and walk back.
/// </summary>
public class CityEntityBehavior : MonoBehaviour
{
    public enum EntityType { Vehicle, Pedestrian }

    [Header("Settings")]
    public EntityType type = EntityType.Vehicle;
    public float speed = 5f;
    public float travelDistance = 100f; // Distance from start before looping/turning

    private Vector3 startPosition;
    private float distanceTraveled = 0f;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float frameMove = speed * Time.deltaTime;
        transform.Translate(Vector3.forward * frameMove, Space.Self);
        distanceTraveled += frameMove;

        if (distanceTraveled >= travelDistance)
        {
            if (type == EntityType.Vehicle)
            {
                // Loop vehicle back to start
                transform.position = startPosition;
                distanceTraveled = 0f;
            }
            else if (type == EntityType.Pedestrian)
            {
                // Pedestrian turns around 180 degrees
                transform.Rotate(0, 180f, 0);
                distanceTraveled = 0f;
                // Update start position to current so it measures distance back accurately
                startPosition = transform.position;
            }
        }
    }
}
