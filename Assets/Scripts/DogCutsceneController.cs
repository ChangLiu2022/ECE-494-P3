using UnityEngine;

public class DogController : MonoBehaviour
{
    public void TeleportTo(GameObject target)
    {
        transform.position = target.transform.position;
    }

    public void Rotate()
    {
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void RotateOpp()
    {
        transform.rotation = Quaternion.Euler(90f, 90f, 0f);
    }
}