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
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
            gameObject.SetActive(!gameObject.activeSelf);

        if (gameObject.activeSelf == false)
            return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.x += offsetX;
        mousePos.y += offsetY;

        transform.position = mousePos;
    }
}