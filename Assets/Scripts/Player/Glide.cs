using Opsive.UltimateCharacterController.Character.Abilities;
using UnityEngine;

using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Character;


[DefaultInputName("Jump")]
[DefaultStartType(AbilityStartType.LongPress)]
[DefaultStopType(AbilityStopType.ButtonUp)]
[DefaultAbilityIndex(1)]
[DefaultUseRootMotionPosition(AbilityBoolOverride.False)]
[DefaultUseRootMotionRotation(AbilityBoolOverride.False)]
public class Glide : Ability
{
    [Tooltip("Multiplier applied to gravity while gliding.")]
    [SerializeField, Range(0,1)] float gravityMultiplier = 0.8f;

    public float GravityMultiplier => gravityMultiplier;


    public override void Awake()
    {
        base.Awake();
        EventHandler.RegisterEvent<Ability, bool>(m_GameObject, "OnCharacterAbilityActive", SwitchToGliding);
        EventHandler.RegisterEvent<bool>(m_GameObject, "OnCharacterGrounded", OnGrounded);
    }

    void SwitchToGliding(Ability ability, bool activated)
    {
        if (ability is Jump && !activated)
        {
            StartAbility();
        }
    }

    void OnGrounded(bool grounded)
    {
        StopAbility(true);
    }


    public override bool ShouldBlockAbilityStart(Ability startingAbility)
    {
        return startingAbility is Fall;
    }


    public override void OnDestroy()
    {
        base.OnDestroy();
        EventHandler.UnregisterEvent<Ability, bool>(m_GameObject, "OnCharacterAbilityActive", SwitchToGliding);
        EventHandler.UnregisterEvent<bool>(m_GameObject, "OnCharacterGrounded", OnGrounded);
    }

    void OnCollisionEnter(Collision collision)
    {
        StopAbility();
    }
}
