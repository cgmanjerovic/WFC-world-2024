using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraMove : MonoBehaviour
{
    public const int MAP_SIZE = 30;
    Vector3 pivotPoint = new Vector3(MAP_SIZE, 0, MAP_SIZE);
    public const float SPEED = 40f;
    Transform t;

    // Start is called before the first frame update
    void Start()
    {
        t = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            t.RotateAround(pivotPoint, Vector3.up, -SPEED * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            t.RotateAround(pivotPoint, Vector3.up, SPEED * Time.deltaTime);
        }
    }
}
