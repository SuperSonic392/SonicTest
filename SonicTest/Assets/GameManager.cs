using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int FPS;
    public GameObject playerObj;
    public Vector3 spawnOrigin;
    public Vector3 spawnDirection;
    public Transform characterCamera;
    public float killPlane;
    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.identity;
        GameObject localObj = Instantiate(playerObj, spawnOrigin, Quaternion.Euler(spawnDirection));
        HedgehogController localplayer = localObj.GetComponent<HedgehogController>();
        localplayer.localPlayer = true;
        localplayer.frameMultiplier = (float)FPS / 30;
        localplayer.input.POV = characterCamera;
        localplayer.cameraTransform = characterCamera;
        localplayer.gameManager = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.25f);
        Gizmos.DrawCube(Vector3.up * killPlane, new Vector3(999999, 0, 999999));
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(spawnOrigin + (Vector3.up * playerObj.GetComponent<HedgehogController>().height), 2);
        transform.rotation = Quaternion.Euler(spawnDirection);
        Gizmos.DrawLine(spawnOrigin + (Vector3.up * playerObj.GetComponent<HedgehogController>().height), spawnOrigin + (Vector3.up * playerObj.GetComponent<HedgehogController>().height + (transform.forward * 3)));
        transform.rotation = Quaternion.identity;
    }
}
