﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Character
{
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
    using Opsive.Shared.Networking;
#endif
    using Opsive.Shared.StateSystem;
    using Opsive.Shared.Utility;
    using Opsive.UltimateCharacterController.SurfaceSystem;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// The CharacterFootEffects component will detect when a footstep has occurred.
    /// </summary>
    public class CharacterFootEffects : StateBehavior
    {
        /// <summary>
        /// Specifies the properties of a foot.
        /// </summary>
        [System.Serializable]
        public struct Foot
        {
            [Tooltip("A reference to the foot Transform. This reference should be pointing in the character's forward direction so the foot is placed correctly.")]
            [SerializeField] private Transform m_Object;
#pragma warning disable 0649
            [Tooltip("The grouping of the foot. If for example the character is a dog, the two left feet will be in the first group while the right feet are in the second group.")]
            [SerializeField] private int m_Group;
#pragma warning restore 0649
            [Tooltip("Should the footprint be flipped for this foot?")]
            [SerializeField] private bool m_FlippedFootprint;

            public Transform Object { get { return m_Object; } set { m_Object = value; } }
            public int Group { get { return m_Group; } }
            public bool FlippedFootprint { get { return m_FlippedFootprint; } set { m_FlippedFootprint = value; } }
        }

        /// <summary>
        /// Specifies how the footsteps are placed.
        /// </summary>
        public enum FootstepPlacementMode
        {
            BodyStep,       // The footsteps are determined by the vertical height of the character's feet.
            Trigger,        // The footsteps are placed by a trigger on each foot.
            FixedInterval,  // The footsteps are placed at a regular interval while the character is moving.
            CameraBob,      // The footsteps are place according to the camera's bob.
            None
        }
        
        [Tooltip("The Surface Impact triggered when there is a footstep.")]
        [SerializeField] protected SurfaceImpact m_SurfaceImpact;
        [Tooltip("Specifies how the footsteps are placed.")]
        [SerializeField] protected FootstepPlacementMode m_FootstepMode;
        [Tooltip("The character's feet. Only used with the BodyStep and Trigger placement modes.")]
        [SerializeField] protected Foot[] m_Feet;
        [Tooltip("If using the BodyStep mode, specifies the number of frames that the foot must be moving in order for it to be checked if it is down.")]
        [SerializeField] protected int m_MoveDirectionFrameCount = 7;
        [Tooltip("Specifies an offset for when a raycast is cast to determine if the character's foot is considered down.")]
        [SerializeField] protected float m_FootOffset = 0.11f;
        [Tooltip("If using the Trigger mode, specifies the amount of time that must elapse before another effect can be spawned.")]
        [SerializeField] protected float m_MinTriggerInterval = 0.1f;
        [Tooltip("If using the Trigger mode, specifies if the character needs to be moving in order for the footstep to be placed.")]
        [SerializeField] protected bool m_RequireMovement = true;
        [Tooltip("If using the Trigger mode, specifies if the footstep should not repeat.")]
        [SerializeField] protected bool m_RequireAlternatingFeet = true;
        [Tooltip("If using the FixedInterval mode, specifies how often the footsteps occur when the character is moving.")]
        [SerializeField] protected float m_FixedInterval = 0.3f;
        [Tooltip("If using the CameraBob mode, specifies the minimum time that must elapse before another footstep occurs.")]
        [SerializeField] protected float m_MinBobInterval = 0.2f;

        public FootstepPlacementMode FootstepMode { get { return m_FootstepMode; }
            set
            {
                if (m_FootstepMode != value) {
                    m_FootstepMode = value;
                    if (Application.isPlaying) {
                        PrepareVerticalOffsetLists();
                    }
                }
            }
        }
        public SurfaceImpact SurfaceImpact { get { return m_SurfaceImpact; } set { m_SurfaceImpact = value; } }
        [NonSerialized] public Foot[] Feet { get { return m_Feet; } set { m_Feet = value; } }
        public int MoveDirectionFrameCount { get { return m_MoveDirectionFrameCount; } set { m_MoveDirectionFrameCount = value; } }
        public float FootOffset { get { return m_FootOffset; } set { m_FootOffset = value; } }
        public float MinTriggerInterval { get { return m_MinTriggerInterval; } set { m_MinTriggerInterval = value; } }
        public bool RequireMovement { get { return m_RequireMovement; } set { m_RequireMovement = value; } }
        public bool RequireAlternatingFeet { get { return m_RequireAlternatingFeet; } set { m_RequireAlternatingFeet = value; } }
        public float FixedInterval { get { return m_FixedInterval; } set { m_FixedInterval = value; } }
        public float MinBobInterval { get { return m_MinBobInterval; } set { m_MinBobInterval = value; } }

        [System.NonSerialized] private GameObject m_GameObject;
        private Transform m_Transform;
        protected UltimateCharacterLocomotion m_CharacterLocomotion;
        protected CharacterLayerManager m_CharacterLayerManager;
        private ILookSource m_LookSource;
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
        private INetworkInfo m_NetworkInfo;
        private bool m_CanPlaceFootstep;
        public bool CanPlaceFootstep { set { m_CanPlaceFootstep = value; } }
#endif

        private List<List<Transform>> m_FeetGrouping = new List<List<Transform>>();
        private HashSet<Transform> m_FlippedFootprints = new HashSet<Transform>();
        private float[] m_VerticalOffset;
        private float[] m_LastVerticalOffset;
        private int[] m_UpCount;
        private int[] m_DownCount;
        private Transform m_LastFootDown;
        private float m_LastFootstepTime = float.MinValue;
        private int m_FootstepGroupIndex;
        private float m_TeleportTime;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            if (!Game.CharacterInitializer.AutoInitialization) {
                Game.CharacterInitializer.Instance.OnAwake += AwakeInternal;
                return;
            }

            AwakeInternal();
        }

        /// <summary>
        /// Internal method which initializes the default values.
        /// </summary>
        private void AwakeInternal()
        {
            if (Game.CharacterInitializer.Instance) {
                Game.CharacterInitializer.Instance.OnAwake -= AwakeInternal;
            }

            base.Awake();

            m_Transform = transform;
            m_CharacterLocomotion = gameObject.GetComponentInParent<UltimateCharacterLocomotion>();
            m_GameObject = m_CharacterLocomotion.gameObject;
            m_CharacterLayerManager = m_CharacterLocomotion.gameObject.GetCachedComponent<CharacterLayerManager>();
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
            m_NetworkInfo = m_CharacterLocomotion.gameObject.GetCachedComponent<INetworkInfo>();
#endif
            if (m_Feet == null) {
                InitializeHumanoidFeet(false);
            }

            if (m_Feet != null && m_Feet.Length != 0) {
                for (int i = 0; i < m_Feet.Length; ++i) {
                    if (m_Feet[i].Object == null) {
                        continue;
                    }

                    // The FeetGrouping list should be at least the size of the current group index.
                    while (m_Feet[i].Group >= m_FeetGrouping.Count) {
                        m_FeetGrouping.Add(new List<Transform>());
                    }
                    m_FeetGrouping[m_Feet[i].Group].Add(m_Feet[i].Object);
                    // The Transform should only be added to the set if the footprint is flipped. If the Transform is not in the set then the footprint is not flipped.
                    if (m_Feet[i].FlippedFootprint) {
                        m_FlippedFootprints.Add(m_Feet[i].Object);
                    }
                }
            } else {
                m_FeetGrouping.Add(new List<Transform>());
                m_FeetGrouping[0].Add(m_Transform);
            }

            if (m_FootstepMode == FootstepPlacementMode.Trigger || m_FootstepMode == FootstepPlacementMode.CameraBob || m_FootstepMode == FootstepPlacementMode.None) {
                // The component doesn't need to be enabled if using a trigger - the FootstepTrigger component will detect the footstep. The CameraBob will enable the component
                // when the look source is attached.
                enabled = false;
            } else if (m_Feet != null) {
                PrepareVerticalOffsetLists();
            }

            m_TeleportTime = Time.realtimeSinceStartup;
            EventHandler.RegisterEvent<ILookSource>(m_GameObject, "OnCharacterAttachLookSource", OnAttachLookSource);
            EventHandler.RegisterEvent<bool>(m_GameObject, "OnCharacterMoving", OnMoving);
            EventHandler.RegisterEvent<bool>(m_GameObject, "OnCharacterImmediateTransformChange", OnImmediateTransformChange);
        }

        /// <summary>
        /// Tries to initialize the feet if the character is a humanoid.
        /// </summary>
        /// <param name="addAudioSource">Should an AudioSource be added to the feet?</param>
        public void InitializeHumanoidFeet(bool addAudioSource)
        {
            if (m_Feet != null) {
                return;
            }

            // Add the humanoid feet if the character is a humanoid.
            var animator = gameObject.GetComponent<Animator>();
            if (animator != null) {
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null) {
                    m_Feet = new CharacterFootEffects.Foot[2];
                    // Try to use the toes if the bones have been mapped.
                    var toe = animator.GetBoneTransform(HumanBodyBones.LeftToes);
                    if (toe != null) {
                        m_Feet[0].Object = toe;
                    } else {
                        m_Feet[0].Object = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    }
                    if (addAudioSource && m_Feet[0].Object != null && m_Feet[0].Object.gameObject.GetComponent<AudioSource>() == null) {
                        var audioSource = m_Feet[0].Object.gameObject.AddComponent<AudioSource>();
                        audioSource.volume = 0.4f;
                        audioSource.playOnAwake = false;
                        audioSource.spatialBlend = 1;
                        audioSource.maxDistance = 20;
                    }
                    toe = animator.GetBoneTransform(HumanBodyBones.RightToes);
                    if (toe != null) {
                        m_Feet[1].Object = toe;
                    } else {
                        m_Feet[1].Object = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                    }
                    if (addAudioSource && m_Feet[1].Object != null && m_Feet[1].Object.gameObject.GetComponent<AudioSource>() == null) {
                        var audioSource = m_Feet[1].Object.gameObject.AddComponent<AudioSource>();
                        audioSource.volume = 0.4f;
                        audioSource.playOnAwake = false;
                        audioSource.spatialBlend = 1;
                        audioSource.maxDistance = 20;
                    }
                    m_Feet[1].FlippedFootprint = true;
                }
            }
        }

        /// <summary>
        /// A new ILookSource object has been attached to the character.
        /// </summary>
        /// <param name="lookSource">The ILookSource object attached to the character.</param>
        private void OnAttachLookSource(ILookSource lookSource)
        {
            if (m_LookSource == null) {
                EventHandler.RegisterEvent<bool>(m_GameObject, "OnCharacterChangePerspectives", OnChangePerspectives);
            }

            m_LookSource = lookSource;

            if (m_LookSource == null) {
                EventHandler.UnregisterEvent<bool>(m_GameObject, "OnCharacterChangePerspectives", OnChangePerspectives);
            }

            PrepareVerticalOffsetLists();
        }

        /// <summary>
        /// The character perspective between first and third person has changed.
        /// </summary>
        /// <param name="firstPersonPerspective">Is the character in a first person perspective?</param>
        private void OnChangePerspectives(bool firstPersonPerspective)
        {
            PrepareVerticalOffsetLists();
        }

        /// <summary>
        /// Initializes the vertical offset arrays.
        /// </summary>
        private void PrepareVerticalOffsetLists()
        {
            if (m_FootstepMode == FootstepPlacementMode.Trigger || m_FootstepMode == FootstepPlacementMode.FixedInterval || m_FootstepMode == FootstepPlacementMode.None) {
                return;
            }

            var count = m_FootstepMode == FootstepPlacementMode.CameraBob ? 1 : m_Feet.Length;
            if (m_VerticalOffset == null) {
                m_VerticalOffset = new float[count];
                m_LastVerticalOffset = new float[count];
            } else if (m_VerticalOffset.Length != count){
                System.Array.Resize(ref m_VerticalOffset, count);
                System.Array.Resize(ref m_LastVerticalOffset, count);
            } else {
                // Return early if the array lenth is the same. The arrays are already setup for the current footstep mode.
                return;
            }

            // Setup the default values.
            if (m_FootstepMode == FootstepPlacementMode.CameraBob) {
                if (m_LookSource != null) {
                    m_LastVerticalOffset[0] = m_VerticalOffset[0] = m_Transform.InverseTransformPoint(m_LookSource.LookPosition(true)).y;
                }
            } else { // Body Step.
                if (m_UpCount == null) {
                    m_UpCount = new int[count];
                    m_DownCount = new int[count];
                } else if (m_UpCount.Length != count) {
                    System.Array.Resize(ref m_UpCount, count);
                    System.Array.Resize(ref m_DownCount, count);
                }
                for (int i = 0; i < m_Feet.Length; ++i) {
                    m_LastVerticalOffset[i] = m_VerticalOffset[i] = m_Transform.InverseTransformPoint(m_Feet[i].Object.position).y;
                    m_UpCount[i] = 0;
                }
            }
            enabled = true;
        }

        /// <summary>
        /// Detect the footstep.
        /// </summary>
        private void FixedUpdate()
        {
            if (m_FootstepMode == FootstepPlacementMode.None) {
                return;
            }

#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
            if (m_NetworkInfo != null && !m_NetworkInfo.HasAuthority() && !m_NetworkInfo.IsLocalPlayer()) {
                // The transform interpolator will set CanPlaceFootstep for remote clients. This will indicate if the character moved.
                // The character should also be grounded.
                if (!m_CanPlaceFootstep || !Physics.Raycast(m_Transform.position, -m_Transform.up, (m_CharacterLocomotion.SkinWidth + m_CharacterLocomotion.ColliderSpacing), m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore)) {
                    return;
                }
                m_CanPlaceFootstep = false;
            } else {
#endif
                // The character has to be grounded and moving in order to be able to place footsteps.
                if (!m_CharacterLocomotion.Grounded || !m_CharacterLocomotion.Moving) {
                    return;
                }
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
            }
#endif

            switch (m_FootstepMode) {
                case FootstepPlacementMode.BodyStep:
                    DetectBodyStep();
                    break;
                case FootstepPlacementMode.FixedInterval:
                    UpdateFixedInterval();
                    break;
                case FootstepPlacementMode.CameraBob:
                    DetectCameraBob();
                    break;
            }
        }

        /// <summary>
        /// A body step is detected when the foot touches the ground.
        /// </summary>
        private void DetectBodyStep()
        {
            for (int i = 0; i < m_Feet.Length; ++i) {
                var verticalOffset = m_Transform.InverseTransformPoint(m_Feet[i].Object.position).y;
                // A footstep can be detected when the foot is moving down after moving up for a minimum number of frames.
                if (verticalOffset < m_LastVerticalOffset[i] && m_CharacterLocomotion.LocalVelocity.sqrMagnitude > m_CharacterLocomotion.ColliderSpacing) {
                    // The downward foot must have moved in the same direction for at least the specified number of frames.
                    if ((m_UpCount[i] > (m_MoveDirectionFrameCount  / (m_CharacterLocomotion.TimeScale * Time.timeScale)) || m_DownCount[i] > (m_MoveDirectionFrameCount / (m_CharacterLocomotion.TimeScale * Time.timeScale))) && 
                            m_Feet[i].Object != m_LastFootDown && FootStep(m_Feet[i].Object, m_Feet[i].FlippedFootprint)) {
                        m_LastFootDown = m_Feet[i].Object;
                        m_LastFootstepTime = Time.time;
                        for (int j = 0; j < m_UpCount.Length; ++j) {
                            m_UpCount[j] = 0;
                            m_DownCount[j] = 0;
                        }
                    } else {
                        m_DownCount[i]++;
                    }
                } else if (verticalOffset > m_LastVerticalOffset[i]) {
                    m_UpCount[i]++;
                }
                m_LastVerticalOffset[i] = verticalOffset;
            }
        }

        /// <summary>
        /// Places a group footstep at a fixed interval.
        /// </summary>
        private void UpdateFixedInterval()
        {
            // Don't place a footstep if there was recently a footstep.
            if (m_LastFootstepTime + m_FixedInterval * m_CharacterLocomotion.TimeScale > Time.time) {
                return;
            }

            // Place a footprint with the current group.
            GroupFootStep();
            m_LastFootstepTime = Time.time;
        }

        /// <summary>
        /// A camera bob is detected when the look source point is at its lowest point relative to the character's transform.
        /// </summary>
        private void DetectCameraBob()
        {
            // In order for the lowest point to be detected the look source position must be decreasing. When the look source position
            // starts to increase again then it was at the lowest point.
            var verticalOffset = m_Transform.InverseTransformPoint(m_LookSource.LookPosition(true)).y;
            if (m_LastVerticalOffset[0] >= m_VerticalOffset[0] && verticalOffset > m_VerticalOffset[0] && m_LastFootstepTime + (m_MinBobInterval * m_CharacterLocomotion.TimeScale) < Time.time) {
                GroupFootStep();
                m_LastFootstepTime = Time.time;
            }
            m_LastVerticalOffset[0] = m_VerticalOffset[0];
            m_VerticalOffset[0] = verticalOffset;
        }

        /// <summary>
        /// The active group index caused a footstep.
        /// </summary>
        private void GroupFootStep()
        {
            var grouping = m_FeetGrouping[m_FootstepGroupIndex];
            for (int i = 0; i < grouping.Count; ++i) {
                FootStep(grouping[i], m_FlippedFootprints.Contains(grouping[i]));
            }
            // Move to the next index which will cause the footstep.
            m_FootstepGroupIndex = (m_FootstepGroupIndex + 1) % m_FeetGrouping.Count;
        }

        /// <summary>
        /// A footstep has occurred from the trigger collider.
        /// </summary>
        /// <param name="foot">The foot which caused the footstep.</param>
        /// <param name="flipFootprint">Should the footprint be flipped?</param>
        /// <returns>True if the footstep was successfully planted.</returns>
        public bool TriggerFootStep(Transform foot, bool flipFootprint)
        {
            var moving = new Vector2(m_CharacterLocomotion.LocalVelocity.x, m_CharacterLocomotion.LocalVelocity.z).sqrMagnitude > 0.01f;
            if (!moving && m_RequireMovement) {
                return false;
            }

            // Don't place a footstep if:
            // - The character isn't moving but is required to move.
            // - There was recently a footstep.
            if ((!moving && m_RequireMovement) || m_LastFootstepTime + m_MinTriggerInterval > Time.time || (m_RequireAlternatingFeet && foot == m_LastFootDown)) {
                return false;
            }

            // Don't place a footstep when the character teleports.
            if (Time.realtimeSinceStartup <= m_TeleportTime + Time.fixedDeltaTime * 3) {
                return false;
            }

            if (FootStep(foot, flipFootprint)) {
                m_LastFootDown = foot;
                m_LastFootstepTime = Time.time;
                return true;
            }
            return false;
        }


        /// <summary>
        /// A footstep has occurred. Notify the SurfaceManager.
        /// </summary>
        /// <param name="foot">The foot which caused the footstep.</param>
        /// <param name="flipFootprint">Should the footprint be flipped?</param>
        /// <returns>True if the footstep was successfully planted.</returns>
        public virtual bool FootStep(Transform foot, bool flipFootprint)
        {
            // A RaycastHit is required for the SurfaceManager.
            RaycastHit hit;
            if (Physics.Raycast(foot.position + m_Transform.up * 0.2f, -m_Transform.up, out hit, 0.21f + m_FootOffset + Mathf.Abs(m_CharacterLocomotion.LocalVelocity.y / Time.deltaTime), m_CharacterLayerManager.IgnoreInvisibleCharacterWaterLayers, QueryTriggerInteraction.Ignore)) {
                SurfaceManager.SpawnEffect(hit, m_SurfaceImpact, m_CharacterLocomotion.GravityDirection, m_CharacterLocomotion.TimeScale, foot.gameObject, m_Transform.forward, flipFootprint);
                return true;
            }
            return false;
        }

        /// <summary>
        /// The character has started to or stopped moving.
        /// </summary>
        /// <param name="moving">Is the character moving?</param>
        private void OnMoving(bool moving)
        {
            // When the character starts to move reset the footstep time so footsteps don't appear immediately after the character starts to moving.
            if (moving) {
                m_LastFootstepTime = Time.time;
            }
        }

        /// <summary>
        /// The character's position or rotation has been teleported.
        /// </summary>
        /// <param name="snapAnimator">Should the animator be snapped?</param>
        private void OnImmediateTransformChange(bool snapAnimator)
        {
            m_TeleportTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Callback when the StateManager has changed the active state on the current object.
        /// </summary>
        public override void StateChange()
        {
            base.StateChange();

            // The component doesn't need to be active if the footsteps are being triggered from a trigger.
            enabled = m_FootstepMode != FootstepPlacementMode.Trigger;
        }

        /// <summary>
        /// Resets the component from the inspector.
        /// </summary>
        private void OnReset()
        {
            if (GetComponentInChildren<AnimationMonitorBase>() == null) {
                FootstepMode = FootstepPlacementMode.FixedInterval;
            }
        }

        /// <summary>
        /// The character has been destroyed.
        /// </summary>
        private void OnDestroy()
        {
            EventHandler.UnregisterEvent<ILookSource>(m_GameObject, "OnCharacterAttachLookSource", OnAttachLookSource);
            EventHandler.UnregisterEvent<bool>(m_GameObject, "OnCharacterMoving", OnMoving);
            EventHandler.UnregisterEvent<bool>(m_GameObject, "OnCharacterImmediateTransformChange", OnImmediateTransformChange);
            EventHandler.UnregisterEvent<bool>(m_GameObject, "OnCharacterChangePerspectives", OnChangePerspectives);
        }
    }
}