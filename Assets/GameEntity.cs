using UnityEngine;

public class GameEntity : MonoBehaviour
{
    public LayerMask GroundMask;
    public float spd;
    public float movementAngle;
    public Vector3 airspd;
    public bool grounded;
    public float radius;
    public float height;
    public Transform model;
    public Vector3 currentRotation;
    public float incline;
    public float roff; // Rotational offset

    public virtual void CheckGrounded()
    {
        RaycastHit hit;
        // Cast a ray downward from the midpoint of the character
        Vector3 raycastOrigin = transform.position + (transform.up * height);
        Debug.DrawRay(raycastOrigin, -transform.up * height, Color.red);
        if (Physics.Raycast(raycastOrigin, -transform.up, out hit, height, GroundMask))
        {
            grounded = true;

            // Calculate the difference in height
            float heightDifference = height - hit.distance;

            // Adjust the position of the character to be exactly on the ground
            transform.position = hit.point - (transform.up * 0.05f);

            // Calculate slope angles based on the surface normal
            Vector3 slopeNormal = hit.normal;
            // Project the forward direction onto the plane defined by the slope normal
            Vector3 forwardDirection = Vector3.ProjectOnPlane(Vector3.forward, slopeNormal).normalized;

            // Rotate the forward direction based on the movement angle
            forwardDirection = Quaternion.Euler(0f, movementAngle + roff, 0f) * forwardDirection;

            // Calculate the movement direction based on the slope-normal adjusted forward direction
            Vector3 movementDirection = Quaternion.FromToRotation(Vector3.up, slopeNormal) * (Quaternion.Euler(0f, movementAngle, 0f) * Vector3.forward);

            // Calculate the incline relative to the character's facing direction
            incline = Vector3.Angle(model.forward, new Vector3(0f, movementAngle, 0f)) - 90;

            // Project the movement direction onto the plane defined by the slope normal
            movementDirection = Vector3.ProjectOnPlane(movementDirection, slopeNormal).normalized;

            // Apply the movement
            airspd = movementDirection * spd;

            // Align the model with the slope normal
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, slopeNormal);
            transform.rotation = targetRotation;

            // Store the current rotation for later use
            currentRotation = slopeNormal;
        }
        else
        {
            grounded = false;
        }
        if (model != null && grounded)
        {
            model.transform.localRotation = Quaternion.Euler(0f, movementAngle, 0f);
        }
    }
    public virtual void CheckWalls()
    {
        RaycastHit hit;
        Vector3 Horizontal = transform.forward + transform.right;
        Vector3 raycastOrigin = transform.position + (transform.up * height);
        Vector3 rayDir = Quaternion.LookRotation(MultiplyVec3(airspd, Horizontal)).eulerAngles * radius;
        if (!grounded)
        {
            rayDir = new Vector3(rayDir.x, 0, rayDir.z);
        }
        if (Physics.Raycast(raycastOrigin, airspd, out hit, radius))
        {
            RaycastHit hit2;
            if (Physics.Raycast(raycastOrigin, -hit.normal, out hit2, radius, GroundMask))
            {
                transform.position = hit2.point + MultiplyVec3(hit2.normal * radius, Horizontal) - (transform.up * height);
                airspd = airspd.y * transform.up;
                spd = 0;
            }

        }
    }

    public virtual void CheckCeil()
    {
        RaycastHit hit;

        // Cast a ray downward from the midpoint of the character
        Vector3 raycastOrigin = transform.position + (transform.up * height);
        Debug.DrawRay(raycastOrigin, transform.up * height, Color.red);
        if (Physics.Raycast(raycastOrigin, transform.up, out hit, height, GroundMask))
        {
            transform.position = hit.point - (transform.up * (height*2));
            airspd.y = -5;
        }
    }
    Vector3 MultiplyVec3(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }
}
