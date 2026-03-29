using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private float offsetX = 0f;
    [SerializeField] private float offsetY = 0f;

    private void Awake()
    {
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.x += offsetX;
        mousePos.y += offsetY;

        transform.position = mousePos;
    }
}