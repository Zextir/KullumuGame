using UnityEngine;

public class DenseAir : MonoBehaviour
{
    [SerializeField, Range(0.1f,4)] float densityMultiplier = 1;
    [SerializeField] Color color = Color.green;
    [SerializeField] new ParticleSystem particleSystem;

    static Vector2 alphaMinMax = new Vector2(0.1f, 0.5f); 
    static Vector2 densityMinMax = new Vector2(0.1f, 4f);

    Vector3 colliderSize;

    public float Density
    {
        get => densityMultiplier;
    }

    private void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        ChangeParticles();
        ChangeCollider();
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
            boxCollider.size = new Vector3(colliderSize.x * 1.2f, colliderSize.y * 1.5f, colliderSize.z * 1.2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player") return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.drag = 1f;
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player") return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.drag = 0f;
        }
    }
}
