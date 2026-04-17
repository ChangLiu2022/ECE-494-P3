using UnityEngine;

public class HeartEmitter : MonoBehaviour
{
    public ParticleSystem heartParticles;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            heartParticles.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            heartParticles.Stop();
        }
    }
}