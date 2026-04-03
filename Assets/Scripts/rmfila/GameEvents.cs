using UnityEngine;

public class GameEvents
{
    // guards subscribe to this event and the collectible publishes it
    public struct AlertEvent
    {
        public Vector3 position;
    }

    public struct GoldEvent { }

    // published when a guard catches the player
    // this is used to call the game over screen and mechanics
    public struct GameOverEvent { }

    // stop game
    public struct WinEvent { }
    
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
}
