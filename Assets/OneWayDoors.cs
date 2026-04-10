using UnityEngine;


public class OneWayDoors : MonoBehaviour
{
    [SerializeField] private bool guard_only_allowed = false;
    [SerializeField] private bool player_one_way = false;

    [SerializeField] private BoxCollider blocked;
    [SerializeField] private Behaviour ExitDoorScript;

    private GuardDoors guard_doors;
    private bool player_entered;
    private bool player_exited_one_way = false;



    public bool GetPlayerEnetered()     
    {
        return player_entered;
    }


    public bool GetPlayerExitedOneWay()
    {
        return player_exited_one_way;
    }


    private void Start()
    {
        blocked.enabled = true;
        player_entered = false;

        if (ExitDoorScript != null)
            ExitDoorScript.enabled = false;
    }


    private void OnTriggerEnter(Collider coll)
    {
        // if guard is only allowed and the collision is a guard
        if (guard_only_allowed && coll.CompareTag("Enemy"))
        {
            // disable the collider to let them through
            blocked.enabled = false;
        }
        // if player is entering and its the first time they entered
        // and the door is marked for just the player
        else if (!player_entered && player_one_way && coll.CompareTag("Player"))
        {
            // set the bool to signify the player entered
            player_entered = true;
            // and disable the collider to let them through
            blocked.enabled = false;
        }
    }


    private void OnTriggerExit(Collider coll)
    {
        if (guard_only_allowed && coll.CompareTag("Enemy"))
        {
            blocked.enabled = true;
        }

        else if (player_one_way && coll.CompareTag("Player"))
        {
            player_entered = true;
            blocked.enabled = true;
            player_exited_one_way = true;

            if (ExitDoorScript != null)
                ExitDoorScript.enabled = true;

            EventBus.Publish(new GameEvents.PlayerEnteredMapEvent());
        }
    }
}