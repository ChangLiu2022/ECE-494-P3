using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static GameEvents;

public class HasInventory : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int bullets = 6;

    // to help determine whether the player can shoot
    public int BulletCount() => bullets;

    // to add/remove bullets manually
    public void AddBullets(int delta) => bullets += delta;

    // generic shoot method, can be modified later
    // rounds_used added for ease of shotgun, etc. implementation
    public void Shoot(int rounds_used)
    {
        if(bullets < rounds_used)
        {
            Debug.Log("Not enough bullets to shoot!");
            return;
        }

        bullets -= rounds_used;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("We have collided!");
        // when the player collides with the gold:
        if(other.CompareTag("Gold"))
        {
            other.gameObject.SetActive(false); // 1) deactivate the game object ("pick it up")
            Debug.Log("Exit/ending the game!"); // 2) change game state (to be added later)
            EventBus.Publish(new AlertEvent()); // 3) publish the AlertEvent()
        }
    }
}
