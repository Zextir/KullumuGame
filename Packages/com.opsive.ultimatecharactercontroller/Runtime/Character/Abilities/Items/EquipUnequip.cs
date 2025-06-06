﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Character.Abilities.Items
{
    using Opsive.Shared.Inventory;
    using Opsive.UltimateCharacterController.Items;
    using Opsive.UltimateCharacterController.Items.Actions;
    using Opsive.UltimateCharacterController.Utility;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using EventHandler = Opsive.Shared.Events.EventHandler;

    /// <summary>
    /// Equips or unequips an ItemSet. Can be started manually by calling EquipUnequip.StartEquipUnequip(ItemSetIndex).
    /// </summary>
    [DefaultStartType(AbilityStartType.ButtonDown)]
    [DefaultInputName("Equip First Item", 0)]
    [DefaultInputName("Equip Second Item", 1)]
    [DefaultInputName("Equip Third Item", 2)]
    [DefaultInputName("Equip Fourth Item", 3)]
    [DefaultInputName("Equip Fifth Item", 4)]
    [DefaultInputName("Equip Sixth Item", 5)]
    [DefaultInputName("Equip Seventh Item", 6)]
    [DefaultInputName("Equip Eighth Item", 7)]
    [DefaultInputName("Equip Ninth Item", 8)]
    [DefaultInputName("Equip Tenth Item", 9)]
    [AllowDuplicateTypes]
    public class EquipUnequip : ItemSetAbilityBase
    {
        /// <summary>
        /// Specifies when to equip a new Item.
        /// </summary>
        [System.Flags]
        public enum AutoEquipType
        {
            Always = 1,             // Always equip a picked up item.
            Unequipped = 2,         // Equip the item if there are no items equipped.
            OutOfUsableItem = 4,    // Equip the item if the current item has no more usable ItemIdentifiers left.
            NotPreset = 8,          // Equip the item if the item hasn't been added to the inventory already.
            FirstTime = 16          // Equip the item the first time the item has been added.
        }

        /// <summary>
        /// Specifies what action take place with the next update.
        /// </summary>
        private enum EquipUnequipAction
        {
            Inactive,           // No actions are currently necessary.
            Unequip,            // The Unequip method should be called.
            UnequipComplete,    // The UnequipComplete method should be called.
            Equip,              // The Equip method should be called.
            EquipComplete       // The EquipComplete method should be called.
        }

        [Tooltip("Mask which specifies when to auto equip a new item.")]
        [SerializeField] protected AutoEquipType m_AutoEquip = AutoEquipType.Unequipped | AutoEquipType.OutOfUsableItem | AutoEquipType.NotPreset | AutoEquipType.FirstTime;
        [Tooltip("The Item State Index while equipping.")]
        [SerializeField] protected int m_EquipItemStateIndex = 4;
        [Tooltip("The Item State Index while unequipping.")]
        [SerializeField] protected int m_UnequipItemStateIndex = 5;
        [Tooltip("The value to add to the Item Substate Index when the character is aiming.")]
        [SerializeField] protected int m_AimItemSubstateIndexAddition = 100;

        public AutoEquipType AutoEquip { get { return m_AutoEquip; } set { m_AutoEquip = value; } }

        private EquipUnequip[] m_EquipUnequipAbilities;
        private bool m_StartEquipUnequip;
        private int m_StartEquipUnequipIndex;
        private int m_ActiveItemSetIndex = -1;
        private int m_PrevActiveItemSetIndex = -1;
        private EquipUnequipAction[] m_EquipUnequipActions;
        private CharacterItem[] m_EquipItems;
        private CharacterItem[] m_UnequipItems;
        private bool m_CanEquip;
        private bool[] m_EquippingItems;
        private bool[] m_UnequippingItems;
        private Dictionary<IItemIdentifier, int> m_InventoryAmount = new Dictionary<IItemIdentifier, int>();
        private bool m_ImmediateEquipUnequip;
        private bool m_PlayEquipAudio;
        private bool m_Aiming;
        private Action<int> m_OnAnimatorItemEquip;
        private Action<int> m_OnAnimatorItemEquipComplete;
        private Action<int> m_OnAnimatorItemUnequip;
        private Action<int> m_OnAnimatorItemUnequipComplete;

        public override bool CanReceiveMultipleStarts { get { return true; } }
        public int ActiveItemSetIndex { get { return m_ActiveItemSetIndex; } }
        public override bool CanStayActivatedOnDeath { get { return true; } }

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            m_EquipUnequipAbilities = m_CharacterLocomotion.GetAbilities<EquipUnequip>();
            m_EquipUnequipActions = new EquipUnequipAction[m_Inventory.SlotCount];
            m_EquipItems = new CharacterItem[m_Inventory.SlotCount];
            m_UnequipItems = new CharacterItem[m_Inventory.SlotCount];
            m_EquippingItems = new bool[m_Inventory.SlotCount];
            m_UnequippingItems = new bool[m_Inventory.SlotCount];

            EventHandler.RegisterEvent(m_GameObject, "OnItemPickupStartPickup", WillStartPickup);
            EventHandler.RegisterEvent(m_GameObject, "OnItemPickupStopPickup", StopPickup);
            EventHandler.RegisterEvent<CharacterItem, int, bool, bool>(m_GameObject, "OnInventoryPickupItem", OnPickupItem);
            EventHandler.RegisterEvent<IItemIdentifier, int, bool, bool>(m_GameObject, "OnInventoryPickupItemIdentifier", OnPickupItemIdentifier);
            EventHandler.RegisterEvent<int, int>(m_GameObject, "OnItemSetIndexChange", OnItemSetIndexChange);
            EventHandler.RegisterEvent<int, int>(m_GameObject, "OnEquipUnequipVerifyUnequipItem", OnVerifyUnequipItem);
            EventHandler.RegisterEvent<CharacterItem, int>(m_GameObject, "OnInventoryRemoveItem", OnRemoveItem);
            EventHandler.RegisterEvent<bool, bool>(m_GameObject, "OnAimAbilityStart", OnAim);
            EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(m_GameObject, "OnDeath", OnDeath);
            EventHandler.RegisterEvent(m_GameObject, "OnInventoryRespawned", OnInventoryRespawned);

            // Animation events cannot have multiple parameters so use the event name to determine which slot to equip/unequip.
            m_OnAnimatorItemEquip = OnAnimatorItemEquip;
            m_OnAnimatorItemEquipComplete = OnAnimatorItemEquipComplete;
            m_OnAnimatorItemUnequip = OnAnimatorItemUnequip;
            m_OnAnimatorItemUnequipComplete = OnAnimatorItemUnequipComplete;
        }

        /// <summary>
        /// The ItemPickup component is starting to pick up ItemIdentifiers.
        /// </summary>
        public void WillStartPickup()
        {
            // Remember the initial item inventory list to be able to determine if an item has been added.
            m_InventoryAmount.Clear();
            var allItems = m_Inventory.GetAllCharacterItems();
            for (int i = 0; i < allItems.Count; ++i) {
                // A duplicate item will exist if the item shares IItemIdentifiers.
                if (m_InventoryAmount.ContainsKey(allItems[i].ItemIdentifier)) {
                    continue;
                }

                m_InventoryAmount.Add(allItems[i].ItemIdentifier, m_Inventory.GetItemIdentifierAmount(allItems[i].ItemIdentifier));
            }
            m_PlayEquipAudio = true;
        }

        /// <summary>
        /// The ItemPickup component is no longer picking up any ItemIdentifiers.
        /// </summary>
        private void StopPickup()
        {
            m_PlayEquipAudio = false;
        }

        /// <summary>
        /// An item has been picked up within the inventory. Determine if the ability should start.
        /// </summary>
        /// <param name="characterItem">The item that has been equipped.</param>
        /// <param name="amount">The amount of item picked up.</param>
        /// <param name="immediatePickup">Was the item be picked up immediately?</param>
        /// <param name="forceEquip">Should the item be force equipped?</param>
        private void OnPickupItem(CharacterItem characterItem, int amount, bool immediatePickup, bool forceEquip)
        {
            // The ability doesn't need to respond if the category doesn't match.
            if (!m_ItemSetManager.IsCategoryMember(characterItem.ItemDefinition, m_ItemSetGroupIndex) || !Enabled) {
                return;
            }

#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
            if (m_NetworkInfo != null && !m_NetworkInfo.HasAuthority()) {
                return;
            }
#endif

            // If another EquipUnequip ability exists with an exact category match then that ability should be used instead.
            if (m_ItemSetManager.ItemSetGroups[m_ItemSetGroupIndex].ItemCategory.ID != characterItem.ItemDefinition.GetItemCategory().ID) {
                for (int i = 0; i < m_EquipUnequipAbilities.Length; ++i) {
                    if (m_EquipUnequipAbilities[i] == this) {
                        continue;
                    }

                    if (m_ItemSetManager.ItemSetGroups[m_EquipUnequipAbilities[i].ItemSetGroupIndex].ItemCategory.ID == characterItem.ItemDefinition.GetItemCategory().ID) {
                        return;
                    }
                }
            }

            // Determine if the item should be auto equipped. There are a variety of circumstances which will allow the item to be equipped.
            if (!ShouldEquip(characterItem.ItemIdentifier, characterItem.SlotID, amount)) { return; }
            
            // The ItemSetManager will manage which items are equipped.
            var itemSetIndex = m_ItemSetManager.GetItemSetIndex(characterItem, m_ItemSetGroupIndex, true);
            // The itemSet may not be valid for the item yet. 
            if (itemSetIndex != -1) {
                // The ItemSet can be equipped immediately or play the equip animation. If equipping immediately ensure
                // the character starts with the topmost ItemSet no matter the init order.
                if (immediatePickup && (forceEquip || m_ActiveItemSetIndex == -1 || itemSetIndex < m_ActiveItemSetIndex)) {
                    // The ItemSet should be updated immediately.
                    m_StartEquipUnequipIndex = m_ActiveItemSetIndex = itemSetIndex;
                    EventHandler.ExecuteEvent<int>(this, "OnEquipUnequipItemSetIndexChange", m_ActiveItemSetIndex);
                    m_ItemSetManager.UpdateNextItemSet(m_ItemSetGroupIndex, m_ActiveItemSetIndex);
                    m_ItemSetManager.UpdateActiveItemSet(m_ItemSetGroupIndex, m_ActiveItemSetIndex);

                    for (int i = 0; i < m_Inventory.SlotCount; ++i) {
                        // The current slot will be updated immediately.
                        ForceEquipUnequip(i, false);

                        var itemIdentifier = m_ItemSetManager.GetEquipItemIdentifier(m_ItemSetGroupIndex, itemSetIndex, i);
                        if (itemIdentifier == null) {
                            // Unequip the current item if no items should be equipped with the current item set.
                            var unequipItem = m_Inventory.GetActiveCharacterItem(i);
                            if (unequipItem != null && m_ItemSetManager.IsCategoryMember(unequipItem.ItemIdentifier.GetItemDefinition(), m_ItemSetGroupIndex)) {
                                m_Inventory.UnequipItem(i);
                                EventHandler.ExecuteEvent(m_GameObject, "OnAbilityUnequipItemComplete", unequipItem, i);
                            }

                            continue;
                        }
                        // Only manage the ItemIdentifier if the category matches.
                        if (m_ItemSetManager.IsCategoryMember(itemIdentifier.GetItemDefinition(), m_ItemSetGroupIndex)) {
                            var equippedItem = m_Inventory.GetActiveCharacterItem(i);
                            if (equippedItem != null) {
                                // No changes are necessary if the ItemIdentifier that should be equipped is the same as the ItemIdentifier that is already equipped.
                                if (itemIdentifier == equippedItem.ItemIdentifier) {
                                    continue;
                                }
                                m_Inventory.UnequipItem(i);
                                EventHandler.ExecuteEvent(m_GameObject, "OnAbilityUnequipItemComplete", equippedItem, i);
                            }
                            var equipItem = m_Inventory.GetCharacterItem(itemIdentifier, i);
                            if (equipItem != null) {
                                InvokeWillEquipItem(equipItem, i);
                                equipItem.StartEquip(false);
                            }
                            m_Inventory.EquipItem(itemIdentifier, i, !m_PlayEquipAudio);
                        }
                    }
                } else if (forceEquip) {
                    StartEquipUnequip(itemSetIndex);
                }
            } else if (m_ItemSetManager.GetActiveItemSetIndex(m_ItemSetGroupIndex) == -1) {
                // The ItemSet isn't currently valid. Set the default ItemSet so at least one ItemSet is active which will allow the states to switch
                // the ItemSet when it becomes valid.
                m_ItemSetManager.SetDefaultItemSet(m_ItemSetGroupIndex);
            }
            
        }

        /// <summary>
        /// Invokes WillEquip on the specified item.
        /// </summary>
        /// <param name="equipCharacterItem">The item being equipped.</param>
        /// <param name="slotID">The slot ID of the item that is trying to be equipped.</param>
        private void InvokeWillEquipItem(CharacterItem equipCharacterItem, int slotID)
        {
            EventHandler.ExecuteEvent(m_GameObject, "OnAbilityWillEquipItem", equipCharacterItem, slotID);
            equipCharacterItem.WillEquip();
        }

        /// <summary>
        /// Should the item be equipped?
        /// </summary>
        /// <param name="itemIdentifier">The IItemIdentifier that may be equipped.</param>
        /// <param name="slotID">The ID of the slot may be equipped.</param>
        /// <param name="amount">The amount of item picked up.</param>
        /// <returns>True if the item should be equipped.</returns>
        public bool ShouldEquip(IItemIdentifier itemIdentifier, int slotID, int amount)
        {
            // The character shouldn't equip the item if an item is currently in use or is reloading.
            if (m_CharacterLocomotion.IsAbilityTypeActive<Use>() || m_CharacterLocomotion.IsAbilityTypeActive<Reload>()) {
                return false;
            }

            var shouldEquip = false;
            var currentItem = m_Inventory.GetActiveCharacterItem(slotID);
            if (!m_InventoryAmount.TryGetValue(itemIdentifier, out var itemAmount)) {
                itemAmount = -1;
            }
            if ((m_AutoEquip & AutoEquipType.Always) != 0) {
                shouldEquip = true;
            } else if ((m_AutoEquip & AutoEquipType.Unequipped) != 0 && currentItem == null && m_EquipItems[slotID] == null) {
                shouldEquip = true;
            } else if ((m_AutoEquip & AutoEquipType.NotPreset) != 0 && itemAmount < amount) {
                shouldEquip = true;
            } else if ((m_AutoEquip & AutoEquipType.FirstTime) != 0 && itemAmount <= 0) {
                shouldEquip = true;
            } else if ((m_AutoEquip & AutoEquipType.OutOfUsableItem) != 0 && currentItem != null && currentItem.ItemActions != null) {
                for (int i = 0; i < currentItem.ItemActions.Length; ++i){
                    var usableItem = currentItem.ItemActions[i] as IUsableItem;
                    if (usableItem == null) {
                        continue;
                    }

                    //If the current item should unequip then the new one can be equipped.
                    if (usableItem.ShouldUnequip()) {
                        shouldEquip = true;
                        break;
                    }
                }
            }

            // An active ability may prevent the item equip.
            if (shouldEquip && !IsEquipAllowed(-1, itemIdentifier.GetItemDefinition(), slotID)) {
                shouldEquip = false;
            }

            return shouldEquip;
        }

        /// <summary>
        /// Can the specified ItemDefinition be equipped? An ability may prevent it from being equipped.
        /// </summary>
        /// <param name="itemSetIndex">The index of the ItemSet that is being equipped.</param>
        /// <param name="itemDefinition">The ItemDefinition of the item that is trying to be equipped.</param>
        /// <param name="slotID">The slot ID of the item that is trying to be equipped.</param>
        /// <returns>True if the ItemIdentifier can be equipped.</returns>
        private bool IsEquipAllowed(int itemSetIndex, ItemDefinitionBase itemDefinition, int slotID)
        {
            if (itemDefinition == null || (itemSetIndex != -1 && m_ItemSetManager != null && m_ItemSetManager.ItemSetGroups[m_ItemSetGroupIndex].DefaultItemSetIndex == itemSetIndex)) {
                return true;
            }
            for (int i = 0; i < m_CharacterLocomotion.ActiveAbilityCount; ++i) {
                var ability = m_CharacterLocomotion.ActiveAbilities[i];
                if (!MathUtility.InLayerMask(slotID, ability.AllowEquippedSlotsMask)) {
                    return false;
                }

                // The AllowItemIdentifiers list may prevent the item from being equipped.
                if (ability.AllowItemDefinitions != null && ability.AllowItemDefinitions.Length > 0) {
                    var allowed = false;
                    for (int j = 0; j < ability.AllowItemDefinitions.Length; ++j) {
                        if (ability.AllowItemDefinitions[j] == itemDefinition) {
                            allowed = true;
                            break;
                        }
                    }
                    if (!allowed) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// An ItemIdentifier has been picked up within the inventory.
        /// </summary>
        /// <param name="itemIdentifier">The ItemIdentifier that has been picked up.</param>
        /// <param name="amount">The amount of item picked up.</param>
        /// <param name="immediatePickup">Was the item be picked up immediately?</param>
        /// <param name="forceEquip">Should the item be force equipped?</param>
        private void OnPickupItemIdentifier(IItemIdentifier itemIdentifier, int amount, bool immediatePickup, bool forceEquip)
        {
            // The ability doesn't need to respond if the category doesn't match.
            var itemDefinition = itemIdentifier.GetItemDefinition();
            if (!m_ItemSetManager.IsCategoryMember(itemDefinition, m_ItemSetGroupIndex) || !Enabled) {
                return;
            }
        }

        /// <summary>
        /// Starts equipping/unequipping to the specified ItemSet.
        /// </summary>
        /// <param name="itemSetIndex">The ItemSet to equip/unequip the items to.</param>
        /// <returns>True if the ability was started.</returns>
        public bool StartEquipUnequip(int itemSetIndex)
        {
            return StartEquipUnequip(itemSetIndex, false);
        }

        /// <summary>
        /// Starts equipping/unequipping to the specified ItemSet.
        /// </summary>
        /// <param name="itemSetIndex">The ItemSet to equip/unequip the items to.</param>
        /// <param name="forceEquipUnequip">Should the ability be force started? This will stop all abilities that would prevent EquipUnequip from starting.</param>
        /// <returns>True if the ability was started.</returns>
        public bool StartEquipUnequip(int itemSetIndex, bool forceEquipUnequip)
        {
            return StartEquipUnequip(itemSetIndex, forceEquipUnequip, m_ImmediateEquipUnequip);
        }

        /// <summary>
        /// Starts equipping/unequipping to the specified ItemSet.
        /// </summary>
        /// <param name="itemSetIndex">The ItemSet to equip/unequip the items to.</param>
        /// <param name="forceEquipUnequip">Should the ability be force started? This will stop all abilities that would prevent EquipUnequip from starting.</param>
        /// <param name="immediateEquipUnequip">Should the items be equipped or unequipped immediately?</param>
        /// <returns>True if the ability was started.</returns>
        public bool StartEquipUnequip(int itemSetIndex, bool forceEquipUnequip, bool immediateEquipUnequip)
        {
            // No actions are necessary if the item set is already equipped.
            if ((!IsActive && itemSetIndex == m_ItemSetManager.GetActiveItemSetIndex(m_ItemSetGroupIndex)) || 
                (IsActive && !immediateEquipUnequip && itemSetIndex != -1 && itemSetIndex == m_ItemSetManager.GetNextItemSetIndex(m_ItemSetGroupIndex))) {
                return false;
            }

            // Equip unequip normally will not start if use or reload is active. If the ability is forced then it should first stop the abilities.
            if (forceEquipUnequip) {
                var activeItemAbilities = m_CharacterLocomotion.ActiveItemAbilities;
                for (int i = m_CharacterLocomotion.ActiveItemAbilityCount - 1; i > -1; --i) {
                    if (activeItemAbilities[i] is Use || activeItemAbilities[i] is Reload) {
                        m_CharacterLocomotion.TryStopAbility(activeItemAbilities[i], true);
                    }
                }
            }
            
            m_ActiveItemSetIndex = itemSetIndex;
            m_ImmediateEquipUnequip = immediateEquipUnequip;
            return StartAbility();
        }

        /// <summary>
        /// Can the ability be started?
        /// </summary>
        /// <returns>True if the ability can be started.</returns>
        public override bool CanStartAbility()
        {
            // An attribute may prevent the ability from starting.
            if (!base.CanStartAbility()) {
                return false;
            }

            // If the InputIndex is -1 then the ability has been started manually.
            var itemSetIndex = m_ActiveItemSetIndex;
            if (InputIndex != -1) { // Item specified by button index.
                itemSetIndex = InputIndex;
            }

            // Don't start if the ItemSetIndex is the same or invalid. The check has already been performed for the manual start type.
            if (InputIndex != -1 && (!m_ItemSetManager.IsItemSetValid(m_ItemSetGroupIndex, itemSetIndex, true) || 
                                    (!IsActive && itemSetIndex == m_ItemSetManager.GetActiveItemSetIndex(m_ItemSetGroupIndex)) ||
                                    (IsActive && itemSetIndex == m_ItemSetManager.GetNextItemSetIndex(m_ItemSetGroupIndex)))) {
                InputIndex = -1;
                return false;
            }

            // Don't start the ability if another ability is preventing the ability from being equipped.
            for (int i = 0; i < m_Inventory.SlotCount; ++i) {
                var itemIdentifier = m_ItemSetManager.GetEquipItemIdentifier(m_ItemSetGroupIndex, itemSetIndex, i);
                if (itemIdentifier != null && !IsEquipAllowed(itemSetIndex, itemIdentifier.GetItemDefinition(), i)) {
                    return false;
                }
            }

            // Don't try to equip to an invalid ItemSet index.
            if (itemSetIndex >= m_ItemSetManager.ItemSetGroups[m_ItemSetGroupIndex].ItemSetList.Count) {
                return false;
            }

            m_ActiveItemSetIndex = itemSetIndex;

            return true;
        }

        /// <summary>
        /// The ability has started.
        /// </summary>
        protected override void AbilityStarted()
        {
            base.AbilityStarted();

            m_StartEquipUnequip = true;
            m_StartEquipUnequipIndex = m_ActiveItemSetIndex;
            if (m_ImmediateEquipUnequip) {
                Update();
            }
        }

        /// <summary>
        /// Called when another ability is attempting to start and the current ability is active.
        /// Returns true or false depending on if the new ability should be blocked from starting.
        /// </summary>
        /// <param name="startingAbility">The ability that is starting.</param>
        /// <returns>True if the ability should be blocked.</returns>
        public override bool ShouldBlockAbilityStart(Ability startingAbility)
        {
            if (base.ShouldBlockAbilityStart(startingAbility)) {
                return true;
            }
            if (startingAbility is EquipScroll) {
                return true;
            }
            // The item can't be used or reloaded if it is being unequipped.
            if (startingAbility is Use || startingAbility is Reload) {
                for (int i = 0; i < m_UnequipItems.Length; ++i) {
                    if (m_UnequipItems[i] != null) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the Item State Index which corresponds to the slot ID.
        /// </summary>
        /// <param name="slotID">The ID of the slot that corresponds to the Item State Index.</param>
        /// <returns>The Item State Index which corresponds to the slot ID.</returns>
        public override int GetItemStateIndex(int slotID)
        {
            if (m_UnequipItems[slotID] != null) {
                return m_UnequipItemStateIndex;
            }
            if (m_CanEquip && m_EquipItems[slotID] != null) {
                return m_EquipItemStateIndex;
            }
            return -1;
        }

        /// <summary>
        /// Returns the Item Substate Index which corresponds to the slot ID.
        /// </summary>
        /// <param name="slotID">The ID of the slot that corresponds to the Item Substate Index.</param>
        /// <returns>The Item Substate Index which corresponds to the slot ID.</returns>
        public override int GetItemSubstateIndex(int slotID)
        {
            if (m_UnequipItems[slotID] != null) {
                return m_UnequipItems[slotID].UnequipAnimatorAudioStateSet.GetItemSubstateIndex() + (m_Aiming ? m_AimItemSubstateIndexAddition : 0);
            }
            if (m_CanEquip && m_EquipItems[slotID] != null) {
                return m_EquipItems[slotID].EquipAnimatorAudioStateSet.GetItemSubstateIndex() + (m_Aiming ? m_AimItemSubstateIndexAddition : 0);
            }
            return -1;
        }

        /// <summary>
        /// Updates the ability
        /// </summary>
        public override void Update()
        {
            base.Update();

            if (m_StartEquipUnequip) {
                m_StartEquipUnequip = false;
                
                // The ActiveItemIndexSetIndex may have been changed due to the EquipUnequipAction above.
                m_ActiveItemSetIndex = m_StartEquipUnequipIndex;

                // The ItemSet may no longer be valid between the time it was first checked and when the ability actually started.
                var isItemSetValid = m_ItemSetManager.IsItemSetValid(m_ItemSetGroupIndex, m_ActiveItemSetIndex, false);
                if (m_ActiveItemSetIndex != -1 && !isItemSetValid) {
                    StopAbility();
                    return;
                }

                // Equip any unequipped items which are within the ItemSetIndex and belong to the specified category.
                // Unequip any items which are equipped and are not within the ItemSetIndex and belong to the specified category.
                var unequip = false;
                var equip = false;
                for (int i = 0; i < m_Inventory.SlotCount; ++i) {
                    // Stop any equips/unequips that have already started.
                    ForceEquipUnequip(i, true);
                    m_EquipUnequipActions[i] = EquipUnequipAction.Inactive;

                    // Determine the item that is currently equipped and the item that should be equipped.
                    CharacterItem currentCharacterItem = null, targetCharacterItem = null;
                    var currentItemIdentifier = m_ItemSetManager.GetEquipItemIdentifier(m_ItemSetGroupIndex, i);
                    if (currentItemIdentifier != null) {
                        currentCharacterItem = m_Inventory.GetCharacterItem(currentItemIdentifier, i);
                    }
                    var targetItemIdentifier = m_ItemSetManager.GetEquipItemIdentifier(m_ItemSetGroupIndex, m_ActiveItemSetIndex, i);
                    var skipEquip = false;
                    // If the target ItemIdentifier doesn't equal the equip ItemIdentifier from the ItemSetManager then the equip ItemIdentifier is equipped in a different category.
                    // Only the lower categories should be searched because they have a higher priority.
                    for (int j = 0; j < m_ItemSetGroupIndex; ++j) {
                        var equipItemIdentifier = m_ItemSetManager.GetEquipItemIdentifier(j, i);
                        if (equipItemIdentifier != null && equipItemIdentifier != targetItemIdentifier) {
                            skipEquip = true;
                            break;
                        }
                    }
                    if (skipEquip) {
                        continue;
                    }
                    if (targetItemIdentifier != null && m_Inventory.GetItemIdentifierAmount(targetItemIdentifier) > 0) {
                        targetCharacterItem = m_Inventory.GetCharacterItem(targetItemIdentifier, i);
                    }

                    // Nothing needs to be done if the current item is equal to the item that should be equipped. The target item may not be active if
                    // the ability is quickly moving through the items.
                    if (currentCharacterItem == targetCharacterItem && (targetCharacterItem == null || (targetCharacterItem != null && targetCharacterItem.IsActive()))) {
                        continue;
                    }
                    // ForceEquipUnequip may be unequipping the item.
                    if (m_UnequipItems[i] != null) {
                        unequip = true;
                    } else if (currentCharacterItem != targetCharacterItem && currentCharacterItem != null && 
                                m_Inventory.GetActiveCharacterItem(currentCharacterItem.SlotID) == currentCharacterItem && 
                                m_ItemSetManager.IsCategoryMember(currentCharacterItem.ItemIdentifier.GetItemDefinition(), m_ItemSetGroupIndex)) {
                        // The item first needs to be unequipped before another item can be equipped.
                        SetUnequipItem(i, currentCharacterItem);
                        m_UnequippingItems[i] = true;
                        unequip = true;
                        currentCharacterItem.StartUnequip(m_ImmediateEquipUnequip);
                    }
                    if (targetItemIdentifier != null && m_ItemSetManager.IsCategoryMember(targetItemIdentifier.GetItemDefinition(), m_ItemSetGroupIndex)) {
                        SetEquipItem(i, targetCharacterItem);
                        // Wait to equip until unequip is complete.
                        m_EquippingItems[i] = false;
                        equip = true;
                    }
                }

                // The ability can be stopped if no action needs to be performed.
                if (unequip || equip) {
                    EventHandler.ExecuteEvent<int>(this, "OnEquipUnequipItemSetIndexChange", m_ActiveItemSetIndex);
                    m_ItemSetManager.UpdateNextItemSet(m_ItemSetGroupIndex, m_ActiveItemSetIndex);

                    m_CanEquip = !unequip; // The ability can equip as soon as the unequips are complete.
                    var canEqup = m_CanEquip; // The can equp status may change if an item is unequipped immediately. Remember the initial value so the item isn't equipped twice.
                                              // Wait to schedule the events until after all of the equip/unequip items have been determined. Otherwise if the event is fired immediate (with a duration
                                              // of 0) then the ability could end before all slots have a chance to determine if any items need to be equipped/unequipped.
                    for (int i = 0; i < m_Inventory.SlotCount; ++i) {
                        if (m_UnequipItems[i] == null && m_EquipItems[i] == null) {
                            continue;
                        }

                        if (m_UnequipItems[i] != null) {
                            if (m_ImmediateEquipUnequip) {
                                ItemUnequip(i, true);
                            } else {
                                m_UnequipItems[i].UnequipEvent.WaitForEvent(false);
                            }
                        } else if (m_EquipItems[i] != null && canEqup) {
                            m_EquippingItems[i] = true;
                            InvokeWillEquipItem(m_EquipItems[i], i);
                            if (m_ImmediateEquipUnequip) {
                                ItemEquip(i, true);
                            } else {
                                m_EquipItems[i].EquipEvent.WaitForEvent(false);
                            }
                        }
                    }
                    
                    UpdateItemAbilityAnimatorParameters();
                } else {
                    EventHandler.ExecuteEvent<int>(this, "OnEquipUnequipItemSetIndexChange", m_ActiveItemSetIndex);
                    StopAbility();
                }
                return;
            }

            // The equip/unequip actions may occur during an animation event which will occur after the animator has updated. To prevent the animator from being out
            // of sync with the controller for a frame the ability should be updated within the Update loop.
            for (int i = 0; i < m_EquipUnequipActions.Length; ++i) {
                if (m_EquipUnequipActions[i] == EquipUnequipAction.Inactive) {
                    continue;
                }
                switch (m_EquipUnequipActions[i]) {
                    case EquipUnequipAction.Unequip:
                        ItemUnequip(i, true);
                        break;
                    case EquipUnequipAction.UnequipComplete:
                        ItemUnequipComplete(i, true);
                        break;
                    case EquipUnequipAction.Equip:
                        ItemEquip(i, true);
                        break;
                    case EquipUnequipAction.EquipComplete:
                        ItemEquipComplete(i, true);
                        break;
                }
            }
        }

        /// <summary>
        /// Set the equip item.
        /// </summary>
        /// <param name="slotID">The slot ide to assign the equip item to.</param>
        /// <param name="characterItem">The character item to set to that slot.</param>
        private void SetEquipItem(int slotID, CharacterItem characterItem)
        {
            var previous = m_EquipItems[slotID];
            if (previous != null) {
                RegisterUnregisterEquipAnimationEvents(previous, false);
            }

            m_EquipItems[slotID] = characterItem;
            if (characterItem != null) {
                RegisterUnregisterEquipAnimationEvents(characterItem, true);
            }
        }
        
        /// <summary>
        /// Set the unequip item.
        /// </summary>
        /// <param name="slotID">The slot ide to assign the unequip item to.</param>
        /// <param name="characterItem">The character item to set to that slot.</param>
        private void SetUnequipItem(int slotID, CharacterItem characterItem)
        {
            var previous = m_UnequipItems[slotID];
            if (previous != null) {
                RegisterUnregisterUnequipAnimationEvents(previous, false);
            }
           
            m_UnequipItems[slotID] = characterItem;
            if (characterItem != null) {
                RegisterUnregisterUnequipAnimationEvents(characterItem, true);
            }
        }

        /// <summary>
        /// Register or Unregister the animator equip animation events.
        /// </summary>
        /// <param name="characterItem">The character item to register the event for.</param>
        /// <param name="register">Register or unregister?</param>
        protected virtual void RegisterUnregisterEquipAnimationEvents(CharacterItem characterItem, bool register)
        {
            if (register) {
                characterItem.OnAnimatorItemEquipEvent += m_OnAnimatorItemEquip;
                characterItem.OnAnimatorItemEquipCompleteEvent += m_OnAnimatorItemEquipComplete;
            } else {
                characterItem.OnAnimatorItemEquipEvent -= m_OnAnimatorItemEquip;
                characterItem.OnAnimatorItemEquipCompleteEvent -= m_OnAnimatorItemEquipComplete;
            }
        }
        
        /// <summary>
        /// Register or Unregister the animator unequip animation events.
        /// </summary>
        /// <param name="characterItem">The character item to register the event for.</param>
        /// <param name="register">Register or unregister?</param>
        protected virtual void RegisterUnregisterUnequipAnimationEvents(CharacterItem characterItem, bool register)
        {
            if (register) {
                characterItem.OnAnimatorItemUnequipEvent += m_OnAnimatorItemUnequip;
                characterItem.OnAnimatorItemUnequipCompleteEvent += m_OnAnimatorItemUnequipComplete;
            } else {
                characterItem.OnAnimatorItemUnequipEvent -= m_OnAnimatorItemUnequip;
                characterItem.OnAnimatorItemUnequipCompleteEvent -= m_OnAnimatorItemUnequipComplete;
            }
        }

        /// <summary>
        /// Equips and unequips all of the pending items.
        /// </summary>
        /// <param name="slotID">The slot ID to equip or unequip the item at.</param>
        /// <param name="startAbility">Is the ability being started?</param>
        private void ForceEquipUnequip(int slotID, bool startAbility)
        {
            if (m_EquipItems[slotID] != null || m_UnequipItems[slotID] != null) {
                // Cancel the pending events.
                if (m_EquipItems[slotID] != null) {
                    m_EquipItems[slotID].EquipEvent.CancelWaitForEvent();
                    m_EquipItems[slotID].EquipCompleteEvent.CancelWaitForEvent();
                }
                
                if (m_UnequipItems[slotID] != null) {
                    m_UnequipItems[slotID].UnequipEvent.CancelWaitForEvent();
                    m_UnequipItems[slotID].UnequipCompleteEvent.CancelWaitForEvent();
                }
                
                var startUnequip = false;
                // If the item is no longer being equipped then it is unequipped.
                if (startAbility) {
                    if (m_EquipItems[slotID] != null && m_EquippingItems[slotID]) {
                        m_EquipItems[slotID].StartUnequip(true);
                        m_Inventory.UnequipItem(m_EquipItems[slotID].ItemIdentifier, slotID);
                        EventHandler.ExecuteEvent(m_GameObject, "OnAbilityUnequipItemComplete", m_EquipItems[slotID], slotID);
                    } else if (m_EquipItems[slotID] != null && m_CanEquip) {
                        // Don't unequip the item currently being equipped if the ItemSet uses it.
                        var equipItemIdentifier = m_ItemSetManager.GetEquipItemIdentifier(m_ItemSetGroupIndex, m_ActiveItemSetIndex, slotID);
                        if (equipItemIdentifier == null || m_EquipItems[slotID].ItemIdentifier != equipItemIdentifier) {
                            if (m_UnequipItems[slotID] != null) {
                                m_Inventory.UnequipItem(m_UnequipItems[slotID].ItemIdentifier, slotID);
                                EventHandler.ExecuteEvent(m_GameObject, "OnAbilityUnequipItemComplete", m_UnequipItems[slotID], slotID);
                            }

                            // The equipping item should be unequipped smoothly.
                            SetUnequipItem(slotID, m_EquipItems[slotID]);
                            m_UnequippingItems[slotID] = true;
                            m_UnequipItems[slotID].StartUnequip(m_ImmediateEquipUnequip);
                            startUnequip = true;
                        }
                    }
                } else {
                    // Equip the equip item immediately if the ability is being stopped to prevent the items from getting into an invalid state.
                    if (m_EquipItems[slotID] != null) {
                        InvokeWillEquipItem(m_EquipItems[slotID], slotID);
                        m_EquipItems[slotID].StartEquip(m_ImmediateEquipUnequip);
                        if (m_Inventory.GetActiveCharacterItem(slotID) != m_EquipItems[slotID]) {
                            m_Inventory.EquipItem(m_EquipItems[slotID].ItemIdentifier, slotID, m_ImmediateEquipUnequip);
                        }
                    }
                }
                
                SetEquipItem(slotID, null);
                m_EquippingItems[slotID] = false;

                if (!startUnequip && m_UnequipItems[slotID] != null) {
                    if (m_UnequippingItems[slotID] && startAbility) {
                        // Reset the unequpping item back to being equip if the item set uses the same item again. If a different item should be equipped
                        // then the ability will unequip it through AbilityStarted.
                        var itemIdentifier = m_ItemSetManager.GetEquipItemIdentifier(m_ItemSetGroupIndex, m_ActiveItemSetIndex, slotID);
                        if (itemIdentifier == m_UnequipItems[slotID].ItemIdentifier) {
                            InvokeWillEquipItem(m_UnequipItems[slotID], slotID);
                            m_UnequipItems[slotID].StartEquip(m_ImmediateEquipUnequip);
                            if (m_Inventory.GetActiveCharacterItem(slotID) != m_UnequipItems[slotID]) {
                                m_Inventory.EquipItem(m_UnequipItems[slotID].ItemIdentifier, slotID, true);
                            }
                            SetUnequipItem(slotID, null);
                            m_UnequippingItems[slotID] = false;
                        } else if (m_EquipUnequipActions[slotID] == EquipUnequipAction.Unequip) {
                            m_Inventory.UnequipItem(m_UnequipItems[slotID].ItemIdentifier, slotID);
                            EventHandler.ExecuteEvent(m_GameObject, "OnAbilityUnequipItemComplete", m_UnequipItems[slotID], slotID);
                            SetUnequipItem(slotID, null);
                            m_UnequippingItems[slotID] = false;
                        }
                    } else {
                        m_UnequipItems[slotID].StartUnequip(true);
                        m_Inventory.UnequipItem(m_UnequipItems[slotID].ItemIdentifier, slotID);
                        EventHandler.ExecuteEvent(m_GameObject, "OnAbilityUnequipItemComplete", m_UnequipItems[slotID], slotID);
                        SetUnequipItem(slotID, null);
                        m_UnequippingItems[slotID] = false;
                    }
                }

                UpdateItemAbilityAnimatorParameters();
            }
        }

        /// <summary>
        /// The Aim ability has started or stopped.
        /// </summary>
        /// <param name="aim">Has the Aim ability started?</param>
        /// <param name="inputStart">Was the ability started from input?</param>
        private void OnAim(bool aim, bool inputStart)
        {
            if (!inputStart) {
                return;
            }
            m_Aiming = aim;
        }

        /// <summary>
        /// The Animation Event has equipped the item in the specified slot.
        /// </summary>
        /// <param name="slotID">The slot Id of the item equipped.</param>
        private void OnAnimatorItemEquip(int slotID)
        {
            if (m_EquipItems[slotID] != null) {
                // Can update if the event is instant.
                var animationEvent = m_EquipItems[slotID].EquipEvent;
                var canUpdate = !animationEvent.WaitForAnimationEvent && animationEvent.Duration <= 0;
                ItemEquip(slotID, canUpdate);
            }
        }
        
        /// <summary>
        /// The Animation Event has completed the equip of the item in the specified slot.
        /// </summary>
        /// <param name="slotID">The slot Id of the item equipped.</param>
        private void OnAnimatorItemEquipComplete(int slotID)
        {
            if (m_EquipItems[slotID] != null) {
                // Can update if the event is instant.
                var animationEvent = m_EquipItems[slotID].EquipCompleteEvent;
                var canUpdate = animationEvent.WaitForAnimationEvent == false
                                && animationEvent.Duration <= 0;
                ItemEquipComplete(slotID, canUpdate);
            }
        }
        
        /// <summary>
        /// The Animation Event has unequipped the item in the specified slot.
        /// </summary>
        /// <param name="slotID">The slot Id of the item unequipped.</param>
        private void OnAnimatorItemUnequip(int slotID)
        {
            if (m_UnequipItems[slotID] != null) {
                // Can update if the event is instant.
                var animationEvent = m_UnequipItems[slotID].UnequipEvent;
                var canUpdate = animationEvent.WaitForAnimationEvent == false
                                && animationEvent.Duration <= 0;
                ItemUnequip(slotID, canUpdate);
            }
        }
        
        /// <summary>
        /// The Animation Event has completed the unequip of the item in the specified slot.
        /// </summary>
        /// <param name="slotID">The slot Id of the item equipped.</param>
        private void OnAnimatorItemUnequipComplete(int slotID)
        {
            if (m_UnequipItems[slotID] != null) {
                // Can update if the event is instant.
                var animationEvent = m_UnequipItems[slotID].UnequipCompleteEvent;
                var canUpdate = animationEvent.WaitForAnimationEvent == false
                                && animationEvent.Duration <= 0;
                ItemUnequipComplete(slotID, canUpdate);
            }
        }

        /// <summary>
        /// The animation is done unequipping the item.
        /// </summary>
        /// <param name="slotID">The slot ID of the item that was unequipped.</param>
        /// <param name="canUpdate">Can the item be updated? If false the status enum will be set and the item will be updated within the Update loop.</param>
        private void ItemUnequip(int slotID, bool canUpdate)
        {
            var unequipItem = m_UnequipItems[slotID];
            if (unequipItem == null || !m_UnequippingItems[slotID]) {
                return;
            }

            // If the item can't be updated then the event should wait until the Update loop. This ensures items are updated in the proper order.
            if (!canUpdate) {
                m_EquipUnequipActions[slotID] = EquipUnequipAction.Unequip;
                return;
            }
            m_EquipUnequipActions[slotID] = EquipUnequipAction.Inactive;

            // Clear out the unequipped item and notify those interested.
            m_Inventory.UnequipItem(slotID);
            m_UnequippingItems[slotID] = false;
            
            if (m_ImmediateEquipUnequip) {
                ItemUnequipComplete(slotID, true);
            } else {
                unequipItem.UnequipCompleteEvent.WaitForEvent();
            }
        }

        /// <summary>
        /// The animation is done unequipping the item.
        /// </summary>
        /// <param name="slotID">The slot ID of the item that was unequipped.</param>
        /// <param name="canUpdate">Can the item be updated? If false the status enum will be set and the item will be updated within the Update loop.</param>
        private void ItemUnequipComplete(int slotID, bool canUpdate)
        {
            var unequipItem = m_UnequipItems[slotID];
            if (unequipItem == null) {
                return;
            }

            // If the item can't be updated then the event should wait until the Update loop. This ensures items are updated in the proper order.
            if (!canUpdate) {
                m_EquipUnequipActions[slotID] = EquipUnequipAction.UnequipComplete;
                return;
            }
            m_EquipUnequipActions[slotID] = EquipUnequipAction.Inactive;

            unequipItem.UnequipCompleteEvent.CancelWaitForEvent();
            SetUnequipItem(slotID, null);
            EventHandler.ExecuteEvent(m_GameObject, "OnAbilityUnequipItemComplete", unequipItem, slotID);
            UpdateItemAbilityAnimatorParameters();

            // The ability shouldn't start to equip or stop until all unequip items have been unequipped.
            for (int i = 0; i < m_UnequipItems.Length; ++i) {
                if (m_UnequipItems[i] != null) {
                    return;
                }
            }

            // Start equipping the next item if all of the items have been unequipped. The next item doesn't have to be in the same slot 
            // because the item could have been waiting for the item in the current slot to be unequipped.
            m_CanEquip = true;
            var stopAbility = true;
            for (int i = 0; i < m_EquipItems.Length; ++i) {
                if (m_EquipItems[i] != null && !m_EquippingItems[i]) {
                    stopAbility = false;
                    m_EquippingItems[i] = true;
                    InvokeWillEquipItem(m_EquipItems[i], i);
                    // EquipItems[i] may be null if the item has an equip duration of 0.
                    if (m_EquipItems[i] != null) {
                        if (m_ImmediateEquipUnequip) {
                            ItemEquip(i, true);
                        } else {
                            m_EquipItems[i].EquipEvent.WaitForEvent();
                        }
                    }
                }
            }

            // Stop the ability if no items need to be unequipped/equipped.
            if (!m_StartEquipUnequip && stopAbility) {
                TryStopEquipUnequipAbility();
            }
        }

        /// <summary>
        /// An item was just equipped. Verify that the unequip item in the specified slot still needs to be unequipped.
        /// </summary>
        /// <param name="categoryIndex">The index that the item was equipped.</param>
        /// <param name="slotID">The slot ID of the equipped item.</param>
        private void OnVerifyUnequipItem(int categoryIndex, int slotID)
        {
            if (m_UnequipItems[slotID] == null || m_ItemSetGroupIndex == categoryIndex) {
                return;
            }

            // Don't unequip the item if it should be equipped.
            if (m_UnequipItems[slotID].ItemIdentifier == m_ItemSetManager.GetEquipItemIdentifier(slotID)) {
                SetUnequipItem(slotID, null);
                m_UnequippingItems[slotID] = false;
                UpdateItemAbilityAnimatorParameters();
            }
        }

        /// <summary>
        /// The animation is done equipping the item.
        /// </summary>
        /// <param name="slotID">The slot that the unequipped item belongs to.</param>
        /// <param name="canUpdate">Can the item be updated? If false the status enum will be set and the item will be updated within the Update loop.</param>
        private void ItemEquip(int slotID, bool canUpdate)
        {
            if (!m_CanEquip) {
                return;
            }
            var equipItem = m_EquipItems[slotID];
            if (equipItem == null || !m_EquippingItems[slotID]) {
                return;
            }

            // If the item can't be updated then the event should wait until the Update loop. This ensures items are updated in the proper order.
            if (!canUpdate) {
                m_EquipUnequipActions[slotID] = EquipUnequipAction.Equip;
                return;
            }

            m_EquipUnequipActions[slotID] = EquipUnequipAction.Inactive;

            // Clear out the equipped item and notify those interested.
            m_EquippingItems[slotID] = false;
            UpdateItemAbilityAnimatorParameters();
            equipItem.StartEquip(m_ImmediateEquipUnequip);
            m_Inventory.EquipItem(equipItem.ItemIdentifier, slotID, m_ImmediateEquipUnequip);

            // The new ItemSet is active as soon as the new items are equipped.
            var equip = false;
            for (int i = 0; i < m_EquipItems.Length; ++i) {
                if (m_EquippingItems[slotID]) {
                    equip = true;
                    break;
                }
            }
            if (!equip) {
                m_ItemSetManager.UpdateActiveItemSet(m_ItemSetGroupIndex, m_ActiveItemSetIndex);
                // Throughout the duration that the item was equipped a different category may have started to equip. 
                // Verify that the unequip item should still be unequipped.
                EventHandler.ExecuteEvent(m_GameObject, "OnEquipUnequipVerifyUnequipItem", m_ItemSetGroupIndex, slotID);
            }
            
            if (m_ImmediateEquipUnequip) {
                ItemEquipComplete(slotID, true);
            } else {
                equipItem.EquipCompleteEvent.WaitForEvent();
            }
        }

        /// <summary>
        /// The animation is done equipping the item.
        /// </summary>
        /// <param name="slotID">The slot ID of the item that was unequipped.</param>
        /// <param name="canUpdate">Can the item be updated? If false the status enum will be set and the item will be updated within the Update loop.</param>
        private void ItemEquipComplete(int slotID, bool canUpdate)
        {
            if (!m_CanEquip) {
                return;
            }
            var equipItem = m_EquipItems[slotID];
            if (equipItem == null) {
                return;
            }

            // If the item can't be updated then the event should wait until the Update loop. This ensures items are updated in the proper order.
            if (!canUpdate) {
                m_EquipUnequipActions[slotID] = EquipUnequipAction.EquipComplete;
                return;
            }
            m_EquipUnequipActions[slotID] = EquipUnequipAction.Inactive;

            equipItem.EquipCompleteEvent.CancelWaitForEvent();
            SetEquipItem(slotID, null);

            TryStopEquipUnequipAbility();
        }

        /// <summary>
        /// The animation event has indicated that the ability should stop.
        /// </summary>
        private void TryStopEquipUnequipAbility()
        {
            // Don't stop the ability unless all slots have been equipped/unequipped.
            var stopAbility = true;
            for (int i = 0; i < m_EquipItems.Length; ++i) {
                if (m_EquipItems[i] != null || m_UnequipItems[i] != null) {
                    stopAbility = false;
                    break;
                }
            }
            if (stopAbility) {
                StopAbility();
            }
        }

        /// <summary>
        /// The ability has stopped running.
        /// </summary>
        /// <param name="force">Was the ability force stopped?</param>
        protected override void AbilityStopped(bool force)
        {
            base.AbilityStopped(force);
            m_ImmediateEquipUnequip = false;
            
            if (force) {
                // If the ability was forced to stop when it wasn't even able to start force an immediate equip/unequip.
                if (m_StartEquipUnequip) {
                    m_ImmediateEquipUnequip = true;
                    Update();
                }
                
                // If the ability was force stopped then it won't have a chance at finishing the item equip/unequip. Don't even try.
                for (int i = 0; i < m_Inventory.SlotCount; ++i) {
                    ForceEquipUnequip(i, false);
                }
            }

            UpdateItemAbilityAnimatorParameters();
            m_ItemSetManager.UpdateActiveItemSet(m_ItemSetGroupIndex, m_ActiveItemSetIndex);
        }

        /// <summary>
        /// The ItemSetManager has changed the active ItemSet.
        /// </summary>
        /// <param name="categoryIndex">The index of the category that changed.</param>
        /// <param name="itemSetIndex">The updated active ItemSet index value.</param>
        protected void OnItemSetIndexChange(int categoryIndex, int itemSetIndex)
        {
            if (m_ItemSetGroupIndex != categoryIndex) {
                return;
            }
            
            m_ActiveItemSetIndex = itemSetIndex;
        }

        /// <summary>
        /// An item has been removed.
        /// </summary>
        /// <param name="characterItem">The item that was removed.</param>
        /// <param name="slotID">The slot that the item was removed from.</param>
        private void OnRemoveItem(CharacterItem characterItem, int slotID)
        {
            if (!m_ItemSetManager.IsCategoryMember(characterItem.ItemDefinition, m_ItemSetGroupIndex)) { return; }

            // The item may not be included in the active ItemSet.
            if (m_ActiveItemSetIndex == -1 || m_ActiveItemSetIndex != m_ItemSetManager.GetItemSetIndex(characterItem, m_ItemSetGroupIndex, false)) { 
                return;
            }

            var prevImmediateEquipUnequip = m_ImmediateEquipUnequip;
            // If the ItemSet contains an item that isn't being removed then the character should animate moving to the next ItemSet.
            for (int i = 0; i < m_Inventory.SlotCount; ++i) {
                var equipItemIdentifier = m_ItemSetManager.GetEquipItemIdentifier(m_ItemSetGroupIndex, m_ActiveItemSetIndex, i);
                if (equipItemIdentifier != null && equipItemIdentifier != characterItem.ItemIdentifier) {
                    m_ImmediateEquipUnequip = false;
                    break;
                }
            }
            StartEquipUnequip(m_ItemSetManager.GetDefaultItemSetIndex(m_ItemSetGroupIndex), true, true);
            m_ImmediateEquipUnequip = prevImmediateEquipUnequip;
        }

        /// <summary>
        /// The character has died.
        /// </summary>
        /// <param name="position">The position of the force.</param>
        /// <param name="force">The amount of force which killed the character.</param>
        /// <param name="attacker">The GameObject that killed the character.</param>
        private void OnDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            m_PrevActiveItemSetIndex = m_ActiveItemSetIndex;
            // Don't immediately unequip if in first person view to allow the arms move off the screen.
            StartEquipUnequip(-1, false, !m_CharacterLocomotion.FirstPersonPerspective);

            if (m_Inventory.UnequipAllOnDeath) {
                m_InventoryAmount.Clear();
            }
        }

        /// <summary>
        /// The inventory has reloaded after respawning. 
        /// </summary>
        private void OnInventoryRespawned()
        {
            // If the items weren't removed upon death then the ability should equip what they previously had equipped.
            if (!m_Inventory.UnequipAllOnDeath && m_PrevActiveItemSetIndex != -1) {
                StartEquipUnequip(m_PrevActiveItemSetIndex, true, true);

                // No equip animations need to play - it should be like the character started fresh.
                UpdateItemAbilityAnimatorParameters();
                EventHandler.ExecuteEvent(m_GameObject, "OnCharacterSnapAnimator", false);
            }
        }

        /// <summary>
        /// Called when the character is destroyed.
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();

            EventHandler.UnregisterEvent(m_GameObject, "OnItemPickupStartPickup", WillStartPickup);
            EventHandler.UnregisterEvent(m_GameObject, "OnItemPickupStopPickup", StopPickup);
            EventHandler.UnregisterEvent<CharacterItem, int, bool, bool>(m_GameObject, "OnInventoryPickupItem", OnPickupItem);
            EventHandler.UnregisterEvent<IItemIdentifier, int, bool, bool>(m_GameObject, "OnInventoryPickupItemIdentifier", OnPickupItemIdentifier);
            EventHandler.UnregisterEvent<int, int>(m_GameObject, "OnItemSetIndexChange", OnItemSetIndexChange);
            EventHandler.UnregisterEvent<int, int>(m_GameObject, "OnEquipUnequipVerifyUnequip", OnVerifyUnequipItem);
            EventHandler.UnregisterEvent<CharacterItem, int>(m_GameObject, "OnInventoryRemoveItem", OnRemoveItem);
            EventHandler.UnregisterEvent<bool, bool>(m_GameObject, "OnAimAbilityStart", OnAim);
            EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(m_GameObject, "OnDeath", OnDeath);
            EventHandler.UnregisterEvent(m_GameObject, "OnInventoryRespawned", OnInventoryRespawned);
        }
    }
}