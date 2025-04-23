using UnityEngine;

public class CustomSliderFill : MonoBehaviour
{

    [SerializeField] GameObject squarePrefab;

    public void CreateSlider(int segments)
    {
        float currentPosition = 0;        

        for (int i = 0; i < segments; i++)
        {
            RectTransform inst = Instantiate(squarePrefab, transform).GetComponent<RectTransform>();
            inst.anchoredPosition = new Vector2(0, currentPosition);

            currentPosition += 30;
        }
    }

}
