using UnityEngine;

namespace Arash.Combat
{
    /// <summary>World-space health bar above a character, drawn with two sliced sprites.</summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] SpriteRenderer fill;
        [SerializeField] float width = 1.16f;
        [SerializeField] float height = 0.12f;
        [SerializeField] Color fullColor = new Color(0.35f, 0.8f, 0.3f);
        [SerializeField] Color emptyColor = new Color(0.85f, 0.2f, 0.15f);

        void OnEnable()
        {
            if (health == null)
                health = GetComponentInParent<Health>();
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Changed += Refresh;
            }
            Refresh();
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Changed -= Refresh;
            }
        }

        void OnDamaged(DamageInfo info, bool killed)
        {
            if (killed)
                gameObject.SetActive(false);
            else
                Refresh();
        }

        void Refresh()
        {
            if (health == null || fill == null || !gameObject.activeInHierarchy)
                return;

            var fraction = Mathf.Clamp01(health.Fraction);
            fill.size = new Vector2(width * fraction, height);
            fill.transform.localPosition = new Vector3(-width * 0.5f + width * fraction * 0.5f, 0f, 0f);
            fill.color = Color.Lerp(emptyColor, fullColor, fraction);
        }
    }
}
