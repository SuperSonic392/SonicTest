using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class fpscounter : MonoBehaviour
{
    void Update()
    {
        GetComponent<TMP_Text>().text = (1.0f / Time.deltaTime).ToString() + "\n" + Application.targetFrameRate;
    }
}
