using GameLogic.Interface.Behaviour;
using GameLogic.Managers;
using GameLogic.ObjectPools;
using GameLogic.ScriptableObjects.Items;
using GameLogic.Utility.Extras;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameLogic.Interface.UI
{
    public class Inventory : InGameMenuBehaviour
    {
        [SerializeField] Transform _buttonContainer;
        private SortParameter _currentSortParameter = SortParameter.Category;

        private void Start()
        {
            InterfaceMenu.anchoredPosition = new Vector2(-300, 0);
            InterfaceMenu.localRotation = Quaternion.Euler(new Vector3(0, -120, 0));
        }

        public override void SetMenuOpenState(bool setActive, bool setMenuOpenState = true)
        {
            base.SetMenuOpenState(setActive, setMenuOpenState);

            if (!setActive && FullItemInfo.Current.ItemButtonBehaviour == ItemButtonBehaviour.Inventory)
                FullItemInfo.Current.HideFullItemInfoUI();

            if (setActive)
                InterfaceController.Current.AnimateElementEnabled(InterfaceMenu, new Vector2(20, 0));
            else
                InterfaceController.Current.AnimateElementDisabled(InterfaceMenu, new Vector2(-300, 0), new Vector3(0, -120, 0));
        }

        #region Button functions
        public void SortByName() => SortItemButtons(SortParameter.Name);
        public void SortByType() => SortItemButtons(SortParameter.Category);
        public void SortByTier() => SortItemButtons(SortParameter.Tier);
        public void SortByValue() => SortItemButtons(SortParameter.Value);
        #endregion

        #region Sorting and creating buttons
        private void SortItemButtons(SortParameter sortParameter)
        {
            _currentSortParameter = sortParameter;
            RemoveAllItemButtons();
            SetItemButtonsProperties(ItemSorter.GetNewSortedButtons(ItemManager.Instance.CurrentInventory.Keys.ToList(), _buttonContainer, sortParameter));
        }

        private void SetItemButtonsProperties(Dictionary<GameObject, ItemInfoSO> sortedButtons)
        {
            foreach (GameObject button in sortedButtons.Keys)
            {
                button.name = sortedButtons[button].Id;
                ItemButtonLogic newButtonTemplate = button.GetComponent<ItemButtonLogic>();
                newButtonTemplate.SetButtonVisuals(sortedButtons[button], ItemButtonBehaviour.Inventory, ItemManager.Instance.CurrentInventory[sortedButtons[button]].ToString() + "x");
            }
        }

        private void RemoveAllItemButtons()
        {
            foreach (Transform button in _buttonContainer.transform)
                ItemBtnObjectPool.Current.TryReleaseObject(button.gameObject);
        }
        #endregion

        #region Event listeners
        private void UpdateInventoryButton(ItemInfoSO itemInfoSO)
        {
            Transform itemButtonTransform = _buttonContainer.transform.Find(itemInfoSO.Id);
            if (!itemButtonTransform) return;
            if (itemButtonTransform.gameObject.activeSelf)
            {
                ItemButtonLogic itemButton = itemButtonTransform.GetComponent<ItemButtonLogic>();

                if (ItemManager.Instance.DoesInventoryContainItem(itemInfoSO))
                {
                    itemButton.Amount.text = ItemManager.Instance.CurrentInventory[itemInfoSO].ToString() + "x";
                    return;
                }

                ItemBtnObjectPool.Current.TryReleaseObject(itemButtonTransform.gameObject);
            }
        }

        private void ResortInventory(ItemInfoSO _) => SortItemButtons(_currentSortParameter);

        public override void OnEnable()
        {
            base.OnEnable();
            ItemManager.OnInventoryCleared += RemoveAllItemButtons;
            ItemManager.OnItemAddedToInventory += ResortInventory;
            ItemManager.OnItemRemovedFromInventory += UpdateInventoryButton;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            ItemManager.OnInventoryCleared -= RemoveAllItemButtons;
            ItemManager.OnItemAddedToInventory -= ResortInventory;
            ItemManager.OnItemRemovedFromInventory -= UpdateInventoryButton;
        }
        #endregion
    }
}