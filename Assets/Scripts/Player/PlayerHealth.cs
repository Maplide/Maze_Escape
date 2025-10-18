using UnityEngine;
using System;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 6;          // ponlo acorde a tus “segmentos”
    public int currentHealth = 6;

    [Header("Invencibilidad")]
    public float invulnTime = 0.6f;    // tiempo tras recibir daño
    float invulnCd;

    public event Action<int,int> OnHealthChanged;   // (vida, max)
    public event Action OnDead;

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        if (invulnCd > 0f) invulnCd -= Time.deltaTime;
    }

    public void Damage(int amount)
    {
        if (amount <= 0) return;
        if (invulnCd > 0f) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        invulnCd = invulnTime;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(int newMax, bool fill = true)
    {
        maxHealth = Mathf.Max(1, newMax);
        if (fill) currentHealth = maxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        OnDead?.Invoke();
        // TODO: desactivar control, reproducir animación, recargar escena, etc.
        // Ejemplo simple:
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
