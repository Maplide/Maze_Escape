using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsUI : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerHealth target;           // arrastra el Player
    public Transform container;           // HorizontalLayoutGroup
    public GameObject heartPrefab;        // prefab con Image

    [Header("Sprites")]
    public Sprite heartFull;
    public Sprite heartHalf;
    public Sprite heartEmpty;

    readonly List<Image> _hearts = new();

    void OnEnable()
    {
        if (!target) target = Object.FindFirstObjectByType<PlayerHealth>();
        if (target)
        {
            target.OnHealthChanged += HandleChanged;
            Rebuild(); // construcción inicial
            HandleChanged(target.currentHealth, target.maxHealth);
        }
    }
    void OnDisable()
    {
        if (target) target.OnHealthChanged -= HandleChanged;
    }

    void Rebuild()
    {
        // corazones = ceil(maxHP / 2)
        int heartCount = Mathf.CeilToInt(target.maxHealth / 2f);

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
        _hearts.Clear();

        for (int i = 0; i < heartCount; i++)
        {
            var go = Instantiate(heartPrefab, container);
            var img = go.GetComponent<Image>();
            if (!img) img = go.AddComponent<Image>();
            _hearts.Add(img);
        }
    }

    void HandleChanged(int hp, int max)
    {
        // reconstruye si cambió el max (power-ups)
        if (_hearts.Count != Mathf.CeilToInt(max / 2f))
            Rebuild();

        for (int i = 0; i < _hearts.Count; i++)
        {
            int heartHPMin = i * 2;      // rango de HP que representa este corazón: [2i .. 2i+1]
            int value = Mathf.Clamp(hp - heartHPMin, 0, 2);

            _hearts[i].sprite = value >= 2 ? heartFull :
                                value == 1 ? heartHalf  :
                                             heartEmpty;
            _hearts[i].enabled = true;
        }
    }
}
