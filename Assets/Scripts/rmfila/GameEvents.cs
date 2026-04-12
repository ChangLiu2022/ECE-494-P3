using Unity.VisualScripting;
using UnityEngine;

public class GameEvents
{
    // guards subscribe to this event and the collectible publishes it
    public struct AlertEvent
    {
        public Vector3 position;
    }

    public struct GoldEvent
    {
        public int level_number;
    }

    public struct MoneyChangedEvent
    {
        public int current_money;
        public int delta;
    }

    // published when a guard catches the player
    // this is used to call the game over screen and mechanics
    public struct GameOverEvent { }

    // stop game
    public struct WinEvent
    {
        public bool is_final_win;
    }

    public struct PowerOffEvent { }

    public struct PowerOnEvent { }

    public struct VehicleEnterEvent
    {
        public Transform vehicleTransform;
    }

    public struct VehiclePitEvent { }

    public struct VehicleExitEvent { }

    public struct GameFreezeEvent { }
    public struct GameUnfreezeEvent { }

    public struct NoiseWaveEvent
    {
        public Vector3 origin;
        public float radius;
        public bool is_gunshot;
    }

    public struct FirstHitEvent { }

    public struct GuardShotEvent { }

    public struct UpgradeActivatedEvent { }

    public struct DowngradeActivatedEvent { }

    public struct TimerExpiredEvent { }

    public struct PlayerEnteredMapEvent { }

    // published by GunUpgrades.TryPurchase so any in scene gun can live refresh
    public struct UpgradePurchasedEvent
    {
        public GunUpgrades.Weapon weapon;
        public GunUpgrades.Track track;
        public int new_level;
    }
}
