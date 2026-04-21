using UnityEngine;

public class HeartEmitter : MonoBehaviour
{
    public ParticleSystem heartParticles;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Body"))
        {
            Debug.Log("Player entered heart emitter trigger.");
            heartParticles.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {

        if (other.CompareTag("Player") | other.CompareTag("Body"))
        {
            Debug.Log("Player left heart emitter trigger.");

            heartParticles.Stop();
        }
    }
}