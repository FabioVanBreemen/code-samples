using GameLogic.Environment.GridBuilding;
using GameLogic.Managers.SaveSystem;
using GameLogic.ScriptableObjects.Items;
using GameLogic.Utility;
using GameLogic.Utility.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameLogic.Managers
{
    public class ItemManager : SingletonCreator<ItemManager>
    {
        public static event Action<ItemInfoSO> OnPrestigeItemObtained;
        public static event Action<ItemInfoSO> OnLootboxItemObtained;
        public static event Action<ItemInfoSO, int> OnItemsPurchased;
        public static event Action<ItemInfoSO, int> OnFailedToPurchaseMissingCredits;
        public static event Action<ItemInfoSO> OnFailedToPurchaseNotForSale;
        public static event Action<ItemInfoSO> OnItemAddedToInventory;
        public static event Action<ItemInfoSO> OnItemRemovedFromInventory;
        public static event Action OnInventoryCleared;

        [SerializeField] private AllItemsSO _allItemsSO;

        private List<ItemInfoSO> _allItems = new();
        public List<ItemInfoSO> AllItems { get => _allItems; }

        private List<ItemInfoSO> _shopItems = new();
        public List<ItemInfoSO> ShopItems { get => _shopItems; }

        private List<ItemInfoSO> _prestigeItems = new();
        public List<ItemInfoSO> PrestigeItems { get => _prestigeItems; }

        private List<ItemInfoSO> _lootboxItems = new();
        public List<ItemInfoSO> LootboxItems { get => _lootboxItems; }

        private Dictionary<string, int> _ownedItems = new();
        public Dictionary<string, int> OwnedItems { get => _ownedItems; }

        private Dictionary<ItemInfoSO, int> _currentInventory = new();
        public Dictionary<ItemInfoSO, int> CurrentInventory { get => _currentInventory; }

        #region Pre-game setup
        protected override void Awake()
        {
            base.Awake();
            GetAllItemsAndAssignLists();
        }

        /// <summary>
        /// Gather every single item and add them to _allItems.
        /// </summary>
        private void GetAllItemsAndAssignLists()
        {
            foreach (ItemInfoSO itemInfoSO in _allItemsSO.AllItems)
                _allItems.Add(itemInfoSO);

            foreach (ItemInfoSO itemInfoSO in _allItems)
                if (itemInfoSO.CanBuy)
                    _shopItems.Add(itemInfoSO);

            foreach (ItemInfoSO itemInfoSO in _allItems)
                if (itemInfoSO.PrestigeItem)
                    _prestigeItems.Add(itemInfoSO);

            foreach (ItemInfoSO itemInfoSO in _allItems)
                if (itemInfoSO.LootboxItem)
                    _lootboxItems.Add(itemInfoSO);

            _allItems = _allItems.OrderBy(o => o.Tier).ToList();
            _shopItems = _shopItems.OrderBy(o => o.BuyPrice).ToList();
        }

        /// <summary>
        /// For testing purposes.
        /// </summary>
        private void AddAllItemsForTesting()
        {
            foreach (ItemInfoSO itemInfoSO in _allItems)
                AddToOwnedItemsDictionary(itemInfoSO.Id, 99);
        }

        /// <summary>
        /// Add default items to _ownedItems. Good for resetting after a prestige.
        /// </summary>
        private void AddDefaultItems()
        {
            foreach (string itemId in DefaultSettings.DefaultItemsIds)
            {
                if (!DoesItemIDExist(itemId)) continue;

                AddToOwnedItemsDictionary(itemId);
            }
        }

        /// <summary>
        /// Loop through every game item and check if it is owned. Adds all owned items to InsideInventory.
        /// </summary>
        private void SetCurrentInventoryToOwnedItems()
        {
            _currentInventory.Clear();

            foreach (string itemId in _ownedItems.Keys)
            {
                ItemInfoSO itemInfoSO = GetItemInfoSOFromString(itemId);

                AddToCurrentInventoryDictionary(itemInfoSO, _ownedItems[itemId]);
            }
        }
        #endregion

        #region Adding or remove from inventory / items owned
        /// <summary>
        /// Used for adding items to the inventory (bought from shop / new prestige item).
        /// </summary>
        private void AddNewItemWithQuantity(string itemId, int quantity = 1)
        {
            AddToOwnedItemsDictionary(itemId, quantity);

            ItemInfoSO itemInfoSO = GetItemInfoSOFromString(itemId);
            if (!itemInfoSO) return;

            AddToCurrentInventoryDictionary(itemInfoSO, quantity);
        }

        /// <summary>
        /// Add item to owned items list.
        /// </summary>
        private void AddToOwnedItemsDictionary(string itemId, int quantity = 1)
        {
            if (!_ownedItems.ContainsKey(itemId))
            {
                _ownedItems.Add(itemId, quantity);
                return;
            }

            _ownedItems[itemId] += quantity;
        }

        /// <summary>
        /// Add item to currently in inventory list.
        /// </summary>
        private void AddToCurrentInventoryDictionary(ItemInfoSO itemInfoSO, int quantity = 1)
        {
            if (!_currentInventory.ContainsKey(itemInfoSO))
            {
                _currentInventory.Add(itemInfoSO, quantity);
                OnItemAddedToInventory?.Invoke(itemInfoSO);
                return;
            }

            _currentInventory[itemInfoSO] += quantity;
            OnItemAddedToInventory?.Invoke(itemInfoSO);
        }

        /// <summary>
        /// Remove item from owned items list.
        /// </summary>
        private void RemoveFromOwnedItemsDictionary(string itemId)
        {
            _ownedItems[itemId] -= 1;
            if (_ownedItems[itemId] == 0)
                _ownedItems.Remove(itemId);
        }

        /// <summary>
        /// Remove item from currently in inventory list.
        /// </summary>
        private void RemoveFromCurrentInventoryDictionary(ItemInfoSO itemInfoSO)
        {
            _currentInventory[itemInfoSO] -= 1;
            if (_currentInventory[itemInfoSO] == 0)
                _currentInventory.Remove(itemInfoSO);

            OnItemRemovedFromInventory?.Invoke(itemInfoSO);
        }
        #endregion

        #region Lootboxes & Prestige
        /// <summary>
        /// Awards the player with a random presige item.
        /// </summary>
        private void AwardRandomPrestigeItem()
        {
            Dictionary<ItemInfoSO, float> lootTableItems = new();
            foreach (ItemInfoSO item in _prestigeItems)
            {
                if (GameManager.CurrentPrestigeLevel < item.MinimumPrestige) continue;
                lootTableItems.Add(item, item.PrestigeRarity);
            }

            ItemInfoSO itemInfoSO = RandomLootTable.GetRandomItem(lootTableItems);
            AddNewItemWithQuantity(itemInfoSO.Id, 1);
            OnPrestigeItemObtained?.Invoke(itemInfoSO);
        }

        /// <summary>
        /// Awards the player with a random lootbox item.
        /// </summary>
        private void AwardRandomLootboxItem()
        {
            Dictionary<ItemInfoSO, float> lootTableItems = new();
            foreach (ItemInfoSO item in _lootboxItems)
            {
                if (GameManager.AccountBalance < item.MinAccountBalance.ConvertToBigInt()) continue;
                lootTableItems.Add(item, item.LootboxRarity);
            }

            ItemInfoSO itemInfoSO = RandomLootTable.GetRandomItem(lootTableItems);
            AddNewItemWithQuantity(itemInfoSO.Id, 1);
            OnLootboxItemObtained?.Invoke(itemInfoSO);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Try to get the ItemInfoSO from _allItems using a string ID.
        /// </summary>
        public ItemInfoSO GetItemInfoSOFromString(string itemId)
        {
            foreach (ItemInfoSO itemInfoSO in _allItems)
            {
                if (itemInfoSO.Id != itemId) continue;
                return itemInfoSO;
            }

            Debug.LogWarning("Item ID does not exist! " + itemId);
            return null;
        }

        /// <summary>
        /// Does the string ID actually exist in _allItems?
        /// </summary>
        public bool DoesItemIDExist(string itemId)
        {
            if (GetItemInfoSOFromString(itemId)) return true;

            Debug.LogWarning("Item ID does not exist! " + itemId);
            return false;
        }

        /// <summary>
        /// Does given item exist in the player's inventory?
        /// </summary>
        public bool DoesInventoryContainItem(ItemInfoSO itemInfoSO)
        {
            return _currentInventory.ContainsKey(itemInfoSO);
        }

        /// <summary>
        /// Enables build mode with selected ItemInfoSO's prefab.
        /// </summary>
        public void SelectItemInBuildingSystem(ItemInfoSO itemInfoSO)
        {
            BuildingSystem.Current.SetItemToPlaceAndEnableBuildingMode(itemInfoSO.Prefab);
        }

        /// <summary>
        /// Purchase an item. Optional: quantity.
        /// </summary>
        public bool PurchaseItemIfPossible(ItemInfoSO itemInfoSO, int quantity = 1)
        {
            if (!itemInfoSO.CanBuy)
            {
                OnFailedToPurchaseNotForSale?.Invoke(itemInfoSO);
                return false;
            }

            if (GameManager.AccountBalance < itemInfoSO.BuyPrice.ConvertToBigInt() || GameManager.GalaxyPoints < itemInfoSO.MinGalaxyPoints.ConvertToBigInt())
            {
                OnFailedToPurchaseMissingCredits?.Invoke(itemInfoSO, quantity);
                return false;
            }

            GameManager.ModifyAccountBalance(-itemInfoSO.BuyPrice.ConvertToBigInt());
            AddNewItemWithQuantity(itemInfoSO.Id, quantity);
            OnItemsPurchased?.Invoke(itemInfoSO, quantity);
            return true;
        }
        #endregion

        #region Event listeners
        /// <summary>
        /// Called when the item is withdrawn and back in the inventory.
        /// </summary>
        private void ItemWithdrawn(ItemInfoSO itemInfoSO)
        {
            AddToCurrentInventoryDictionary(itemInfoSO);
        }

        /// <summary>
        /// Called when the item is placed and no longer in the inventory.
        /// </summary>
        private void ItemPlaced(ItemInfoSO itemInfoSO, bool manuallyPlaced)
        {
            RemoveFromCurrentInventoryDictionary(itemInfoSO);
        }

        /// <summary>
        /// Sell an item. Do not use this to sell an item from inside the inventory!
        /// </summary>
        private void SellItem(ItemInfoSO itemInfoSO)
        {
            GameManager.ModifyAccountBalance(itemInfoSO.SellPrice.ConvertToBigInt());
            RemoveFromOwnedItemsDictionary(itemInfoSO.Id);
        }

        /// <summary>
        /// Converts OwnedItems struct List to _ownedItems List.
        /// </summary>
        private void SetOwnedItemsListFromStruct(List<OwnedItems> ownedItemsStruct)
        {
            if (ownedItemsStruct == null) return;

            foreach (OwnedItems ownedItemStruct in ownedItemsStruct)
            {
                if (!DoesItemIDExist(ownedItemStruct.ItemId)) continue;
                _ownedItems.Add(ownedItemStruct.ItemId, ownedItemStruct.Quantity);
            }

            SetCurrentInventoryToOwnedItems();
        }

        /// <summary>
        /// Converts _ownedItems List to OwnedItems struct List.
        /// </summary>
        private List<OwnedItems> ConvertOwnedItemsListToStruct()
        {
            List<OwnedItems> ownedItemsList = new();

            if (_ownedItems.Count == 0) return ownedItemsList;

            foreach (string itemId in _ownedItems.Keys)
            {
                OwnedItems ownedItems = new()
                {
                    ItemId = itemId,
                    Quantity = _ownedItems[itemId]
                };
                ownedItemsList.Add(ownedItems);
            }

            return ownedItemsList;
        }

        private void ExtractSaveData(SaveSlotData saveData)
        {
        #if UNITY_EDITOR
            AddAllItemsForTesting();
            SetCurrentInventoryToOwnedItems();
        #else
            SetOwnedItemsListFromStruct(saveData.OwnedItems);
        #endif
        }

        private void SetSaveData()
        {
            SaveDataManager.CurrentSaveData.OwnedItems = ConvertOwnedItemsListToStruct();
        }

        private void ResetOnNewSaveStarted()
        {
            ClearInventoryAndOwnedItems();

        #if UNITY_EDITOR
            AddAllItemsForTesting();
        #else
            AddDefaultItems();
        #endif
            SetCurrentInventoryToOwnedItems();
        }

        private void ClearInventoryAndOwnedItems()
        {
            _currentInventory.Clear();
            _ownedItems.Clear();
            OnInventoryCleared?.Invoke();
        }

        private void PrestigePerformed(int prestigeLevelsGained)
        {
            Dictionary<string, int> prestigeProofOwnedItems = new();
            foreach (string ownedItemId in _ownedItems.Keys)
            {
                ItemInfoSO itemInfoSO = GetItemInfoSOFromString(ownedItemId);

                if (!itemInfoSO.PrestigeProof) continue;

                prestigeProofOwnedItems.Add(ownedItemId, _ownedItems[ownedItemId]);
            }

            ClearInventoryAndOwnedItems();
        #if UNITY_EDITOR
            AddAllItemsForTesting();
        #else
            AddDefaultItems();
        #endif

            foreach (string itemId in prestigeProofOwnedItems.Keys)
                AddToOwnedItemsDictionary(itemId, prestigeProofOwnedItems[itemId]);
            
            SetCurrentInventoryToOwnedItems();

            for (int i = 1; i <= prestigeLevelsGained; i++)
                if (RandomExtras.CalculateProbability(100 / i))
                    AwardRandomPrestigeItem();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BuildingSystem.OnItemWithdrawn += ItemWithdrawn;
            BuildingSystem.OnItemPlaced += ItemPlaced;
            BuildingSystem.OnSellItem += SellItem;
            SaveDataManager.OnSaveDataFetched += ExtractSaveData;
            SaveDataManager.OnDataSaveRequested += SetSaveData;
            SaveDataManager.OnNewSaveGameStarted += ResetOnNewSaveStarted;
            GameManager.OnExitToMenu += ClearInventoryAndOwnedItems;
            GameManager.OnPrestigePerformed += PrestigePerformed;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            BuildingSystem.OnItemWithdrawn -= ItemWithdrawn;
            BuildingSystem.OnItemPlaced -= ItemPlaced;
            BuildingSystem.OnSellItem -= SellItem;
            SaveDataManager.OnSaveDataFetched -= ExtractSaveData;
            SaveDataManager.OnDataSaveRequested -= SetSaveData;
            SaveDataManager.OnNewSaveGameStarted -= ResetOnNewSaveStarted;
            GameManager.OnExitToMenu -= ClearInventoryAndOwnedItems;
            GameManager.OnPrestigePerformed -= PrestigePerformed;
        }
        #endregion
    }
}