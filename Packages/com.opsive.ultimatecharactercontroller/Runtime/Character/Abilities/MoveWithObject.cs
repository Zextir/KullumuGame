﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Character.Abilities
{
    using UnityEngine;

    /// <summary>
    /// Moves with the specified object.
    /// </summary>
    [DefaultStopType(AbilityStopType.Automatic)]
    public class MoveWithObject : Ability
    {
        [Tooltip("The object that the character should move with.")]
        [SerializeField] protected Transform m_Target;

        public Transform Target { get { return m_Target; }
            set {
                var prevTarget = m_Target;
                m_Target = value;

                if (m_Target != null && m_Target.GetComponent<Game.KinematicObject>() == null) {
                    Debug.Log($"Error: The target {Target.name} does not have the Kinematic Object component. See the Move With Object documentation for more information.", Target);
                    m_Target = null;
                }

                if (IsActive && prevTarget != null && m_Target != prevTarget) {
                    m_CharacterLocomotion.SetMovingPlatform(m_Target);
                }
            }
        }

        public override bool IsConcurrent { get { return true; } }

        /// <summary>
        /// Initailize the default values.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Set the property so it goes through the error check.
            if (m_Target != null) {
                Target = m_Target;
            }
        }

        /// <summary>
        /// Can the ability be started?
        /// </summary>
        /// <returns>True if the ability can be started.</returns>
        public override bool CanStartAbility()
        {
            if (m_Target == null) {
                return false;
            }

            return base.CanStartAbility();
        }

        /// <summary>
        /// The ability has started.
        /// </summary>
        protected override void AbilityStarted()
        {
            base.AbilityStarted();

            m_CharacterLocomotion.SetMovingPlatform(m_Target);
        }

        /// <summary>
        /// Can the ability be stopped?
        /// </summary>
        /// <param name="force">Should the ability be force stopped?</param>
        /// <returns>True if the ability can be stopped.</returns>
        public override bool CanStopAbility(bool force)
        {
            if (force) { return true; }

            if (m_Target != null) {
                return false;
            }

            return base.CanStopAbility(force);
        }

        /// <summary>
        /// The ability has stopped running.
        /// </summary>
        /// <param name="force">Was the ability force stopped?</param>
        protected override void AbilityStopped(bool force)
        {
            base.AbilityStopped(force);

            m_CharacterLocomotion.SetMovingPlatform(null, false);
        }
    }
}