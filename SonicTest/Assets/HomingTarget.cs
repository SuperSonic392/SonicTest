using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingTarget : MonoBehaviour
{
    public Vector3 offset;
    public float disableTimer;
    private void FixedUpdate()
    {
        disableTimer -= Time.fixedDeltaTime;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + offset, 1);
    }
}
