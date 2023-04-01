using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookatMouse : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = - (transform.position.x - Camera.main.transform.position.x);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint (mousePos);
        transform.LookAt (worldPos);    
    }
}
