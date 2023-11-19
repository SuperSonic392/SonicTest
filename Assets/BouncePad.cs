using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncePad : MonoBehaviour
{
    public Vector3 force;
    public float scriptingLock;
    private void OnTriggerEnter(Collider other)
    {
        HedgehogController obj = other.GetComponent<HedgehogController>();
        if (obj != null)
        {
            obj.transform.position = transform.position;
            obj.airspd = transform.up * force.y + (transform.right * force.x) + (transform.forward * force.z);
            obj.currentstate = HedgehogController.CharacterState.Sprung;
            obj.scriptLock = scriptingLock;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3 simPos = transform.position;
        Vector3 simVel = transform.up * force.y + (transform.right * force.x) + (transform.forward * force.z);
        int steps = 90;
        for (int i = 0; i < steps; i++)
        {
            if(simVel.y > 0)
            {
                simVel.y -= 35/ 2 * 0.01666667f;
            }
            else
            {
                simVel.y -= 35 * 0.01666667f;
            }
            Gizmos.DrawLine(simPos, simPos+ simVel * 0.01666667f);
            simPos += simVel * 0.01666667f;
        }
    }
    private void OnDrawGizmosSelected()
    {
        float script = scriptingLock;
        Gizmos.color = Color.green;
        Vector3 simPos = transform.position;
        Vector3 simVel = transform.up * force.y + (transform.right * force.x) + (transform.forward * force.z);
        int steps = 1500;
        for (int i = 0; i < steps; i++)
        {
            if(scriptingLock > 0)
            {
                script -= 0.01666667f;
                if (script > 0)
                {
                    Gizmos.color = Color.blue;
                }
                else
                {
                    break;
                }
            }
            if (simVel.y > 0)
            {
                simVel.y -= 35 / 2 * 0.01666667f;
            }
            else
            {
                simVel.y -= 35 * 0.01666667f;
            }
            Gizmos.DrawLine(simPos, simPos + simVel * 0.01666667f);
            simPos += simVel * 0.01666667f;
            HomingTarget target = FindHomingTarget(simPos);
            if(Vector3.Distance(simPos, target.transform.position + target.offset) < 8)
                Gizmos.DrawLine(simPos, target.transform.position + target.offset); //homing radius
        }
    }

    public HomingTarget FindHomingTarget(Vector3 position)
    {
        Transform closest = null;
        HomingTarget tar = null;
        Vector3 off = Vector3.zero;
        float closestDistance = Mathf.Infinity;

        foreach (HomingTarget target in FindObjectsOfType<HomingTarget>())
        {
            if (target.disableTimer <= 0)
            {
                float distance = Vector3.Distance(position, target.transform.position + target.offset);

                if (distance < closestDistance)
                {
                    tar = target;
                    off = target.offset;
                    closestDistance = distance;
                    closest = target.transform;
                }
            }
        }
        return tar;
    }
}
