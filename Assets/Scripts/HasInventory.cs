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
        PlayerWallet.ResetLevelProgress();
        exit = GameObject.FindGameObjectWithTag("EXIT");
        if(exit != null) exit.SetActive(false);
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
        // when the player collides with the gold
        if (other.CompareTag("Gold"))
        {
            PlayerWallet.ClaimLevelReward();
            EventBus.Publish(new GoldEvent 
            { 
                level_number = PlayerWallet.current_level 
            });
        }

        else if (other.CompareTag("EXIT"))
        {
            EventBus.Publish(new WinEvent 
            { 
                is_final_win = true 
            });
        }

        else if (other.CompareTag("PistolAmmo"))
        {
            bullets = 6;
            other.gameObject.SetActive(false);
            EventBus.Publish(new AmmoChangedEvent());
        }
    }
}
public static class GunEvents
{
    // alerts HUD to flash the ammo indicator red
    public struct FailedToFireEvent { }


    // used to tell HUD to update its ammo count
    public struct AmmoChangedEvent { }

    public struct WeaponChangedEvent
    {
        public string new_weapon;

        public WeaponChangedEvent(string newWeapon)
        {
            new_weapon = newWeapon;
        }
    }
}
