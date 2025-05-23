﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Camera.ViewTypes
{
    using Opsive.Shared.Game;
    using Opsive.Shared.StateSystem;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateCharacterController.Utility;
    using System;
    using UnityEngine;

    /// <summary>
    /// Base class for the objects which describe how the camera moves.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    [Serializable]
    public abstract class ViewType : StateObject
    {
        [Tooltip("The field of view of the camera.")]
        [Range(1, 179)] [SerializeField] protected float m_FieldOfView = 70f;
        [Tooltip("The damping time of the field of view angle when changed.")]
        [Range(0, 5)] [SerializeField] protected float m_FieldOfViewDamping = 0.2f;

        public abstract bool FirstPersonPerspective { get; }
        public abstract float Pitch { get; }
        public abstract float Yaw { get; }
        public abstract Quaternion BaseCharacterRotation { get; }
        public abstract float LookDirectionDistance { get; }

        protected CameraController m_CameraController;
        protected Transform m_Transform;
        protected GameObject m_GameObject;
        protected Camera m_Camera;
        protected GameObject m_Character;
        protected Transform m_CharacterTransform;
        protected UltimateCharacterLocomotion m_CharacterLocomotion;
        protected CharacterLayerManager m_CharacterLayerManager;

        protected float m_FieldOfViewChangeTime;

        public virtual GameObject GameObject { get => m_GameObject; }
        public virtual Transform Transform { get => m_Transform; }
        public virtual GameObject CameraGameObject { get => m_GameObject; }
        public virtual Transform CameraTransform { get => m_Transform; }
        public virtual bool UseSmoothOffset { get => false; }

        public Quaternion CharacterRotation { get => m_CharacterLocomotion.Rotation; }
        public Vector3 CharacterPosition { get => m_CharacterLocomotion.Position; }

        /// <summary>
        /// Initializes the view type to the specified camera controller.
        /// </summary>
        /// <param name="cameraController">The camera controller to initialize the view type to.</param>
        public virtual void Initialize(CameraController cameraController)
        {
            Initialize(cameraController.gameObject);

            m_CameraController = cameraController;
            m_Transform = cameraController.transform;
            m_GameObject = cameraController.gameObject;
            m_Camera = m_GameObject.GetCachedComponent<Camera>();
        }

        /// <summary>
        /// Method called by MonoBehaviour.Awake. Can be used for initialization.
        /// </summary>
        public virtual void Awake() { }

        /// <summary>
        /// Attaches the view type to the specified character.
        /// </summary>
        /// <param name="character">The character to attach the camera to.</param>
        public virtual void AttachCharacter(GameObject character)
        {
            m_Character = character;
            if (m_Character == null) {
                m_CharacterTransform = null;
                m_CharacterLocomotion = null;
                m_CharacterLayerManager = null;
            } else {
                m_CharacterTransform = character.transform;
                m_CharacterLocomotion = character.GetCachedComponent<UltimateCharacterLocomotion>();
                m_CharacterLayerManager = character.GetCachedComponent<CharacterLayerManager>();
            }
        }

        /// <summary>
        /// Resets the ViewType's character rotation and springs.
        /// </summary>
        /// <param name="characterRotation">The rotation of the character.</param>
        public virtual void Reset(Quaternion characterRotation) { }

        /// <summary>
        /// Resets the View Type rotation parameters to the specified values.
        /// </summary>
        /// <param name="pitch">The pitch of the camera (in degrees).</param>
        /// <param name="yaw">The yaw of the camera (in degrees).</param>
        /// <param name="baseCharacterRotation">The rotation of the character.</param>
        public virtual void ResetRotation(float pitch, float yaw, Quaternion baseCharacterRotation) { }

        /// <summary>
        /// Sets the crosshairs to the specified transform.
        /// </summary>
        /// <param name="crosshairs">The transform of the crosshairs.</param>
        public virtual void SetCrosshairs(Transform crosshairs) { }

        /// <summary>
        /// Returns the delta rotation caused by the crosshairs.
        /// </summary>
        /// <returns>The delta rotation caused by the crosshairs.</returns>
        public virtual Quaternion GetCrosshairsDeltaRotation() { return Quaternion.identity; }

        /// <summary>
        /// The view type has changed.
        /// </summary>
        /// <param name="activate">Should the current view type be activated?</param>
        /// <param name="pitch">The pitch of the camera (in degrees).</param>
        /// <param name="yaw">The yaw of the camera (in degrees).</param>
        /// <param name="baseCharacterRotation">The rotation of the character.</param>
        public virtual void ChangeViewType(bool activate, float pitch, float yaw, Quaternion baseCharacterRotation)
        {
            if (activate && m_Camera.fieldOfView != m_FieldOfView) {
                m_FieldOfViewChangeTime = Time.time + m_FieldOfViewDamping / m_CharacterLocomotion.TimeScale;
            }
        }

        /// <summary>
        /// Updates the camera field of view.
        /// </summary>
        /// <param name="immediateUpdate">Should the field of view be updated immediately?</param>
        public virtual void UpdateFieldOfView(bool immediateUpdate)
        {
#if ULTIMATE_CHARACTER_CONTROLLER_VERSION_2_VR
            if (UnityEngine.XR.XRSettings.enabled) {
                return;
            }
#endif

            if (m_Camera.fieldOfView != m_FieldOfView) {
                var time = (immediateUpdate || m_FieldOfViewDamping == 0) ? 1 : ((Time.time - m_FieldOfViewChangeTime) / (m_FieldOfViewDamping / m_CharacterLocomotion.TimeScale));
                m_Camera.fieldOfView = Mathf.SmoothStep(m_Camera.fieldOfView, m_FieldOfView, time);
                if (immediateUpdate) {
                    SimulationManager.SetCameraFieldOfView(m_CameraController.SimulationIndex, m_Camera.fieldOfView);
                }
            }
        }

        /// <summary>
        /// Rotates the camera according to the horizontal and vertical movement values.
        /// </summary>
        /// <param name="horizontalMovement">-1 to 1 value specifying the amount of horizontal movement.</param>
        /// <param name="verticalMovement">-1 to 1 value specifying the amount of vertical movement.</param>
        /// <param name="immediateUpdate">Should the camera be updated immediately?</param>
        /// <returns>The updated rotation.</returns>
        public abstract Quaternion Rotate(float horizontalMovement, float verticalMovement, bool immediateUpdate);

        /// <summary>
        /// Rotates the camera within the LateUpdate loop.
        /// </summary>
        /// <param name="immediateUpdate">Should the camera be updated immediately?</param>
        /// <returns>The updated rotation.</returns>
        public virtual Quaternion LateRotate(bool immediateUpdate) { return m_Transform.rotation; }

        /// <summary>
        /// Moves the camera according to the current pitch and yaw values.
        /// </summary>
        /// <param name="immediateUpdate">Should the camera be updated immediately?</param>
        /// <returns>The updated position.</returns>
        public abstract Vector3 Move(bool immediateUpdate);

        /// <summary>
        /// Returns the position of the look source.
        /// </summary>
        /// <param name="characterLookPosition">Is the character look position being retrieved?</param>
        /// <returns>The position of the look source.</returns>
        public virtual Vector3 LookPosition(bool characterLookPosition) { return m_Transform.position; }

        /// <summary>
        /// Returns the direction that the character is looking.
        /// </summary>
        /// <param name="characterLookDirection">Is the character look direction being retrieved?</param>
        /// <returns>The direction that the character is looking.</returns>
        public virtual Vector3 LookDirection(bool characterLookDirection) { return m_Transform.forward; }

        /// <summary>
        /// Returns the direction that the character is looking.
        /// </summary>
        /// <param name="lookPosition">The position that the character is looking from.</param>
        /// <param name="characterLookDirection">Is the character look direction being retrieved?</param>
        /// <param name="layerMask">The LayerMask value of the objects that the look direction can hit.</param>
        /// <param name="includeRecoil">Should recoil be included in the look direction?</param>
        /// <param name="includeMovementSpread">Should the movement spread be included in the look direction?</param>
        /// <returns>The direction that the character is looking.</returns>
        public abstract Vector3 LookDirection(Vector3 lookPosition, bool characterLookDirection, int layerMask, bool includeRecoil, bool includeMovementSpread);

        /// <summary>
        /// Adds a positional force to the ViewType.
        /// </summary>
        /// <param name="force">The force to add.</param>
        public virtual void AddPositionalForce(Vector3 force) { }

        /// <summary>
        /// Adds a rotational force to the ViewType.
        /// </summary>
        /// <param name="force">The force to add.</param>
        public virtual void AddRotationalForce(Vector3 force) { }

        /// <summary>
        /// Adds a delayed positional force to all of the ViewTypes.
        /// </summary>
        /// <param name="force">The force to add.</param>
        /// <param name="restAccumulation">The percent of the force to accumulate to the rest value.</param>
        public virtual void AddSecondaryPositionalForce(Vector3 force, float restAccumulation) { }

        /// <summary>
        /// Adds a delayed rotational force to all of the ViewTypes.
        /// </summary>
        /// <param name="force">The force to add.</param>
        /// <param name="restAccumulation">The percent of the force to accumulate to the rest value.</param>
        public virtual void AddSecondaryRotationalForce(Vector3 force, float restAccumulation) { }

        /// <summary>
        /// Returns the point that the camera should be anchored to.
        /// </summary>
        /// <returns>The point that the camera should be anchored to.</returns>
        protected Vector3 GetAnchorPosition()
        {
            var anchorDiff = m_CameraController.Anchor.position - m_CameraController.CharacterTransform.position;
            var lookPoint = CharacterPosition + anchorDiff;
            lookPoint += (m_CameraController.AnchorOffset.x * m_Transform.right) + (m_CameraController.AnchorOffset.y * (CharacterRotation * Vector3.up)) + (m_CameraController.AnchorOffset.z * m_Transform.forward);
            return lookPoint;
        }

        /// <summary>
        /// Returns the anchor position transformed by the local position.
        /// </summary>
        /// <param name="localPosition">The position that the anchor position should be transformed by.</param>
        /// <returns>The anchor position transformed by the local position.</returns>
        protected Vector3 GetAnchorTransformPoint(Vector3 localPosition)
        {
            var anchorPosition = GetAnchorPosition();
            return MathUtility.TransformPoint(anchorPosition, m_CameraController.Anchor.rotation, localPosition);
        }

        /// <summary>
        /// Does the ViewType allow zooming?
        /// </summary>
        /// <returns>True if the ViewType allows zooming.</returns>
        public virtual bool CanZoom() { return true; }

        /// <summary>
        /// Sets the function that should override the rotation.
        /// </summary>
        /// <param name="overrideFunc">The function override.</param>
        public virtual void SetRotationalOverride(Func<Vector3, Quaternion, Quaternion> overrideFunc) { }

        /// <summary>
        /// The camera has been destroyed.
        /// </summary>
        public virtual void OnDestroy() { }
    }

    /// <summary>
    /// Attribute which specifies the recommended movement type for the view type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RecommendedMovementType : Attribute
    {
        private Type m_Type;
        public Type Type { get { return m_Type; } }
        public RecommendedMovementType(Type type) { m_Type = type; }
    }
}