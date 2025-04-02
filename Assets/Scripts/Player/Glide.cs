using Opsive.UltimateCharacterController.Character.Abilities;
using UnityEngine;

using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Character;
using Unity.VisualScripting.FullSerializer;


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

    //private float originalGravity = -1;
    private UltimateCharacterLocomotion UCharacterLocomotion;

    private DensityGravityHandler densityGravityHandler;


    public override void Awake()
    {
        base.Awake();
        UCharacterLocomotion = GetComponent<UltimateCharacterLocomotion>();
        densityGravityHandler = GetComponent<DensityGravityHandler>();
        EventHandler.RegisterEvent<Ability, bool>(m_GameObject, "OnCharacterAbilityActive", SwitchToGliding);
        EventHandler.RegisterEvent<bool>(m_GameObject, "OnCharacterGrounded", OnGrounded);
    }

    void SwitchToGliding(Ability ability, bool activated)
    {
        //if (!CanStartAbility()) return;
        if (ability is Jump && !activated)
        {
            StartAbility();
        }
    }

    void OnGrounded(bool grounded)
    {
        StopAbility(true);
    }

    // Will be called when pressing space, not while it's held (after jumping, for example)
    //public override bool CanStartAbility()
    //{
    //    return !m_grounded && base.CanStartAbility();
    //}

    //public override bool CanStopAbility(bool force)
    //{
    //    if (force) return true;

    //    return base.CanStopAbility(force);
    //}

    //on ability start: set gravity, collision is false
    protected override void AbilityStarted()
    {
        base.AbilityStarted();
        float modifiedGravity = gravityMultiplier;
        if (densityGravityHandler != null) modifiedGravity *= densityGravityHandler.GravityMultiplier;
        UCharacterLocomotion.GravityAmount = modifiedGravity;
    }

    protected override void AbilityStopped(bool force)
    {
        base.AbilityStopped(force);
        UCharacterLocomotion.GravityAmount = densityGravityHandler != null ? densityGravityHandler.GravityMultiplier : 1;
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
