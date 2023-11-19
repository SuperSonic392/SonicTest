using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : GameEntity
{
    private void OnTriggerEnter(Collider other)
    {
        HedgehogController obj = other.GetComponent<HedgehogController>();
        if (obj != null)
        {
            if(obj.currentstate == HedgehogController.CharacterState.Spin || obj.currentstate == HedgehogController.CharacterState.Spindash)
            {
                obj.currentstate = HedgehogController.CharacterState.Spin;
                if (!obj.grounded)
                {
                    obj.airspd = Vector3.up * 15;
                    obj.scriptLock = 0f;
                }
                Destroy(this.gameObject);
            }
            else
            {
                obj.airspd = -obj.model.forward * 10 + Vector3.up * 10;
                obj.currentstate = HedgehogController.CharacterState.Stun;
                obj.grounded = false;
                obj.transform.position += Vector3.up * 0.1f;
            }
        }
    }
}