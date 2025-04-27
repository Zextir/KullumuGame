using UnityEngine;
using UnityEngine.UI;

public class LoadingFruitColor : MonoBehaviour
{
    [SerializeField] Color[] possibleColors;
    [SerializeField] Image[] fruitLayers;

    private void Awake()
    {
        Color chosenColor = possibleColors[Random.Range(0, possibleColors.Length)];
        foreach (var fruitLayer in fruitLayers)
        {
            Color currentColor = fruitLayer.color;
            currentColor.r = chosenColor.r;
            currentColor.g = chosenColor.g;
            currentColor.b = chosenColor.b;
            fruitLayer.color = currentColor;
            
        }
    }
}
