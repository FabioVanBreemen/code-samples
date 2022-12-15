using GameLogic.Interface.UI;
using GameLogic.Managers;
using GameLogic.ScriptableObjects.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic.Interface.Behaviour
{
    public enum ItemButtonBehaviour { None, Inventory, Shop }

    public class ItemButtonLogic : UIButtonLogic, IPointerClickHandler
    {
        [SerializeField] Image _buttonImage;
        public TMP_Text Amount;
        public GameObject ItemLockedPanel;
        public TMP_Text UnlockRequirement;
        public ItemButtonBehaviour ItemButtonBehaviour;
        [HideInInspector] public ItemInfoSO ItemInfoSO;

        public void SetButtonVisuals(ItemInfoSO itemInfoSO, ItemButtonBehaviour itemButtonBehaviour = ItemButtonBehaviour.None, string amountString = null)
        {
            ItemInfoSO = itemInfoSO;
            ItemButtonBehaviour = itemButtonBehaviour;
            _buttonImage.sprite = itemInfoSO.Image;
            Amount.text = amountString ?? string.Empty;
            GetComponent<Image>().color = itemInfoSO.TierColor;
            ItemLockedPanel.SetActive(false);
        }

        /// <summary>
        /// On button click enable building system.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {            
            if (ItemButtonBehaviour == ItemButtonBehaviour.Inventory)
                ItemManager.Instance.SelectItemInBuildingSystem(ItemInfoSO);

            if (ItemButtonBehaviour == ItemButtonBehaviour.Shop)
                ItemManager.Instance.PurchaseItemIfPossible(ItemInfoSO);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            FullItemInfo.Current.ShowFullItemInfoUI(ItemInfoSO, ItemButtonBehaviour);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            FullItemInfo.Current.HideFullItemInfoUI();
        }
    }
}