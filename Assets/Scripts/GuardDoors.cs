using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardDoors : MonoBehaviour
{
    public float interactRange = 0.75f;
    public float swingAngle = 90f;
    public float swingSpeed = 3f;
    public BoxCollider doorCollider;
    public bool IsBlockedByGuard = false;

    private Transform player;
    private bool isOpen = false;
    private float closedAngle;
    private float targetAngle;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        closedAngle = transform.localEulerAngles.y;
        targetAngle = closedAngle;
    }

    void Update()
    {
        Vector3 closestPoint = doorCollider.ClosestPoint(player.position);
        float distance = Vector3.Distance(closestPoint, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            if (isOpen)
            {
                if (!IsBlockedByGuard)
                    CloseDoor();
            } 
            else
            {
                OpenDoor(player);
            }
        }

        float currentY = transform.localEulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * swingSpeed);
        transform.localEulerAngles = new Vector3(0, newY, 0);
    }

    public void OpenDoor(Transform opener)
    {
        if (isOpen) return;

        Vector3 localPos = transform.InverseTransformPoint(opener.position);
        float direction = localPos.x > 0 ? 1f : -1f;

        targetAngle = closedAngle + (swingAngle * direction);
        isOpen = true;
    }

    public void CloseDoor()
    {
        if (!isOpen) return;

        targetAngle = closedAngle;
        isOpen = false;
    }
}
