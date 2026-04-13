using UnityEngine;

public class FixedAudioListener : MonoBehaviour
{
    [SerializeField] private Transform player;

    private void LateUpdate()
    {
        if (player == null) return;

        // Follow position only
        transform.position = player.position;

        // DO NOT rotate this is the key
        transform.rotation = Quaternion.identity;
    }
}