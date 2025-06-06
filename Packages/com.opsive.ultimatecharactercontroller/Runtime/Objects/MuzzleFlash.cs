﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Objects
{
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Character;
#if FIRST_PERSON_CONTROLLER
    using Opsive.UltimateCharacterController.FirstPersonController.Camera;
#endif
    using Opsive.UltimateCharacterController.Game;
    using Opsive.UltimateCharacterController.Items;
    using Opsive.UltimateCharacterController.Items.Actions;
    using UnityEngine;

    /// <summary>
    /// Shows an object which slowly fades out with time. Can optionally attach a light to the GameObject and that light will be faded as well.
    /// </summary>
    public class MuzzleFlash : MonoBehaviour, IMuzzleFlash
    {
        [Tooltip("The Renderer with the materials to change color tint.")]
        [SerializeField] protected Renderer m_MuzzleRenderer;
        [Tooltip("The name of the shader tint color property.")]
        [SerializeField] protected string m_TintColorPropertyName = "_TintColor";

        [Tooltip("The alpha value to initialize the muzzle flash material to.")]
        [Range(0, 1)] [SerializeField] protected float m_StartAlpha = 0.5f;
        [Tooltip("The minimum fade speed - the larger the value the quicker the muzzle flash will fade.")]
        [SerializeField] protected float m_MinFadeSpeed = 3;
        [Tooltip("The maximum fade speed - the larger the value the quicker the muzzle flash will fade.")]
        [SerializeField] protected float m_MaxFadeSpeed = 4;

        [System.NonSerialized] private GameObject m_GameObject;
#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
        private Transform m_Transform;
#endif
        private Material m_Material;
        private Light m_Light;
        private ParticleSystem m_Particles;

        private int m_TintColorPropertyID;
        private Color m_Color;
        private float m_StartLightIntensity;
        private float m_FadeSpeed;
        private float m_TimeScale = 1;

        private GameObject m_Character;
#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
        private CharacterItem m_CharacterItem;
        private int m_ItemActionID;
#endif
#if FIRST_PERSON_CONTROLLER
        private MaterialSwapper m_MaterialSwapper;
#endif
        private bool m_Pooled;
        private int m_StartLayer;

        protected IPerspectiveProperty<Transform> m_PerspectiveLocation;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected virtual void Awake()
        {
            m_GameObject = gameObject;
#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
            m_Transform = transform;
#endif
            m_TintColorPropertyID = Shader.PropertyToID(m_TintColorPropertyName);

            if (m_MuzzleRenderer == null) {
                m_MuzzleRenderer = GetComponent<Renderer>();
            }
            
            if (m_MuzzleRenderer != null) {
                m_Material = m_MuzzleRenderer.sharedMaterial;
            }
            m_Light = GetComponent<Light>();
            m_Particles = GetComponent<ParticleSystem>();
            // If a light exists set the start light intensity. Every time the muzzle flash is enabed the light intensity will be reset to its starting value.
            if (m_Light != null) {
                m_StartLightIntensity = m_Light.intensity;
            }
            m_StartLayer = m_GameObject.layer;
        }

        /// <summary>
        /// The muzzle flash has been enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            m_Color.a = 0;
            if (m_Material != null) {
                m_Material.SetColor(m_TintColorPropertyID, m_Color);
            }
            if (m_Light != null) {
                m_Light.intensity = 0;
            }
            if (m_Particles != null) {
                m_Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>
        /// A weapon has been fired and the muzzle flash needs to show. Set the starting alpha value and light intensity if the light exists.
        /// </summary>
        /// <param name="characterItem">The item that the muzzle flash is attached to.</param>
        /// <param name="itemActionID">The ID which corresponds to the ItemAction that spawned the muzzle flash.</param>
        /// <param name="perspectiveLocation">The muzzle flash location where it should be placed if the perspective changes.</param>
        /// <param name="pooled">Is the muzzle flash pooled?</param>
        /// <param name="characterLocomotion">The character that the muzzle flash is attached to.</param>
        public virtual void Show(CharacterItem characterItem, int itemActionID, IPerspectiveProperty<Transform> perspectiveLocation, bool pooled, UltimateCharacterLocomotion characterLocomotion)
        {
            m_PerspectiveLocation = perspectiveLocation;
            
            // The muzzle flash may be inactive if the object isn't pooled.
            if (!m_Pooled) {
                m_GameObject.SetActive(true);
            }

            if (characterLocomotion != null) {
                m_Character = characterLocomotion.gameObject;
                if (m_Pooled) {
                    EventHandler.RegisterEvent<float>(m_Character, "OnCharacterChangeTimeScale", OnChangeTimeScale);
#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
                    EventHandler.RegisterEvent<bool>(m_Character, "OnCharacterChangePerspectives", OnChangePerspectives);
#endif
                }

#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
                m_MaterialSwapper = characterLocomotion.LookSource.GameObject.GetCachedComponent<MaterialSwapper>();
                if (m_MaterialSwapper != null) {
                    m_MaterialSwapper.OnEnableFirstPersonMaterials += OnMaterialSwapperEnableFirstPerson;
                    m_MaterialSwapper.OnEnableThirdPersonMaterials += OnMaterialSwapperEnableThirdPerson;
                }
#endif
            }

#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
            m_CharacterItem = characterItem;
            m_ItemActionID = itemActionID;
#endif
            m_Pooled = pooled;
            if (characterLocomotion != null) {
                m_TimeScale = characterLocomotion.TimeScale;
                m_GameObject.layer = characterLocomotion.FirstPersonPerspective ? LayerManager.Overlay : m_StartLayer;
            } else {
                m_TimeScale = 1;
                m_GameObject.layer = m_StartLayer;
            }

            m_Color = Color.white;
            m_Color.a = m_StartAlpha;
            if (m_Material != null) {
                m_Material.SetColor(m_TintColorPropertyID, m_Color);
            }
            m_FadeSpeed = Random.Range(m_MinFadeSpeed, m_MaxFadeSpeed);
            if (m_Light != null) {
                m_Light.intensity = m_StartLightIntensity;
            }
            if (m_Particles != null) {
                m_Particles.Play(true);
            }
            // The muzzle flash may be inactive if the object isn't pooled.
            if (!m_Pooled) {
                m_GameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Decrease the alpha value of the muzzle flash to give it a fading effect. As soon as the alpha value reaches zero place the muzzle flash back in
        /// the object pool. If a light exists decrease the intensity of the light as well.
        /// </summary>
        protected virtual void Update()
        {
            if (m_Color.a > 0) {
                m_Color.a = Mathf.Max(m_Color.a - (m_FadeSpeed * Time.deltaTime * m_TimeScale), 0);
                if (m_Material != null) {
                    m_Material.SetColor(m_TintColorPropertyID, m_Color);
                }
                // Keep the light intensity synchronized with the alpha channel's value.
                if (m_Light != null) {
                    m_Light.intensity = m_StartLightIntensity * (m_Color.a / m_StartAlpha);
                }
            } else {
                if (m_Pooled) {
                    ObjectPoolBase.Destroy(m_GameObject);
                } else {
                    m_GameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// The character's local timescale has changed.
        /// </summary>
        /// <param name="timeScale">The new timescale.</param>
        protected virtual void OnChangeTimeScale(float timeScale)
        {
            m_TimeScale = timeScale;
        }

#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
        /// <summary>
        /// The character perspective between first and third person has changed.
        /// </summary>
        /// <param name="firstPersonPerspective">Is the character in a first person perspective?</param>
        private void OnChangePerspectives(bool firstPersonPerspective)
        {
            // When switching locations the local position and rotation should remain the same.
            var localPosition = m_Transform.localPosition;
            var localRotation = m_Transform.rotation;
            
            var muzzleFlashLocation = m_PerspectiveLocation.GetValue(firstPersonPerspective);
            m_Transform.parent = muzzleFlashLocation;
            m_Transform.localPosition = localPosition;
            m_Transform.rotation = localRotation;

            m_GameObject.layer = firstPersonPerspective ? LayerManager.Overlay : m_StartLayer;
        }

        /// <summary>
        /// The material swapper has enabled the first person materials.
        /// </summary>
        private void OnMaterialSwapperEnableFirstPerson()
        {
            OnChangePerspectives(true);
        }

        /// <summary>
        /// The material swapper has enabled the third person materials.
        /// </summary>
        private void OnMaterialSwapperEnableThirdPerson()
        {
            OnChangePerspectives(false);
        }
#endif

        /// <summary>
        /// The object has been disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (m_Character != null) {
                if (m_Pooled) {
                    EventHandler.UnregisterEvent<float>(m_Character, "OnCharacterChangeTimeScale", OnChangeTimeScale);
#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
                    EventHandler.UnregisterEvent<bool>(m_Character, "OnCharacterChangePerspectives", OnChangePerspectives);
#endif
                }

#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
                if (m_MaterialSwapper != null) {
                    m_MaterialSwapper.OnEnableFirstPersonMaterials += OnMaterialSwapperEnableFirstPerson;
                    m_MaterialSwapper.OnEnableThirdPersonMaterials += OnMaterialSwapperEnableThirdPerson;
                }
#endif
            }
        }
    }
}