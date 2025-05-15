/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Character.Abilities
{
    using System.Collections.Generic;
    using Opsive.Shared.Events;
    using Opsive.UltimateCharacterController.Utility;
    using UnityEngine;

    /// <summary>
    /// The Slide ability will apply a force to the character if the character is on a steep slope.
    /// </summary>
    [DefaultStopType(AbilityStopType.Automatic)]
    public class SlideV2 : Ability
    {
        [Tooltip("Steepness (in degrees) in which the character can slide.")]
        [Shared.Utility.MinMaxRange(0, 89)][SerializeField] protected Shared.Utility.MinMaxFloat m_SlideLimit = new Shared.Utility.MinMaxFloat(50, 89);
        [Tooltip("Steepness (in degrees) in which the character can slide when on the edge of a platform.")]
        [SerializeField] protected float m_EdgeSlideLimit = 30;
        [Tooltip("Acceleration of the ground's slide value. The slide value is determined by (1 - dynamicFriction) of the ground's physic material.")]
        [SerializeField] protected float m_Acceleration = 0.14f;
        [Tooltip("The maximum speed that the character can slide.")]
        [SerializeField] protected float m_MaxSlideSpeed = 0.54f;
        [Tooltip("The rate at which the slide speed decelerates.")]
        [SerializeField] protected float m_SlideDamping = 0.08f;
        [Tooltip("Optionally specifies the up direction that should override the character's up direction.")]
        [SerializeField] protected Vector3 m_OverrideUpDirection;

        [Tooltip("The moving vs. sliding angle (in radians) at which the character should stop sliding.")]
        [SerializeField, Range(0,4)] protected float m_moveSlideStopAngle = 1.5f;

        [Tooltip("Prefabs containing the values for the different levels of sliding. Ordered from no sliding to most sliding.")]
        public UltimateCharacterLocomotion[] slidingPrefabs;

        public Shared.Utility.MinMaxFloat SlideLimit { get { return m_SlideLimit; } set { m_SlideLimit = value; } }
        public float EdgeSlideLimit { get { return m_EdgeSlideLimit; } set { m_EdgeSlideLimit = value; } }
        public float Acceleration { get { return m_Acceleration; } set { m_Acceleration = value; } }
        public float MaxSlideSpeed { get { return m_MaxSlideSpeed; } set { m_MaxSlideSpeed = value; } }
        public float SlideDamping { get { return m_SlideDamping; } set { m_SlideDamping = value; } }
        public Vector3 OverrideUpDirection { get { return m_OverrideUpDirection; } set { m_OverrideUpDirection = value; } }

        public float MoveSlideStopAngle { get { return m_moveSlideStopAngle; } set { m_moveSlideStopAngle = value; } }

        private float m_SlideSpeed;
        private Vector3 m_SlideDirection;
        private Vector3 m_Momentum;


        private bool slowing = false;

        private Slideable currentSlopeSurface;
        private Slideable increasedSlideArea;

        private List<SlideV2> slidingValues = new List<SlideV2>();

        public override bool IsConcurrent { get { return true; } }
        public Slideable IncreasedSlideArea
        {
            set
            {
                increasedSlideArea = value;
            }
        }


        private int LevelOfSliding
        {
            get
            {
                if (currentSlopeSurface == null) return 0;
                if (increasedSlideArea == null) return (int)currentSlopeSurface.LevelOfSliding;
                var actualLevel = Mathf.Max(currentSlopeSurface.LevelOfSliding, increasedSlideArea.LevelOfSliding);
                return (int)Mathf.Clamp(actualLevel, 0, slidingValues.Count - 1);
            }
        }


        /// <summary>
        /// Initialize the default values.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            EventHandler.RegisterEvent<bool>(m_GameObject, "OnCharacterGrounded", OnGrounded);
            foreach (var slidingPrefab in slidingPrefabs)
                if (slidingPrefab != null)
                {
                    SlideV2 slideValue = slidingPrefab.GetAbility<SlideV2>();
                    if (slideValue != null) slidingValues.Add(slideValue);
                }
        }

        /// <summary>
        /// Called when the ablity is tried to be started. If false is returned then the ability will not be started.
        /// </summary>
        /// <returns>True if the ability can be started.</returns>
        public override bool CanStartAbility()
        {
            // An attribute may prevent the ability from starting.
            if (!base.CanStartAbility())
            {
                return false;
            }

            return CanSlide();
        }

        /// <summary>
        /// Returns true if the character can slide on the ground.
        /// </summary>
        /// <returns>True if the character can slide on the ground.</returns>
        private bool CanSlide()
        {
            // The character cannot slide in the air.
            if (!m_CharacterLocomotion.Grounded)
            {
                return false;
            }

            if (m_CharacterLocomotion.Velocity.magnitude <= m_CharacterLocomotion.ColliderSpacing)
            {
                return false;
            }

            if (m_CharacterLocomotion.Moving && !MovingDownward())
            {
                return false;
            }

            // Is the character on a surface that allows sliding?
            var slopeRaycastHit = DetermineSlideSurface();

            if (!OnSlidingSurface()) return false;

            SetSlidingValues();

            // If the character is on an edge then the slope limit is different.
            var upDirection = m_OverrideUpDirection.sqrMagnitude > 0 ? m_OverrideUpDirection : m_Transform.up;
            var slopeAngle = Vector3.Angle(m_CharacterLocomotion.GroundedRaycastHit.normal, upDirection);
            var ray = new Ray(m_CharacterLocomotion.TargetPosition + m_CharacterLocomotion.Up * m_CharacterLocomotion.ColliderSpacing * 2, -upDirection);
            if (!Physics.Raycast(ray, m_CharacterLocomotion.MaxStepHeight, m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore))
            {
                return slopeAngle >= m_EdgeSlideLimit - 0.001f;
            }


            // The character cannot slide if the slope isn't steep enough or is too steep.
            slopeAngle = Vector3.Angle(slopeRaycastHit.normal, upDirection);
            if (slopeAngle < m_SlideLimit.MinValue + 0.001f || slopeAngle > m_SlideLimit.MaxValue - 0.001f)
            {
                return false;
            }

            // The character can slide.
            return true;
        }


        private void UpdateSlide()
        {
            DetermineSlideSurface();
            SetSlidingValues();
        }

        private RaycastHit DetermineSlideSurface()
        {
            Ray ray = new Ray(m_CharacterLocomotion.GroundedRaycastHit.point + m_CharacterLocomotion.GroundedRaycastHit.normal * m_CharacterLocomotion.ColliderSpacing, -m_CharacterLocomotion.GroundedRaycastHit.normal);
            bool hit = Physics.Raycast(ray, out var slopeRaycastHit, m_CharacterLocomotion.ColliderSpacing * 2, m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore);
            currentSlopeSurface = hit ? slopeRaycastHit.transform.GetComponent<Slideable>() : null;
            return slopeRaycastHit;
        }

        private bool OnSlidingSurface()
        {
            return currentSlopeSurface != null;
        }

        private void SetSlidingValues()
        {
            SlideV2 slidingValue = OnSlidingSurface() ? slidingValues[LevelOfSliding] : slidingValues[0];
            if (slidingValue == null) return;

            SlideLimit = slidingValue.SlideLimit;
            EdgeSlideLimit = slidingValue.EdgeSlideLimit;
            Acceleration = slidingValue.Acceleration;
            MaxSlideSpeed = slidingValue.MaxSlideSpeed;
            SlideDamping = slidingValue.SlideDamping;
            OverrideUpDirection = slidingValue.OverrideUpDirection;
            MoveSlideStopAngle = slidingValue.MoveSlideStopAngle;
        }

        /// <summary>
        /// The ability has started.
        /// </summary>
        protected override void AbilityStarted()
        {
            base.AbilityStarted();
            var speedChange = m_CharacterLocomotion.GetAbility<SpeedChangeGradual>();
            if (speedChange != null)
            {
                speedChange.StopAbility();
            }
            m_SlideSpeed = 0;
            m_SlideDirection = Vector3.zero;
            m_CharacterLocomotion.ForceStickToGround = true;
        }

        /// <summary>
        /// Update the controller's position values.
        /// </summary>
        public override void UpdatePosition()
        {
            base.UpdatePosition();

            UpdateSlide();

            bool downwards = MovingDownward();


            m_SlideSpeed /= (1 + m_SlideDamping * m_CharacterLocomotion.TimeScale * Time.timeScale);

            // The slide value uses the ground's physic material to get the amount of friction of the material.
            var upDirection = m_OverrideUpDirection.sqrMagnitude > 0 ? m_OverrideUpDirection : m_CharacterLocomotion.Up;
            var slopeAngle = Vector3.Angle(m_CharacterLocomotion.GroundedRaycastHit.normal, upDirection);
            var direction = Vector3.Cross(Vector3.Cross(m_CharacterLocomotion.GroundedRaycastHit.normal, -upDirection), m_CharacterLocomotion.GroundedRaycastHit.normal).normalized;
            var directionDot = Vector3.Dot(m_Momentum.normalized, direction);

            // The slope may not be within the range but m_SlopeSpeed is still greater than 0 so the ability hasn't stopped yet. 
            var increaseSlideSpeed = slopeAngle >= m_SlideLimit.MinValue - 0.001f && slopeAngle <= m_SlideLimit.MaxValue + 0.001f;
            var minSlideValue = m_SlideLimit.MinValue;
            if (!increaseSlideSpeed)
            {
                slowing = true;
                // The character may be on the edge.
                var ray = new Ray(m_CharacterLocomotion.TargetPosition + m_CharacterLocomotion.Up * m_CharacterLocomotion.ColliderSpacing * 2, -upDirection);
                if (!Physics.Raycast(ray, m_CharacterLocomotion.MaxStepHeight, m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore))
                {
                    increaseSlideSpeed = slopeAngle >= m_EdgeSlideLimit - 0.001f;
                    minSlideValue = m_EdgeSlideLimit;
                }
            }

            if (increaseSlideSpeed)
            {
                slowing = false;
                // The character may be on a step.
                if (Physics.Raycast(MathUtility.TransformPoint(m_CharacterLocomotion.TargetPosition, m_CharacterLocomotion.Rotation, Vector3.forward * m_CharacterLocomotion.SkinWidth) +
                                                            upDirection * m_CharacterLocomotion.MaxStepHeight,
                                                            -upDirection, out var raycastHit,
                                                            m_CharacterLocomotion.MaxStepHeight + m_CharacterLocomotion.SkinWidth, m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore) &&
                                                            Vector3.Dot(raycastHit.normal, upDirection) > 0.99f)
                {
                    return;
                }

                // Increase the slide speed if the slope is in the direction of the character's momentum, otherwise decrease the slide speed.
                var slide = m_Acceleration * (1 - m_CharacterLocomotion.GroundedRaycastHit.collider.material.dynamicFriction) * ((slopeAngle - minSlideValue) / (m_SlideLimit.MaxValue - minSlideValue)) * (directionDot >= 0 ? 1 : -1);
                m_SlideSpeed = Mathf.Max(0, Mathf.Min(m_SlideSpeed + slide, m_MaxSlideSpeed));
            }

            if (m_SlideSpeed > 0)
            {
                // If the character isn't on a flat surface then they should move in the direction of the slope. The inverse direction will be used if the slope is facing the 
                // oppsite direction of the momentum.
                if (direction.sqrMagnitude > 0 && increaseSlideSpeed)
                {
                    if (directionDot >= 0)
                    {
                        m_Momentum = m_SlideDirection = direction;
                    }
                    else
                    {
                        m_SlideDirection = -direction;
                    }
                }

                m_CharacterLocomotion.DesiredMovement += m_SlideSpeed * m_CharacterLocomotion.TimeScale * Time.timeScale * m_SlideDirection;
            }
            else if (direction.sqrMagnitude > 0)
            {

                // The slope is changing directions.
                m_Momentum = direction.normalized;
                
            }
        }

        /// <summary>
        /// Can the ability be stopped?
        /// </summary>
        /// <param name="force">Should the ability be force stopped?</param>
        /// <returns>True if the ability can be stopped.</returns>
        public override bool CanStopAbility(bool force)
        {
            //return m_CharacterLocomotion.Moving && !MovingDownward();
            if (force) { return true; }


            float velocity = m_CharacterLocomotion.Velocity.magnitude;
            float motorAcceleration = m_CharacterLocomotion.MotorAcceleration.magnitude;

            return velocity <= motorAcceleration + 1f ||
                (slowing && velocity <= motorAcceleration * 1.7f) /*||
                (!MovingDownward() && velocity <= motorAcceleration * 2f)*/;
        }

        /// <summary>
        /// The character has changed grounded state. 
        /// </summary>
        /// <param name="grounded">Is the character on the ground?</param>
        private void OnGrounded(bool grounded)
        {
            if (grounded)
            {
                if (!CanSlide())
                {
                    m_SlideSpeed = 0;
                }
            }
            else
            {
                StopAbility(true);
            }
        }

        public override bool ShouldBlockAbilityStart(Ability startingAbility)
        {
            return startingAbility is SpeedChange || startingAbility is SpeedChangeGradual;
        }




        /// <summary>
        /// The ability has stopped running.
        /// </summary>
        /// <param name="force">Was the ability force stopped?</param>
        protected override void AbilityStopped(bool force)
        {
            base.AbilityStopped(force);

            if (m_SlideSpeed > 0)
            {
                AddForce((m_SlideSpeed / Time.deltaTime) * m_CharacterLocomotion.TimeScale * Time.timeScale * m_SlideDirection, 1, false);
            }
            m_CharacterLocomotion.ForceStickToGround = false;
        }

        /// <summary>
        /// The character has been destroyed.
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();

            EventHandler.UnregisterEvent<bool>(m_GameObject, "OnCharacterGrounded", OnGrounded);
        }

        bool MovingDownward()
        {
            var upDirection = m_OverrideUpDirection.sqrMagnitude > 0 ? m_OverrideUpDirection : m_CharacterLocomotion.Up;
            var slideDirection = Vector3.Cross(Vector3.Cross(m_CharacterLocomotion.GroundedRaycastHit.normal, -upDirection), m_CharacterLocomotion.GroundedRaycastHit.normal).normalized;

            var velocity = m_CharacterLocomotion.Velocity;

            var moveSlideAngle =  Mathf.Acos(Vector3.Dot(slideDirection, velocity) / velocity.magnitude);


            return moveSlideAngle < MoveSlideStopAngle;
        }



    }

    
}