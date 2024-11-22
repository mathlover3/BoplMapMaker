using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MapMaker.utils
{
    [RequireComponent(typeof(TextMeshPro))]
    public class FadeOutText : MonoBehaviour
    {
        private TextMeshPro textMeshPro;

        void Start()
        {
            textMeshPro = GetComponent<TextMeshPro>();
            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            yield return new WaitForSeconds(7f);

            float duration = 3f;
            float elapsedTime = 0f;
            Color originalColor = textMeshPro.color;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
                textMeshPro.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            textMeshPro.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            Destroy(gameObject);
        }
    }
}
