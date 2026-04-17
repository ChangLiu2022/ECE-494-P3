using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static GameEvents;

public class HeavyGuardSpawner : MonoBehaviour
{
    public static bool CutsceneActive { get; private set; } = false;

    [Header("Spawning")]
    [SerializeField] private GameObject heavyGuardPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 3f;

    [Header("Cutscene")]
    [SerializeField] private float cameraPanSpeed = 8f;
    [SerializeField] private float cutsceneHoldTime = 2f;
    [SerializeField] private float cameraHeight = 15f;

    [Header("Dependencies")]
    [SerializeField] private GameObject mainCamera;

    [Header("References to Freeze")]
    [SerializeField] private PlayerAiming player_aiming;
    [SerializeField] private PlayerMovement player_movement;
    [SerializeField] private GameObject weapon_pivot;
    [SerializeField] private GameObject crosshair_canvas;

    private Transform player;
    private bool isSpawning = false;

    private void OnEnable()
    {
        EventBus.Subscribe<TimerExpiredEvent>(OnTimerExpired);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<TimerExpiredEvent>(OnTimerExpired);
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Body");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void OnTimerExpired(TimerExpiredEvent e)
    {
        if (isSpawning) return;
        isSpawning = true;
        StartCoroutine(TimerExpiredSequence());
    }

    private IEnumerator TimerExpiredSequence()
    {
        Transform cutsceneSpawn = GetFurthestSpawnPoint();
        if (cutsceneSpawn == null) yield break;

        // --- FREEZE EVERYTHING MANUALLY (but keep timeScale = 1) ---

        CutsceneActive = true;

        // disable player
        if (crosshair_canvas != null) crosshair_canvas.SetActive(false);
        Cursor.visible = true;
        if (weapon_pivot != null) weapon_pivot.SetActive(false);
        if (player_aiming != null) player_aiming.enabled = false;
        if (player_movement != null) player_movement.can_move = false;

        // disable camera follow
        CameraFollow camFollow = null;
        if (mainCamera != null)
        {
            camFollow = mainCamera.GetComponent<CameraFollow>();
            if (camFollow != null) camFollow.enabled = false;
        }

        // stop all EXISTING guards (not the heavy ones we're about to spawn)
        GuardController[] existingGuards = FindObjectsOfType<GuardController>();
        NavMeshAgent[] stoppedAgents = new NavMeshAgent[existingGuards.Length];
        GunBarController gunBar = FindObjectOfType<GunBarController>();

        for (int i = 0; i < existingGuards.Length; i++)
        {
            NavMeshAgent agent = existingGuards[i].GetComponentInChildren<NavMeshAgent>();

            if (gunBar != null) 
                gunBar.enabled = false;

            existingGuards[i].StopAllCoroutines();

            if (agent != null)
            {
                agent.isStopped = true;
                stoppedAgents[i] = agent;
            }
            existingGuards[i].enabled = false;
        }

        // --- SPAWN THE FIRST GUARD ---
        GameObject firstGuard = Instantiate(
            heavyGuardPrefab,
            cutsceneSpawn.position,
            cutsceneSpawn.rotation
        );

        // --- PAN CAMERA TO SPAWN POINT ---
        Transform cam = mainCamera.transform;
        Vector3 camStartPos = cam.position;
        Vector3 camTargetPos = new Vector3(
            cutsceneSpawn.position.x,
            cameraHeight,
            cutsceneSpawn.position.z
        );

        // smoothly pan to spawn
        while (Vector3.Distance(cam.position, camTargetPos) > 0.1f)
        {
            cam.position = Vector3.MoveTowards(
                cam.position, camTargetPos, cameraPanSpeed * Time.deltaTime
            );
            yield return null;
        }
        cam.position = camTargetPos;

        // --- HOLD: watch the guard walk in ---
        yield return new WaitForSeconds(cutsceneHoldTime);

        // --- PAN CAMERA BACK TO PLAYER ---
        Vector3 returnPos = new Vector3(
            player.position.x,
            cameraHeight,
            player.position.z
        );

        while (Vector3.Distance(cam.position, returnPos) > 0.1f)
        {
            // update return target in case we want latest player pos
            returnPos = new Vector3(player.position.x, cameraHeight, player.position.z);
            cam.position = Vector3.MoveTowards(
                cam.position, returnPos, cameraPanSpeed * Time.deltaTime
            );
            yield return null;
        }

        // --- UNFREEZE EVERYTHING ---

        // re-enable existing guards
        for (int i = 0; i < existingGuards.Length; i++)
        {
            existingGuards[i].enabled = true;
            if (stoppedAgents[i] != null)
                stoppedAgents[i].isStopped = false;
        }

        // re-enable player
        if (crosshair_canvas != null) crosshair_canvas.SetActive(true);
        Cursor.visible = false;
        if (weapon_pivot != null) weapon_pivot.SetActive(true);
        if (player_aiming != null) player_aiming.enabled = true;
        if (player_movement != null) player_movement.can_move = true;    

        if (gunBar != null) 
            gunBar.enabled = true;

        // re-enable camera follow
        if (camFollow != null) camFollow.enabled = true;

        // --- START CONTINUOUS SPAWNING ---
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            Transform point = GetFurthestSpawnPoint();
            if (point != null && heavyGuardPrefab != null)
            {
                Instantiate(heavyGuardPrefab, point.position, point.rotation);
            }
        }
    }

    private Transform GetFurthestSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        if (player == null) return spawnPoints[0];

        Transform furthest = null;
        float maxDist = -1f;

        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;
            float dist = Vector3.Distance(player.position, point.position);
            if (dist > maxDist)
            {
                maxDist = dist;
                furthest = point;
            }
        }

        return furthest;
    }
}
