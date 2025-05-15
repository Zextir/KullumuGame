using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using UnityEngine;

public class Slideable : MonoBehaviour
{
    [SerializeField] uint levelOfSliding = 1;

    public uint LevelOfSliding => levelOfSliding;


    bool area = false;

    private void Start()
    {
        foreach (var collider in GetComponents<Collider>())
            area |= collider.isTrigger;
        if (area && levelOfSliding < 2) levelOfSliding = 2;
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

    private void OnValidate()
    {
        if (levelOfSliding == 0) levelOfSliding = 1;
    }
}
