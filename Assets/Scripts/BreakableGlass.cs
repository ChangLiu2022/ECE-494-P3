using UnityEngine;

public class BreakableGlass : MonoBehaviour
{
    [SerializeField] private int health = 1;
    [SerializeField] public bool trigger_backup_timer = true;
    [SerializeField] private GameObject glass_break_particle;
    [SerializeField] private AudioClip break_sound;
    [SerializeField][Range(0f, 1f)] private float break_sound_volume = 0.25f;
    [SerializeField] private float noise_radius = 12f;

    private bool broken = false;

    public void TakeDamage(Collider bullet_col)
    {
        if (broken)
        {
            return;
        }

        health--;

        if (health <= 0)
        {
            Break();
        }
    }

    private void Break()
    {
        broken = true;

        if (glass_break_particle != null)
        {
            Instantiate(glass_break_particle, transform.position, transform.rotation);
        }

        if (break_sound != null)
        {
            AudioSource.PlayClipAtPoint(break_sound, transform.position, break_sound_volume);
        }

        AlertNearbyGuards();

        if (trigger_backup_timer)
        {
            EventBus.Publish(new GameEvents.PlayerEnteredMapEvent());
        }

        Destroy(gameObject);
    }

    private void AlertNearbyGuards()
    {
        GuardController[] all_guards = FindObjectsOfType<GuardController>();

        foreach (var guard in all_guards)
        {
            if (Vector3.Distance(transform.position, guard.transform.position) <= noise_radius)
            {
                guard.Alert();
            }
        }
    }
}