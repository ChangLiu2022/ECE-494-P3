using UnityEngine;

public class SpriteSwapper : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite newSprite;

    public void Swap()
    {
        if (target != null && newSprite != null)
            target.sprite = newSprite;
    }
}