﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Character
{
    using Opsive.Shared.Events;
    using UnityEngine;

    /// <summary>
    /// Acts as a LookSource for any character that does not have a camera (or network) attached.
    /// </summary>
    public class LocalLookSource : MonoBehaviour, ILookSource
    {
        [Tooltip("The distance that the character should look ahead.")]
        [SerializeField] protected float m_LookDirectionDistance = 100;
        [Tooltip("The location of the look source. The character's head is a good value.")]
        [SerializeField] protected Transform m_LookTransform;
        [Tooltip("The object that the character should look at.")]
        [SerializeField] protected Transform m_Target;

        public GameObject GameObject { get { return m_GameObject; } }
        public Transform Transform { get { return m_Transform; } }
        public float LookDirectionDistance { get { return m_LookDirectionDistance; } }
        public Transform LookTransform { get { return m_LookTransform; } set { m_LookTransform = value; } }
        public float Pitch { get { return 0; } }
        public Transform Target { get { return m_Target; } set { m_Target = value; } }

        [System.NonSerialized] private GameObject m_GameObject;
        private Transform m_Transform;
        private bool m_Started;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_GameObject = gameObject;
            m_Transform = transform;

            if (m_LookTransform == null) {
                Animator animator;
                var modelManager = GetComponent<ModelManager>();
                if (modelManager != null) {
                    animator = modelManager.ActiveModel.GetComponent<Animator>();
                } else {
                    animator = GetComponentInChildren<AnimationMonitorBase>(true).GetComponent<Animator>();
                }

                if (animator != null && animator.isHuman) {
                    m_LookTransform = animator.GetBoneTransform(HumanBodyBones.Head);

                    EventHandler.RegisterEvent<GameObject>(m_GameObject, "OnCharacterSwitchModels", OnSwitchModels);
                }

                if (m_LookTransform == null) {
                    m_LookTransform = m_Transform;
                }
            }
        }

        /// <summary>
        /// The component has been enabled.
        /// </summary>
        private void OnEnable()
        {
            // Only attach the look source again if the component has already been started. By attaching the look source within enable it allows for the character to switch
            // look sources between a player-controlled character and an AI character.
            if (m_Started) {
                EventHandler.ExecuteEvent<ILookSource>(m_GameObject, "OnCharacterAttachLookSource", this);
            }
        }

        /// <summary>
        /// The component has been started.
        /// </summary>
        private void Start()
        {
            m_Started = true;
            EventHandler.ExecuteEvent<ILookSource>(m_GameObject, "OnCharacterAttachLookSource", this);
        }

        /// <summary>
        /// The component has been disabled.
        /// </summary>
        private void OnDisable()
        {
            EventHandler.ExecuteEvent<ILookSource>(m_GameObject, "OnCharacterAttachLookSource", null);
        }

        /// <summary>
        /// Returns the direction that the character is looking.
        /// </summary>
        /// <param name="characterLookDirection">Is the character look direction being retrieved?</param>
        /// <returns>The direction that the character is looking.</returns>
        public Vector3 LookDirection(bool characterLookDirection)
        {
            if (m_Target != null) {
                return (m_Target.position - m_LookTransform.position).normalized;
            }
            return m_Transform.forward;
        }

        /// <summary>
        /// Returns the direction that the character is looking.
        /// </summary>
        /// <param name="lookPosition">The position that the character is looking from.</param>
        /// <param name="characterLookDirection">Is the character look direction being retrieved?</param>
        /// <param name="layerMask">The LayerMask value of the objects that the look direction can hit.</param>
        /// <param name="includeRecoil">Should recoil be included in the look direction?</param>
        /// <param name="includeMovementSpread">Should the movement spread be included in the look direction?</param>
        /// <returns>The direction that the character is looking.</returns>
        public Vector3 LookDirection(Vector3 lookPosition, bool characterLookDirection, int layerMask, bool includeRecoil, bool includeMovementSpread)
        {
            if (m_Target != null) {
                return (m_Target.position - lookPosition).normalized;
            }
            return m_Transform.forward;
        }

        /// <summary>
        /// Returns the position of the look source.
        /// </summary>
        /// <param name="characterLookPosition">Is the character look position being retrieved?</param>
        /// <returns>The position of the look source.</returns>
        public Vector3 LookPosition(bool characterLookPosition)
        {
            return m_LookTransform.position;
        }

        /// <summary>
        /// The character's model has switched.
        /// </summary>
        /// <param name="activeModel">The active character model.</param>
        private void OnSwitchModels(GameObject activeModel)
        {
            var animator = activeModel.GetComponentInChildren<AnimationMonitorBase>(true).GetComponent<Animator>();

            if (animator != null && animator.isHuman) {
                m_LookTransform = animator.GetBoneTransform(HumanBodyBones.Head);
            }
        }

        /// <summary>
        /// The character has been destroyed.
        /// </summary>
        private void OnDestroy()
        {
            EventHandler.UnregisterEvent<GameObject>(m_GameObject, "OnCharacterSwitchModels", OnSwitchModels);
        }
    }
}