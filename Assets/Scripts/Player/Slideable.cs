using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using UnityEngine;

public class Slideable : MonoBehaviour
{
    [SerializeField] bool hasIncreasedSliding = false;

    public bool HasIncreasedSliding => hasIncreasedSliding;


    bool area = false;

    private void Start()
    {
        foreach (var collider in GetComponents<Collider>())
            area |= collider.isTrigger;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!area) return;

        var ucl = other.GetComponent<UltimateCharacterLocomotion>();
        if (ucl == null) return;
        var slide = ucl.GetAbility<SlideV2>();
        if (slide == null) return;
        slide.IncreasedSlideArea = this;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!area) return;
        var ucl = other.GetComponent<UltimateCharacterLocomotion>();
        if (ucl == null) return;
        var slide = ucl.GetAbility<SlideV2>();
        if (slide == null) return;
        slide.IncreasedSlideArea = null;
    }


}
