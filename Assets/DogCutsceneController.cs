using UnityEngine;

public class DogController : MonoBehaviour
{
    public void TeleportTo(GameObject target)
    {
        transform.position = target.transform.position;
    }
}