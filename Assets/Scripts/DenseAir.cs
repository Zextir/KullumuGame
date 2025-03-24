using UnityEngine;

public class DenseAir : MonoBehaviour
{
    [SerializeField, Range(0.1f,4)] float densityMultiplier = 1;
    [SerializeField] Color color = Color.green;
    [SerializeField] new ParticleSystem particleSystem;
     
    static Vector2 alphaMinMax = new Vector2(0.001f, 0.05f); 
    static Vector2 densityMinMax = new Vector2(0.1f, 4f);

    Vector3 colliderSize;

    public float Density
    {
        get => densityMultiplier;
    }

    private void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
    }

    private void OnValidate()
    {
        ChangeParticles();
        ChangeCollider();
    }

    void ChangeParticles()
    {
        float densityInterval = densityMinMax.y - densityMinMax.x;
        float alphaInterval = alphaMinMax.y - alphaMinMax.x;

        float density = densityMultiplier;
        float densityMod = Mathf.Pow((density - densityMinMax.x) / densityInterval, 3) * alphaInterval;

        colliderSize = particleSystem.shape.scale;
        float volume = colliderSize.x * colliderSize.y * colliderSize.z;
        float side = Mathf.Pow(volume, 0.33f);

        ParticleSystem.MainModule mainPS = particleSystem.main;
        ParticleSystem.MinMaxCurve startSize = mainPS.startSize;
        startSize.constant = side;
        mainPS.startSize = startSize;

        ParticleSystem.MinMaxGradient colorGrad = mainPS.startColor;
        color.a = alphaMinMax.x + densityMod;
        colorGrad = color;
        mainPS.startColor = colorGrad;

        particleSystem.Clear();
        particleSystem.Play();
    }

    void ChangeCollider()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.size = colliderSize;
        }
    }
}
