﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Editor.Managers
{
    using Opsive.Shared.Editor.Managers;
    using Opsive.UltimateCharacterController.Game;
    using Opsive.UltimateCharacterController.Objects;
    using Opsive.UltimateCharacterController.Objects.CharacterAssist;
    using Opsive.UltimateCharacterController.Objects.ItemAssist;
    using Opsive.UltimateCharacterController.Traits;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// The ObjectManager will draw any item properties
    /// </summary>
    [OrderedEditorItem("Object", 5)]
    public class ObjectManager : Manager
    {
        /// <summary>
        /// The type of object to build.
        /// </summary>
        private enum ObjectType
        {
            ItemPickup,         // Builds an ItemPickup with a Respawner component.
            DroppedItem,        // Builds an ItemPickup that can be dropped from the character with the TrajectoryObject.
            HealthPickup,       // Builds a HealthPickup with a Respawner component.
            Projectile,         // Builds a Projectile that can be fired.
            MuzzleFlash,        // Builds an object with the MuzzleFlash component.
            Shell,              // Builds an object with the Shell component.
            Smoke,              // Builds an object with the Smoke component.
            MeleeTrail,         // Builds an object with the Trail component.
            Grenade,            // Builds a Grenade TrajectoryObject.
            Explosion,          // Builds an object with the Explosion and ParticleSystem components.
            MagicProjectile,    // Builds an object with the MagicProjectile and ParticleSystem components.
            Particle,           // Builds an object with the ParticlePooler and ParticleSystem components
        }

        [SerializeField] private string m_Name;
        [SerializeField] private ObjectType m_ObjectType;
        [SerializeField] private GameObject m_Object;
        [SerializeField] private bool m_MagicParticleCollisions;

        private Button m_BuildButton;

        /// <summary>
        /// Adds the visual elements to the ManagerContentContainer visual element. 
        /// </summary>
        public override void BuildVisualElements()
        {
            m_BuildButton = ManagerUtility.ShowControlBox("Object Builder", "Builds a new object with the specified type.", ShowObjectTypes, "Build Object", BuildObject, m_ManagerContentContainer, true);
            UpdateBuildButtonState();
        }

        /// <summary>
        /// Draws the object type popup.
        /// </summary>
        private void ShowObjectTypes(VisualElement container)
        {
            container.Clear();

            var nameField = new TextField("Name");
            nameField.value = m_Name;
            nameField.RegisterValueChangedCallback(c =>
            {
                m_Name = c.newValue;
                UpdateBuildButtonState();

            });
            container.Add(nameField);

            var objectTypePopup = new EnumField("Object Type", m_ObjectType);
            objectTypePopup.RegisterValueChangedCallback(c =>
            {
                m_ObjectType = (ObjectType)c.newValue;
                ShowObjectTypes(container);
            });
            container.Add(objectTypePopup);

            if (RequiresGameObject()) {
                var objectField = new ObjectField("GameObject");
                objectField.objectType = typeof(GameObject);
                objectField.value = m_Object;
                objectField.RegisterValueChangedCallback(c =>
                {
                    m_Object = (GameObject)c.newValue;
                    ShowObjectTypes(container);
                });
                container.Add(objectField);

                if (m_Object != null && m_ObjectType == ObjectType.ItemPickup) {
                    if (m_Object.GetComponent<Items.CharacterItem>() != null) {
                        var helpbox = new HelpBox("The Item Pickup should not reference an already created item. This GameObject should reference the item model.", HelpBoxMessageType.Error);
                        container.Add(helpbox);
                    }
                }
            } else {
                m_Object = null;
            }

            if (m_ObjectType == ObjectType.MagicProjectile || m_ObjectType == ObjectType.Particle) {
                var magicParticleToggle = new Toggle("Magic Particle Collisions");
                magicParticleToggle.value = m_MagicParticleCollisions;
                magicParticleToggle.RegisterValueChangedCallback(c =>
                {
                    m_MagicParticleCollisions = c.newValue;
                });
                container.Add(magicParticleToggle);
            }

            UpdateBuildButtonState();
        }

        /// <summary>
        /// Does the object type require the GameObject field?
        /// </summary>
        /// <returns>True if the object type requires the GameObject field.</returns>
        private bool RequiresGameObject()
        {
            return m_ObjectType != ObjectType.Explosion
                && m_ObjectType != ObjectType.MeleeTrail
                && m_ObjectType != ObjectType.MagicProjectile && m_ObjectType != ObjectType.Particle;
        }
     
        /// <summary>
        /// Updates the build button enabled state.
        /// </summary>
        private void UpdateBuildButtonState()
        {
            // The build button will be null during the first pass of ShowObjectTypes.
            if (m_BuildButton == null) {
                return;
            }

            var canBuild = true;
            if (string.IsNullOrEmpty(m_Name)) {
                canBuild = false;
            }
            if (m_Object == null && RequiresGameObject()) {
                canBuild = false;
            }
            if (m_Object != null && m_ObjectType == ObjectType.ItemPickup && m_Object.GetComponent<Items.CharacterItem>() != null) {
                canBuild = false;
            }

            m_BuildButton.SetEnabled(canBuild);
        }

        /// <summary>
        /// Builds the object.
        /// </summary>
        private void BuildObject()
        {
            var path = EditorUtility.SaveFilePanel("Save Object", "Assets", m_Name + ".prefab", "prefab");
            if (path.Length == 0 || Application.dataPath.Length > path.Length) {
                return;
            }

            var createdObject = m_Object;
            if (createdObject == null) {
                createdObject = new GameObject();
            } else if (EditorUtility.IsPersistent(createdObject)) {
                createdObject = Object.Instantiate(createdObject) as GameObject;
            }
            createdObject.name = m_Name;

            SphereCollider sphereCollider;
            switch (m_ObjectType) {
                case ObjectType.ItemPickup:
                    createdObject.layer = LayerManager.VisualEffect;
                    AddComponentIfNotAdded<BoxCollider>(createdObject);
                    sphereCollider = AddComponentIfNotAdded<SphereCollider>(createdObject);
                    sphereCollider.isTrigger = true;
                    AddComponentIfNotAdded<ItemPickup>(createdObject);
                    AddComponentIfNotAdded<Respawner>(createdObject);
                    break;
                case ObjectType.DroppedItem:
                    createdObject.layer = LayerManager.VisualEffect;
                    AddComponentIfNotAdded<BoxCollider>(createdObject);
                    sphereCollider = AddComponentIfNotAdded<SphereCollider>(createdObject);
                    sphereCollider.isTrigger = true;
                    AddComponentIfNotAdded<ItemPickup>(createdObject);
                    var trajectoryObject = AddComponentIfNotAdded<TrajectoryObject>(createdObject);
                    trajectoryObject.ImpactLayers = ~(1 << LayerManager.IgnoreRaycast | 1 << LayerManager.Water | 1 << LayerManager.SubCharacter | 1 << LayerManager.Overlay |
                                                      1 << LayerManager.VisualEffect | 1 << LayerManager.SubCharacter | 1 << LayerManager.Character);
                    break;
                case ObjectType.HealthPickup:
                    AddComponentIfNotAdded<SphereCollider>(createdObject);
                    AddComponentIfNotAdded<HealthPickup>(createdObject);
                    AddComponentIfNotAdded<Respawner>(createdObject);
                    break;
                case ObjectType.Projectile:
                    AddComponentIfNotAdded<Rigidbody>(createdObject);
                    AddComponentIfNotAdded<CapsuleCollider>(createdObject);
                    AddComponentIfNotAdded<Projectile>(createdObject);
                    break;
                case ObjectType.MuzzleFlash:
                    AddComponentIfNotAdded<MuzzleFlash>(createdObject);
                    break;
                case ObjectType.Shell:
                    AddComponentIfNotAdded<Rigidbody>(createdObject);
                    AddComponentIfNotAdded<CapsuleCollider>(createdObject);
                    AddComponentIfNotAdded<Shell>(createdObject);
                    var audioSource = AddComponentIfNotAdded<AudioSource>(createdObject);
                    audioSource.spatialBlend = 1;
                    audioSource.maxDistance = 10;
                    break;
                case ObjectType.Smoke:
                    AddComponentIfNotAdded<ParticleSystem>(createdObject);
                    AddComponentIfNotAdded<Smoke>(createdObject);
                    break;
                case ObjectType.MeleeTrail:
                    AddComponentIfNotAdded<MeshFilter>(createdObject);
                    AddComponentIfNotAdded<MeshRenderer>(createdObject);
                    AddComponentIfNotAdded<Trail>(createdObject);
                    break;
                case ObjectType.Grenade:
                    AddComponentIfNotAdded<Rigidbody>(createdObject);
                    AddComponentIfNotAdded<CapsuleCollider>(createdObject);
                    var grenade = AddComponentIfNotAdded<Grenade>(createdObject);
                    grenade.DestroyOnCollision = false;
                    break;
                case ObjectType.Explosion:
                    AddComponentIfNotAdded<ParticleSystem>(createdObject);
                    AddComponentIfNotAdded<Explosion>(createdObject);
                    break;
                case ObjectType.MagicProjectile:
                    AddComponentIfNotAdded<Rigidbody>(createdObject);
                    AddComponentIfNotAdded<ParticleSystem>(createdObject);
                    var magicParticle = AddComponentIfNotAdded<Projectile>(createdObject);
                    magicParticle.Collision = TrajectoryObject.CollisionMode.Ignore;
                    if (m_MagicParticleCollisions) {
                        AddComponentIfNotAdded<MagicParticle>(createdObject);
                    }
                    break;
                case ObjectType.Particle:
                    AddComponentIfNotAdded<ParticlePooler>(createdObject);
                    if (m_MagicParticleCollisions) {
                        AddComponentIfNotAdded<MagicParticle>(createdObject);
                    }
                    break;
            }

            var relativePath = path.Replace(Application.dataPath, "");
            PrefabUtility.SaveAsPrefabAsset(createdObject, "Assets" + relativePath);
            Selection.activeGameObject = AssetDatabase.LoadAssetAtPath("Assets" + relativePath, typeof(GameObject)) as GameObject;
            Object.DestroyImmediate(createdObject, true);
        }

        /// <summary>
        /// Adds the component to the specified GameObject if it isn't already added.
        /// </summary>
        /// <typeparam name="T">The type of component to add.</typeparam>
        /// <param name="obj">The GameObject to add the component to.</param>
        /// <returns>The component of type T.</returns>
        private T AddComponentIfNotAdded<T>(GameObject obj) where T : Component
        {
            T component;
            if ((component = obj.GetComponent<T>()) == null) {
                component = obj.AddComponent<T>();
            }
            return component;
        }
    }
}