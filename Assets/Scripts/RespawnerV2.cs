﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Traits
{
    using Opsive.Shared.Audio;
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
    using Opsive.Shared.Networking;
#endif
    using Opsive.Shared.StateSystem;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateCharacterController.Game;
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
    using Opsive.UltimateCharacterController.Networking.Traits;
#endif
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Specifies the location the object should spawn.
    /// </summary>
    public class RespawnerV2 : StateBehavior
    {
        /// <summary>
        /// Specifies if the object should spawn in the starting position or use a spawn point.
        /// </summary>
        public enum SpawnPositioningMode
        {
            None,           // The object will not change locations when spawning.
            StartLocation,  // The object will spawn in the same location as the object started in.
            SpawnPoint      // The object will use the Spawn Point system to determine where to spawn.
        }
        
        [Tooltip("Specifies the location the object should spawn.")]
        [SerializeField] protected SpawnPositioningMode m_PositioningMode = SpawnPositioningMode.StartLocation;
        [Tooltip("The grouping index to use when spawning to a spawn point. A value of -1 will ignore the grouping value.")]
        [SerializeField] protected int m_Grouping = -1;
        [Tooltip("The minimum amount of time before the object respawns after death or after being disabled.")]
        [SerializeField] protected float m_MinRespawnTime = 2;
        [Tooltip("The maximum amount of time before the object respawns after death or after being disabled.")]
        [SerializeField] protected float m_MaxRespawnTime = 3;
        [Tooltip("Should a respawn be scheduled when the object dies?")]
        [SerializeField] protected bool m_ScheduleRespawnOnDeath = true;
        [Tooltip("Should a respawn be scheduled when the component is disabled?")]
        [SerializeField] protected bool m_ScheduleRespawnOnDisable = true;
        [Tooltip("A set of AudioClips that can be played when the object is respawned.")]
        [HideInInspector] [SerializeField] protected AudioClipSet m_RespawnAudioClipSet = new AudioClipSet();
        [Tooltip("Unity event invoked when the object respawns.")]
        [SerializeField] protected UnityEvent m_OnRespawnEvent;


        [Tooltip("Should the object respawn at the closest Spawning Point in the group? (otherwise choose a point at random)")]
        [SerializeField] protected bool m_RespawnAtClosestPoint;
        [Tooltip("Should the object respawn at the latest Spawning Point reached?")]
        [SerializeField] protected bool m_RespawnAtLatestPoint;

        public SpawnPositioningMode PositioningMode { get { return m_PositioningMode; } set { m_PositioningMode = value; } }
        public int Grouping { get { return m_Grouping; } set { m_Grouping = value; } }
        public float MinRespawnTime { get { return m_MinRespawnTime; } set { m_MinRespawnTime = value; } }
        public float MaxRespawnTime { get { return m_MaxRespawnTime; } set { m_MaxRespawnTime = value; } }
        public bool ScheduleRespawnOnDeath { get { return m_ScheduleRespawnOnDeath; } set { m_ScheduleRespawnOnDeath = value; } }
        public bool ScheduleRespawnOnDisable { get { return m_ScheduleRespawnOnDisable; } set { m_ScheduleRespawnOnDisable = value; } }
        public AudioClipSet RespawnAudioClipSet { get { return m_RespawnAudioClipSet; } set { m_RespawnAudioClipSet = value; } }
        public UnityEvent OnRespawnEvent { get { return m_OnRespawnEvent; } set { m_OnRespawnEvent = value; } }

        public bool RespawnAtClosestPoint { get { return m_RespawnAtClosestPoint; } set { m_RespawnAtClosestPoint = value; } }
        public bool RespawnAtLatestPoint { get { return m_RespawnAtLatestPoint; } set { m_RespawnAtLatestPoint = value; } }



        protected GameObject m_GameObject;
        private Transform m_Transform;
        protected UltimateCharacterLocomotion m_CharacterLocomotion;
        protected SpawnPoint m_ActiveSpawnPoint;

#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
        private INetworkInfo m_NetworkInfo;
        private INetworkRespawnerMonitor m_NetworkRespawnerMonitor;
#endif

        private Vector3 m_StartPosition;
        private Quaternion m_StartRotation;
        protected ScheduledEventBase m_ScheduledRespawnEvent;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_GameObject = gameObject;
            m_Transform = transform;
            m_CharacterLocomotion = m_GameObject.GetCachedComponent<UltimateCharacterLocomotion>();
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
            m_NetworkInfo = m_GameObject.GetCachedComponent<INetworkInfo>();
            m_NetworkRespawnerMonitor = m_GameObject.GetCachedComponent<INetworkRespawnerMonitor>();
            if (m_NetworkInfo != null && m_NetworkRespawnerMonitor == null) {
                Debug.LogError("Error: The object " + m_GameObject.name + " must have a NetworkRespawnerMonitor component.");
            }
#endif

            m_StartPosition = m_Transform.position;
            m_StartRotation = m_Transform.rotation;

            EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(m_GameObject, "OnDeath", OnDeath);
        }

        /// <summary>
        /// The character has died. Prepare for a respawn.
        /// </summary>
        /// <param name="position">The position of the force.</param>
        /// <param name="force">The amount of force which killed the character.</param>
        /// <param name="attacker">The GameObject that killed the character.</param>
        private void OnDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            if (!m_ScheduleRespawnOnDeath) {
                return;
            }

#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
            // The local player will control when the object respawns.
            if (m_NetworkInfo != null && !m_NetworkInfo.HasAuthority()) {
                return;
            }
#endif

            if (m_ScheduledRespawnEvent != null) {
                Scheduler.Cancel(m_ScheduledRespawnEvent);
                m_ScheduledRespawnEvent = null;
            }
            m_ScheduledRespawnEvent = Scheduler.Schedule(Random.Range(m_MinRespawnTime, m_MaxRespawnTime), Respawn);
        }

        /// <summary>
        /// The object has been enabled.
        /// </summary>
        private void OnEnable()
        {
            if (m_ScheduleRespawnOnDisable) {
                CancelRespawn();
            }
        }

        /// <summary>
        /// The object has been disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
            if (m_NetworkInfo != null && !m_NetworkInfo.HasAuthority()) {
                return;
            }
#endif

            if (ScheduleRespawnOnDisable && m_ScheduledRespawnEvent == null) {
                m_ScheduledRespawnEvent = Scheduler.Schedule(Random.Range(m_MinRespawnTime, m_MaxRespawnTime), Respawn);
            }
        }

        /// <summary>
        /// Determines the location to respawn the object to and then does the respawn.
        /// </summary>
        public void Respawn()
        {
            m_ScheduledRespawnEvent = null;

            if (m_PositioningMode != SpawnPositioningMode.None) {
                Vector3 position;
                Quaternion rotation;
                if (m_PositioningMode == SpawnPositioningMode.SpawnPoint) {
                    position = m_Transform.position;
                    rotation = m_Transform.rotation;
                    if (m_RespawnAtLatestPoint && m_ActiveSpawnPoint != null)
                    {

                        // If the object can't be spawned then try again in the future.
                        if (!m_ActiveSpawnPoint.GetPlacement(m_GameObject, ref position, ref rotation))
                        {
                            m_ScheduledRespawnEvent = Scheduler.Schedule(Random.Range(m_MinRespawnTime, m_MaxRespawnTime), Respawn);
                            return;
                        }
                    }
                    else
                    {
                        // If the object can't be spawned then try again in the future.
                        if (!SpawnPointManager.GetPlacement(m_GameObject, m_Grouping, ref position, ref rotation, m_RespawnAtClosestPoint))
                        {
                            m_ScheduledRespawnEvent = Scheduler.Schedule(Random.Range(m_MinRespawnTime, m_MaxRespawnTime), Respawn);
                            return;
                        }
                    }

                } else { // Spawn Location.
                    position = m_StartPosition;
                    rotation = m_StartRotation;
                }

                Respawn(position, rotation, true);
            } else {
                Respawn(m_Transform.position, m_Transform.rotation, false);
            }
        }

        /// <summary>
        /// Respawn at the specific spawning point
        /// </summary>
        /// <param name="spawnPoint">The spawn point where the object should be respawned at.</param>
        public void RespawnAt(SpawnPoint spawnPoint)
        {
            Vector3 position;
            Quaternion rotation;
            position = m_Transform.position;
            rotation = m_Transform.rotation;


            // If the object can't be spawned then try again in the future.
            if (!spawnPoint.GetPlacement(m_GameObject, ref position, ref rotation))
            {
                m_ScheduledRespawnEvent = Scheduler.Schedule(Random.Range(m_MinRespawnTime, m_MaxRespawnTime), Respawn);
                return;
            }

            Respawn(position, rotation, true);
        }

        /// <summary>
        /// Does the respawn by setting the position and rotation to the specified values.
        /// Enable the GameObject and let all of the listening objects know that the object has been respawned.
        /// </summary>
        /// <param name="position">The respawn position.</param>
        /// <param name="rotation">The respawn rotation.</param>
        /// <param name="transformChange">Was the position or rotation changed?</param>
        public virtual void Respawn(Vector3 position, Quaternion rotation, bool transformChange)
        {
            // Send a pre-respawn event so abilities can stop if they should no longer be active.
            EventHandler.ExecuteEvent(m_GameObject, "OnWillRespawn");

            if (transformChange) {
                // Characters require a specific setter for the position and rotation.
                if (m_CharacterLocomotion != null) {
                    m_CharacterLocomotion.SetPositionAndRotation(position, rotation, false);
                } else {
                    m_Transform.SetPositionAndRotation(position, rotation);
                }
            }

            m_GameObject.SetActive(true);

            // The parents may not be active.
            if (m_GameObject.activeInHierarchy) {
                // Play any respawn audio.
                m_RespawnAudioClipSet.PlayAudioClip(m_GameObject);

                EventHandler.ExecuteEvent(m_GameObject, "OnRespawn");
                if (m_OnRespawnEvent != null) {
                    m_OnRespawnEvent.Invoke();
                }
            }

#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
            if (m_NetworkInfo != null && m_NetworkInfo.HasAuthority()) {
                m_NetworkRespawnerMonitor.Respawn(position, rotation, transformChange);
            }
#endif
        }

        /// <summary>
        /// Cancels the respawn.
        /// </summary>
        public void CancelRespawn()
        {
            if (m_ScheduledRespawnEvent != null) {
                Scheduler.Cancel(m_ScheduledRespawnEvent);
                m_ScheduledRespawnEvent = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            SpawnPoint spawnPoint = other.GetComponent<SpawnPoint>();
            if (spawnPoint != null && (Grouping == -1 || spawnPoint.Grouping == Grouping || spawnPoint.Grouping == -1))
            {
                Debug.Log($"Current checkpoint: {spawnPoint.transform.parent.name}");
                m_ActiveSpawnPoint = spawnPoint;
            }
        }

        /// <summary>
        /// The GameObject has been destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            CancelRespawn();
            EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(m_GameObject, "OnDeath", OnDeath);
        }
    }
}