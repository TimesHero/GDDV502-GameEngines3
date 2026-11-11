using UnityEngine;
using Unity.Netcode;

public class CollectableSpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject collectablePrefab;
    [SerializeField] private Vector3 spawnArea;
    [SerializeField] private int initialSpawnCount = 10;
    [SerializeField] private float spawnInterval = 3.0f;

    private float timeSinceLastSpawn = 0;
    private bool initialWaveSpawned;

    public override void OnNetworkSpawn()
    {
        initialWaveSpawned = false;
        timeSinceLastSpawn = 0f;
    }

    private void Update()
    {
        if (!IsServer) return;
        // Outside a round, reset so the next round gets its own opening wave.
        if (GameStateManager.Singleton.State.Value != GameState.Playing)
        {
            initialWaveSpawned = false;
            timeSinceLastSpawn = 0f;
            return;
        }

        if (!initialWaveSpawned)
        {
            for (int i = 0; i < initialSpawnCount; i++)
                SpawnCollectable();
            initialWaveSpawned = true;
        }
        
        timeSinceLastSpawn += Time.deltaTime;
        if(timeSinceLastSpawn > spawnInterval)
        {
            SpawnCollectable();
            timeSinceLastSpawn = 0;
        }
    }

    private void SpawnCollectable()
    {
        Vector3 pos = GetRandomPosition();
        GameObject collectable = Instantiate(collectablePrefab, pos, Quaternion.identity);

        NetworkObject obj = collectable.GetComponent<NetworkObject>();
        obj.Spawn();
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(-spawnArea.x /2, spawnArea.x / 2),
            spawnArea.y,
            Random.Range(-spawnArea.z /2, spawnArea.z / 2));
    }


}
