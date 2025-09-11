using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 3;
    int hp;

    void Awake(){ hp = maxHP; }

    public void Damage(int dmg)
    {
        hp -= dmg;
        Debug.Log("HP Player: " + hp);
        if (hp <= 0)
        {
            // respawn simple: recarga escena
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
