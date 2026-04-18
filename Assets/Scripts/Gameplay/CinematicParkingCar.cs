using UnityEngine;

/// <summary>
/// A specialized script that smoothly pilots a cinematic 'hero' vehicle 
/// through an array of waypoints to perform a dynamic parking sequence.
/// </summary>
public class CinematicParkingCar : MonoBehaviour
{
    [Header("Path Settings")]
    public Vector3[] waypoints;
    
    [Header("Movement")]
    public float speed = 10f;
    public float turnSpeed = 120f;
    public float parkingDecelerationDistance = 5f;

    private int currentWP = 0;
    private float baseSpeed;

    private void Start()
    {
        baseSpeed = speed;
    }

    private void Update()
    {
        if (waypoints == null || currentWP >= waypoints.Length) return;

        Vector3 target = waypoints[currentWP];
        Vector3 dir = target - transform.position;
        dir.y = 0;

        float dist = dir.magnitude;

        if (dist > 0.1f)
        {
            // Slow down automatically as it approaches the final parking spot
            if (currentWP == waypoints.Length - 1 && dist < parkingDecelerationDistance)
            {
                speed = Mathf.Lerp(baseSpeed * 0.1f, baseSpeed, dist / parkingDecelerationDistance);
            }

            // Move
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            // Rotate smoothly towards the target waypoint
            Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        }
        else
        {
            // Reached waypoint
            currentWP++;
        }
    }
}
