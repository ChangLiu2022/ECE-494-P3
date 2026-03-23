using UnityEngine;

public class Crosshair : MonoBehaviour
{
    private void Awake()
    {
        Cursor.visible = false;
    }
    private void Update()
    {
        //if (aiming == null) return;

        //transform.position = aiming.AimPoint;
        transform.position = Input.mousePosition;
    }
}