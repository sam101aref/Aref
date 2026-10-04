using System.Collections;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>Wooden things that fire arrows burn away (F-27), e.g. barricades used as cover.</summary>
    public class Flammable : MonoBehaviour
    {
        [SerializeField] float burnTime = 1.2f;
        [SerializeField] Color burningColor = new Color(1f, 0.45f, 0.1f);
        [SerializeField] Color charredColor = new Color(0.15f, 0.1f, 0.08f);

        public bool IsBurning { get; private set; }

        public void Ignite()
        {
            if (IsBurning)
                return;
            IsBurning = true;
            StartCoroutine(Burn());
        }

        IEnumerator Burn()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            for (var t = 0f; t < burnTime; t += Time.deltaTime)
            {
                var k = t / burnTime;
                var flicker = 0.85f + 0.15f * Mathf.Sin(t * 40f);
                foreach (var r in renderers)
                    if (r != null)
                        r.color = Color.Lerp(burningColor * flicker, charredColor, k);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
