using UnityEngine;

using static GameEvents;
using static GunEvents;

public class HasInventory : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int bullets = 6;
    [SerializeField] private GameObject exit;

    // to help determine whether the player can shoot
    public int BulletCount => bullets;


    private void Awake()
    {
        exit = GameObject.FindGameObjectWithTag("EXIT");
        exit.SetActive(false);
    }

    // to add/remove bullets manually
    public void AddBullets(int delta)
    {
        bullets += delta;
        EventBus.Publish(new AmmoChangedEvent()); // to tell HUD to update ammo count
    }

    // generic shoot method, can be modified later
    // rounds_used added for ease of shotgun, etc. implementation
    public void Shoot(int rounds_used)
    {
        if(bullets < rounds_used)
        {
            EventBus.Publish(new FailedToFireEvent());
            Debug.Log("Not enough bullets to shoot!");
            return;
        }

        bullets -= rounds_used;
        EventBus.Publish(new AmmoChangedEvent()); // to tell HUD to update ammo count
    }

    private void OnTriggerEnter(Collider other)
    {
        // when the player collides with the gold:
        if(other.CompareTag("Gold"))
        {
            other.gameObject.SetActive(false); // 1) deactivate the game object ("pick it up")
            EventBus.Publish(new GoldEvent()); // 4) publish the GoldEvent() to tell the HUD to update the gold count
            exit.SetActive(true);
        }

        else if (other.CompareTag("EXIT"))
        {
            EventBus.Publish(new WinEvent());
        }
    }
}
public static class GunEvents
{
    // alerts HUD to flash the ammo indicator red
    public struct FailedToFireEvent { }


    // used to tell HUD to update its ammo count
    public struct AmmoChangedEvent { }
}
