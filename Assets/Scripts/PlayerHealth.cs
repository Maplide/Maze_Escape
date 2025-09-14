using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHP = 3;
    public float invincibleTime = 0.6f;   // i-frames tras recibir daño
    public bool flashOnHit = true;

    int hp;
    bool invincible;
    SpriteRenderer sr;

    void Awake(){
        hp = maxHP;
        sr = GetComponentInChildren<SpriteRenderer>(); // busca sprite para parpadeo (opcional)
    }

    public void Damage(int dmg)
    {
        if (invincible) return;

        hp -= Mathf.Max(1, dmg);
        Debug.Log("HP Player: " + hp);

        // registra golpe para el panel de "nivel completado"
        LevelStatsTracker.Instance?.RegisterHit();

        if (hp <= 0)
        {
            // respawn simple
            Time.timeScale = 1f; // por si estabas pausado
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            return;
        }

        // i-frames + feedback visual
        if (invincibleTime > 0f)
            StartCoroutine(InvincibleCR());
    }

    IEnumerator InvincibleCR(){
        invincible = true;
        float t = 0f;
        while (t < invincibleTime){
            t += Time.deltaTime;
            if (flashOnHit && sr){
                // parpadeo
                sr.enabled = !sr.enabled;
            }
            yield return new WaitForSeconds(0.08f);
        }
        if (sr) sr.enabled = true;
        invincible = false;
    }
}
