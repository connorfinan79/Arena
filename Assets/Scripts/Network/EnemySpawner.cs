using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawns enemies periodically around the map
/// Only runs on server
/// </summary>
public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private Vector3 spawnCenter = Vector3.zero;
    
    [Header("Wave Settings")]
    [SerializeField] private bool useWaves = false;
    [SerializeField] private int enemiesPerWave = 5;
    [SerializeField] private float timeBetweenWaves = 30f;
    
    [Header("Difficulty Scaling")]
    [SerializeField] private bool scaleDifficulty = false;
    [SerializeField] private float healthMultiplierPerWave = 1.1f;
    [SerializeField] private float damageMultiplierPerWave = 1.05f;
    
    private float nextSpawnTime;
    private int currentEnemyCount;
    private int currentWave = 0;
    private int enemiesSpawnedThisWave;
    private float nextWaveTime;
    
    private void Update()
    {
        if (!IsServer) return;
        if (enemyPrefab == null) return;
        
        if (useWaves)
        {
            UpdateWaveSpawning();
        }
        else
        {
            UpdateContinuousSpawning();
        }
    }
    
    private void UpdateContinuousSpawning()
    {
        if (Time.time >= nextSpawnTime && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    private void UpdateWaveSpawning()
    {
        // Check if we should start next wave
        if (currentWave == 0 || (enemiesSpawnedThisWave >= enemiesPerWave && currentEnemyCount == 0))
        {
            if (Time.time >= nextWaveTime)
            {
                StartNextWave();
            }
        }
        
        // Spawn enemies for current wave
        if (enemiesSpawnedThisWave < enemiesPerWave && Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            enemiesSpawnedThisWave++;
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    private void StartNextWave()
    {
        currentWave++;
        enemiesSpawnedThisWave = 0;
        nextSpawnTime = Time.time;
        nextWaveTime = Time.time + timeBetweenWaves;
        
        Debug.Log($"<color=yellow>Wave {currentWave} starting!</color>");
        
        // Announce wave to all clients (optional - you can add UI later)
        AnnounceWaveClientRpc(currentWave);
    }
    
    [ClientRpc]
    private void AnnounceWaveClientRpc(int waveNumber)
    {
        Debug.Log($"<color=cyan>Wave {waveNumber} has begun!</color>");
        // Add UI notification here later
    }
    
    private void SpawnEnemy()
    {
        // Random position around spawn center
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, 2f, randomCircle.y);
        
        // Spawn enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = enemy.GetComponent<NetworkObject>();
        
        if (netObj != null)
        {
            netObj.Spawn();
            currentEnemyCount++;
            
            // Apply difficulty scaling if enabled
            if (scaleDifficulty && useWaves && currentWave > 1)
            {
                ApplyDifficultyScaling(enemy);
            }
            
            // Subscribe to death event to track enemy count
            EnemyCharacter enemyChar = enemy.GetComponent<EnemyCharacter>();
            if (enemyChar != null)
            {
                enemyChar.OnEnemyDestroyed += OnEnemyDestroyed;
            }
        }
    }
    
    private void ApplyDifficultyScaling(GameObject enemy)
    {
        BaseCharacter enemyChar = enemy.GetComponent<BaseCharacter>();
        if (enemyChar != null)
        {
            // Scale stats based on wave number
            // Note: This would require adding public methods to modify stats
            // For now, this is a placeholder for future implementation
            Debug.Log($"Enemy spawned with Wave {currentWave} scaling");
        }
    }
    
    private void OnEnemyDestroyed()
    {
        currentEnemyCount--;
        
        // Check if wave is complete
        if (useWaves && enemiesSpawnedThisWave >= enemiesPerWave && currentEnemyCount == 0)
        {
            Debug.Log($"<color=green>Wave {currentWave} complete! Next wave in {timeBetweenWaves} seconds.</color>");
        }
    }
    
    /// <summary>
    /// Manually trigger a spawn (useful for testing)
    /// </summary>
    [ContextMenu("Spawn Enemy Now")]
    public void SpawnEnemyManual()
    {
        if (IsServer || !Application.isPlaying)
        {
            SpawnEnemy();
        }
    }
    
    /// <summary>
    /// Clear all enemies
    /// </summary>
    [ContextMenu("Clear All Enemies")]
    public void ClearAllEnemies()
    {
        if (!IsServer) return;
        
        EnemyCharacter[] enemies = FindObjectsOfType<EnemyCharacter>();
        foreach (EnemyCharacter enemy in enemies)
        {
            if (enemy != null && enemy.GetComponent<NetworkObject>() != null)
            {
                enemy.GetComponent<NetworkObject>().Despawn();
            }
        }
        
        currentEnemyCount = 0;
    }
    
    // Visualize spawn area in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
        
        // Draw spawn center
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(spawnCenter, 0.5f);
    }
}