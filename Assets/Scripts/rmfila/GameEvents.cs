public class GameEvents
{
    // guards subscribe to this event and the collectible publishes it
    public struct AlertEvent { }


    // published when a guard catches the player
    // this is used to call the game over screen and mechanics
    public struct GameOverEvent { }


    // stop game
    public struct WinEvent { }


    // lights event to toggle guard vision cone color and fov change for now
    public struct LightsOutEvent { }
}
