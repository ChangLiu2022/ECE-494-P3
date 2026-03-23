using UnityEngine;

public class GameEvents
{
    // guards subscribe to this event and the collectible publishes it
    public struct AlertEvent { }


    // published when a guard catches the player
    // this is used to call the game over screen and mechanics
    public struct GameOverEvent { }


    // stop game
    public struct WinEvent { }
    

    public struct PowerOffEvent { }
    

    public struct PowerOnEvent { }


    // lights event to toggle guard vision cone color and fov change for now
    public struct LightsOutEvent { }


    // activated when player shoots, carries position at the time of shot
    public struct GunshotEvent
    {
        public Vector3 player_position;
    }

    public struct VehicleEnterEvent
    {
        public Transform vehicleTransform;
    }

    public struct VehicleExitEvent { }

}
