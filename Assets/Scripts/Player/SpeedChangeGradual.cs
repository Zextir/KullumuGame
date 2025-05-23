﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Character.Abilities
{
    using Opsive.UltimateCharacterController.Utility;
    using UnityEngine;

    /// <summary>
    /// The SpeedChange ability will update the controller's horizontal and forward movement values based on the multiplier. This value will then be used
    /// by the controller and Animator to change the character's speed.
    /// </summary>
    [AllowDuplicateTypes]
    [DefaultInputName("Change Speeds")]
    [DefaultState("Run")]
    [DefaultStartType(AbilityStartType.ButtonDownContinuous)]
    [DefaultStopType(AbilityStopType.ButtonUp)]
    public class SpeedChangeGradual : Ability
    {
        [Tooltip("The speed multiplier when the ability is active.")]
        [SerializeField] protected float m_SpeedChangeMultiplier = 2;
        [Tooltip("The minimum value the SpeedChangeMultiplier can change the InputVector value to.")]
        [SerializeField] protected float m_MinSpeedChangeValue = -2;
        [Tooltip("The maximum value the SpeedChangeMultiplier can change the InputVector to.")]
        [SerializeField] protected float m_MaxSpeedChangeValue = 2;
        [Tooltip("Specifies the value to set the Speed Animator parameter to.")]
        [SerializeField] protected float m_SpeedParameterValue = 2;
        [Tooltip("Does the ability require movement in order to stay active?")]
        [SerializeField] protected bool m_RequireMovement = true;

        [Tooltip("How long does it take for the change in speed to fully happen?")]
        [SerializeField, Range(0, 1)] protected float m_ChangeDuration = 0.7f;

        public float SpeedChangeMultiplier { get => m_SpeedChangeMultiplier; set => m_SpeedChangeMultiplier = value; }
        public float MinSpeedChangeValue { get => m_MinSpeedChangeValue; set => m_MinSpeedChangeValue = value; }
        public float MaxSpeedChangeValue { get => m_MaxSpeedChangeValue; set => m_MaxSpeedChangeValue = value; }
        public float SpeedParameter { get => m_SpeedParameterValue; set => m_SpeedParameterValue = value; }
        public bool RequireMovement { get => m_RequireMovement; set => m_RequireMovement = value; }

        public override bool IsConcurrent { get { return true; } }


        public float ChangeDuration { get => m_ChangeDuration; set => m_ChangeDuration = value; }



        private float changeTime = 0;
        private bool windingUp = true;
        private float currentSpeedChangeMultiplier = 1f;


        /// <summary>
        /// Called when the ablity is tried to be started. If false is returned then the ability will not be started.
        /// </summary>
        /// <returns>True if the ability can be started.</returns>
        public override bool CanStartAbility()
        {
            // An attribute may prevent the ability from starting.
            if (!base.CanStartAbility()) {
                return false;
            }

            return !m_RequireMovement || m_CharacterLocomotion.Moving;
        }

        /// <summary>
        /// Should the input be checked to ensure button up is using the correct value?
        /// </summary>
        /// <returns>True if the input should be checked.</returns>
        protected override bool ShouldCheckInput() { return false; }

        /// <summary>
        /// The ability has started.
        /// </summary>
        protected override void AbilityStarted()
        {
            base.AbilityStarted();
            windingUp = true;
            changeTime = 0;
            //currentSpeedChangeMultiplier = 1f;
            if (m_SpeedParameterValue != -1) {
                SetSpeedParameter(m_SpeedParameterValue);
            }
        }

        /// <summary>
        /// Updates the ability. Applies a multiplier to the horizontal and forward movement values.
        /// </summary>
        public override void Update()
        {
            base.Update();


            // If RequireMovement is true then the character must be moving in order for the ability to be active.
            if (m_RequireMovement && !m_CharacterLocomotion.Moving) {
                StopAbility(true);
                return;
            }

            if (windingUp)
                currentSpeedChangeMultiplier = Mathf.Lerp(1f, m_SpeedChangeMultiplier, changeTime / m_ChangeDuration);
            else
                currentSpeedChangeMultiplier = Mathf.Lerp(m_SpeedChangeMultiplier, 1f, changeTime / m_ChangeDuration);

            changeTime += Time.deltaTime; 

            var inputVector = m_CharacterLocomotion.InputVector;
            inputVector.x = Mathf.Clamp(inputVector.x * currentSpeedChangeMultiplier, m_MinSpeedChangeValue, m_MaxSpeedChangeValue);
            inputVector.y = Mathf.Clamp(inputVector.y * currentSpeedChangeMultiplier, m_MinSpeedChangeValue, m_MaxSpeedChangeValue);
            m_CharacterLocomotion.InputVector = inputVector;

            // The raw input vector should be updated as well. This allows other abilities to know if the character has a different speed.
            inputVector = m_CharacterLocomotion.RawInputVector;
            inputVector.x = Mathf.Clamp(inputVector.x * currentSpeedChangeMultiplier, m_MinSpeedChangeValue, m_MaxSpeedChangeValue);
            inputVector.y = Mathf.Clamp(inputVector.y * currentSpeedChangeMultiplier, m_MinSpeedChangeValue, m_MaxSpeedChangeValue);
            m_CharacterLocomotion.RawInputVector = inputVector;

            if (!windingUp) StopAbility();
        }

        public override bool CanStopAbility(bool force)
        {
            if (force) { return true; }

            if (windingUp)
            {
                windingUp = false;
                changeTime = 0;
            }
            return !windingUp && changeTime > m_ChangeDuration;

        }

        /// <summary>
        /// The ability has stopped running.
        /// </summary>
        /// <param name="force">Was the ability force stopped?</param>
        protected override void AbilityStopped(bool force)
        {
            base.AbilityStopped(force);

            SetSpeedParameter(0);
        }
    }
}