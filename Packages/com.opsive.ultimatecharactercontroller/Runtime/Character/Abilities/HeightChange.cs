﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Character.Abilities
{
    using Opsive.Shared.Events;
    using Opsive.UltimateCharacterController.Utility;
    using UnityEngine;

    /// <summary>
    /// The HeightChange ability allows the character pose to toggle between height changes. 
    /// </summary>
    [AllowDuplicateTypes]
    [DefaultInputName("Crouch")]
    [DefaultState("Crouch")]
    [DefaultStartType(AbilityStartType.ButtonDown)]
    [DefaultStopType(AbilityStopType.ButtonToggle)]
    [DefaultAbilityIndex(3)]
    [DefaultAbilityIntData(1)]
    public class HeightChange : Ability
    {
        [Tooltip("Is the ability a concurrent ability?")]
        [SerializeField] protected bool m_ConcurrentAbility = true;
        [Tooltip("Specifies the value to set the Height Animator parameter value to.")]
        [SerializeField] protected int m_Height = 1;
        [Tooltip("The amount to adjust the height of the CapsuleCollider by when active. This is only used if the character does not have an animator.")]
        [SerializeField] protected float m_CapsuleColliderHeightAdjustment = -0.4f;
        [Tooltip("Can the SpeedChange ability run while the HeightChange ability is active?")]
        [SerializeField] protected bool m_AllowSpeedChange;

        public bool ConcurrentAbility { get { return m_ConcurrentAbility; } set { m_ConcurrentAbility = value; } }
        public float CapsuleColliderHeightAdjustment { get { return m_CapsuleColliderHeightAdjustment; } set { m_CapsuleColliderHeightAdjustment = value; } }
        public bool AllowSpeedChange { get { return m_AllowSpeedChange; } set { m_AllowSpeedChange = value; } }

        public override bool IsConcurrent { get { return m_ConcurrentAbility; } }

        private Vector3[] m_StartColliderCenter;
        private float[] m_CapsuleColliderHeight;
        private Collider[] m_OverlapColliders = new Collider[1];

        /// <summary>
        /// Initialize the collider storage arrays.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Initialize the arrays which will help determine if the ability can stop.
            m_StartColliderCenter = new Vector3[m_CharacterLocomotion.Colliders.Length];
            m_CapsuleColliderHeight = new float[m_CharacterLocomotion.Colliders.Length];
            var capsuleColliderCount = 0;
            for (int i = 0; i < m_CharacterLocomotion.Colliders.Length; ++i) {
                if (m_CharacterLocomotion.Colliders[i] is CapsuleCollider) {
                    capsuleColliderCount++;
                }
            }

            // The counts won't be equal if the character has non-CapsuleCollider colliders.
            if (capsuleColliderCount != m_CapsuleColliderHeight.Length) {
                System.Array.Resize(ref m_CapsuleColliderHeight, capsuleColliderCount);
            }
        }

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

            return m_CharacterLocomotion.Grounded;
        }

        /// <summary>
        /// The ability has started.
        /// </summary>
        protected override void AbilityStarted()
        {
            base.AbilityStarted();

            // Colliders may have been added since the last time the ability started.
            if (m_CharacterLocomotion.ColliderCount > m_CapsuleColliderHeight.Length) {
                System.Array.Resize(ref m_CapsuleColliderHeight, m_CharacterLocomotion.ColliderCount);
                System.Array.Resize(ref m_StartColliderCenter, m_CharacterLocomotion.ColliderCount);
            }

            // When the ability stops it'll check to ensure there is room for the colliders. Save the collider center/height so this check can be made.
            var capsuleColliderCount = 0;
            for (int i = 0; i < m_CharacterLocomotion.ColliderCount; ++i) {
                if (m_CharacterLocomotion.Colliders[i] is CapsuleCollider) {
                    var capsuleCollider = (m_CharacterLocomotion.Colliders[i] as CapsuleCollider);
                    // If the collider isn't enabled when the ability started then the value isn't correct. Ignore the value.
                    if (!capsuleCollider.gameObject.activeInHierarchy) {
                        m_StartColliderCenter[i].y = float.MaxValue;
                        continue;
                    }
                    m_CapsuleColliderHeight[capsuleColliderCount] = capsuleCollider.height;
                    m_StartColliderCenter[i] = capsuleCollider.center;
                    capsuleColliderCount++;
                } else {
                    if (m_CharacterLocomotion.Colliders[i] is SphereCollider) {
                        m_StartColliderCenter[i] = (m_CharacterLocomotion.Colliders[i] as SphereCollider).center;
                    } else { // BoxCollider.
                        m_StartColliderCenter[i] = (m_CharacterLocomotion.Colliders[i] as BoxCollider).center;
                    }
                }
            }

            if (m_Height != -1) {
                SetHeightParameter(m_Height);
            }
            EventHandler.ExecuteEvent(m_GameObject, "OnHeightChangeAdjustHeight", m_CapsuleColliderHeightAdjustment);
        }

        /// <summary>
        /// Can the ability be stopped?
        /// </summary>
        /// <param name="force">Should the ability be force stopped?</param>
        /// <returns>True if the ability can be stopped.</returns>
        public override bool CanStopAbility(bool force)
        {
            if (force) { return true; }

            if (!m_CharacterLocomotion.UsingHorizontalCollisionDetection || !m_CharacterLocomotion.DetectVerticalCollisions) {
                return true;
            }

            var collisionEnabled = m_CharacterLocomotion.CollisionLayerEnabled;
            m_CharacterLocomotion.EnableColliderCollisionLayer(false);
            var keepActive = false;

            // The ability can't stop if there isn't enough room for the character to occupy their original height.
            var capsuleColliderCount = 0;
            for (int i = 0; i < m_CharacterLocomotion.ColliderCount; ++i) {
                // Determine if the collider would intersect any objects.
                if (m_CharacterLocomotion.Colliders[i] is CapsuleCollider) {
                    if (m_StartColliderCenter[i].y == float.MaxValue) {
                        continue;
                    }
                    var capsuleCollider = m_CharacterLocomotion.Colliders[i] as CapsuleCollider;
                    var radiusMultiplier = MathUtility.ColliderScaleMultiplier(capsuleCollider);
                    Vector3 startEndCap, endEndCap;
                    MathUtility.CapsuleColliderEndCaps(m_CapsuleColliderHeight[capsuleColliderCount] * MathUtility.CapsuleColliderHeightMultiplier(capsuleCollider),
                                                                (capsuleCollider.radius - m_CharacterLocomotion.ColliderSpacing) * radiusMultiplier, Vector3.Scale(m_StartColliderCenter[i], capsuleCollider.transform.lossyScale), MathUtility.CapsuleColliderDirection(capsuleCollider),
                                                                capsuleCollider.transform.position, capsuleCollider.transform.rotation, out startEndCap, out endEndCap);
                    // If there is overlap then the ability can't stop.
                    if (Physics.OverlapCapsuleNonAlloc(startEndCap, endEndCap, (capsuleCollider.radius - m_CharacterLocomotion.ColliderSpacing * 2) * radiusMultiplier, m_OverlapColliders, m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore) > 0) {
                        keepActive = true;
                        break;
                    }
                    capsuleColliderCount++;
                } else {
                    if (!m_CharacterLocomotion.Colliders[i].gameObject.activeInHierarchy) {
                        continue;
                    }

                    if (m_CharacterLocomotion.Colliders[i] is SphereCollider) {
                        var sphereCollider = m_CharacterLocomotion.Colliders[i] as SphereCollider;
                        // If there is overlap then the ability can't stop.
                        if (Physics.OverlapSphereNonAlloc(sphereCollider.transform.TransformPoint(m_StartColliderCenter[i]), (sphereCollider.radius - m_CharacterLocomotion.ColliderSpacing * 2) * MathUtility.ColliderScaleMultiplier(sphereCollider),
                                                                        m_OverlapColliders, m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore) > 0) {
                            keepActive = true;
                            break;
                        }
                    } else { // BoxCollider.
                        var boxCollider = m_CharacterLocomotion.Colliders[i] as BoxCollider;
                        // If there is overlap then the ability can't stop.
                        if (Physics.OverlapBoxNonAlloc(boxCollider.transform.TransformPoint(m_StartColliderCenter[i]), boxCollider.size / 2 * MathUtility.ColliderScaleMultiplier(boxCollider),
                                                                        m_OverlapColliders, boxCollider.transform.rotation, m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore) > 0) {
                            keepActive = true;
                            break;
                        }
                    }
                }
            }

            m_CharacterLocomotion.EnableColliderCollisionLayer(collisionEnabled);
            if (keepActive) {
                return false;
            }

            return base.CanStopAbility(force);
        }

        /// <summary>
        /// Called when another ability is attempting to start and the current ability is active.
        /// Returns true or false depending on if the new ability should be blocked from starting.
        /// </summary>
        /// <param name="startingAbility">The ability that is starting.</param>
        /// <returns>True if the ability should be blocked.</returns>
        public override bool ShouldBlockAbilityStart(Ability startingAbility)
        {
            return (!m_AllowSpeedChange && startingAbility is SpeedChange) || startingAbility is StoredInputAbilityBase;
        }

        /// <summary>
        /// Called when the current ability is attempting to start and another ability is active.
        /// Returns true or false depending on if the active ability should be stopped.
        /// </summary>
        /// <param name="activeAbility">The ability that is currently active.</param>
        public override bool ShouldStopActiveAbility(Ability activeAbility)
        {
            return (!m_AllowSpeedChange && activeAbility is SpeedChange);
        }

        /// <summary>
        /// The ability has stopped running.
        /// </summary>
        /// <param name="force">Was the ability force stopped?</param>
        protected override void AbilityStopped(bool force)
        {
            AbilityStopped(force, true);
        }

        /// <summary>
        /// The ability has stopped running.
        /// </summary>
        /// <param name="force">Was the ability force stopped?</param>
        /// <param name="resetCapsuleColliderHeight">Should the capsule collider height be reset?</param>
        protected void AbilityStopped(bool force, bool resetCapsuleColliderHeight)
        {
            base.AbilityStopped(force);

            if (m_Height != -1) {
                SetHeightParameter(0);
            }
            if (resetCapsuleColliderHeight) {
                EventHandler.ExecuteEvent(m_GameObject, "OnHeightChangeAdjustHeight", -m_CapsuleColliderHeightAdjustment);
            }
        }
    }
}