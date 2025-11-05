using UnityEngine;

[CreateAssetMenu(fileName="DifficultyProfile", menuName="MazeEscape/DifficultyProfile")]
public class DifficultyProfile : ScriptableObject {
  [Range(0,1)] public float plantRatio = 0.6f, electricRatio = 0.2f, rawRatio = 0.2f, evaRatio = 0f;
  [Min(0)] public int totalEnemies = 8;
  public float moveSpeed = 2.0f, chaseRange = 6.0f, fireCooldown = 1.2f, projectileSpeed = 7.0f;
  public int damage = 1;
  public float shockRange = 2.5f; // para Electric si aplica
}
