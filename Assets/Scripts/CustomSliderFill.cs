using UnityEngine;

public class CustomSliderFill : MonoBehaviour
{
    [SerializeField] int segments;
    [SerializeField] GameObject fillSquarePrefab;

    RectTransform rectTransform;

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        int height = 20;
        int width = 30 * segments - 10;

        float currentPosition = 0;

        rectTransform.sizeDelta = new Vector2(width, height);

        for (int i = 0; i < segments; i++)
        {
            RectTransform inst = Instantiate(fillSquarePrefab, transform).GetComponent<RectTransform>();
            inst.anchoredPosition = new Vector2(0, currentPosition);

            currentPosition += 30;
        }
    }


}
