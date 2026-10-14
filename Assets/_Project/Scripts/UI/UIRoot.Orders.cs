using Geprek.Core;
using Geprek.Customers;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        RectTransform _orderCard;
        Image _orderFood;
        Text _orderName, _orderStatus, _orderFallback;
        Customer _orderCustomer;
        float _orderReveal;

        void BuildOrderCard()
        {
            var card = UIFactory.Panel("SelectedOrder", _hud, UIStyle.Panel);
            _orderCard = card.rectTransform;
            UIFactory.Anchor(_orderCard, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(0f, 20f), new Vector2(360f, 116f));
            _orderFood = UIFactory.Icon("Food", _orderCard, null, new Vector2(100f, 100f));
            UIFactory.Anchor(_orderFood.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                new Vector2(10f, 0f), new Vector2(100f, 100f));
            _orderFood.preserveAspect = true;
            _orderFallback = UIFactory.Label("MissingArtwork", _orderFood.transform, "?", UIStyle.FontTitle,
                UIStyle.InkSoft, TextAnchor.MiddleCenter);
            UIFactory.Stretch(_orderFallback.rectTransform);
            _orderName = UIFactory.Label("DishName", _orderCard, "", UIStyle.FontLead, UIStyle.Ink,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor(_orderName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(122f, -12f), new Vector2(224f, 56f));
            _orderName.resizeTextForBestFit = true;
            _orderName.resizeTextMinSize = UIStyle.FontCaption;
            _orderName.resizeTextMaxSize = UIStyle.FontLead;
            _orderStatus = UIFactory.Label("Status", _orderCard, "", UIStyle.FontSmall, UIStyle.InkSoft, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_orderStatus.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(122f, 10f), new Vector2(224f, 34f));
            foreach (var graphic in _orderCard.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            _orderCard.gameObject.SetActive(false);
        }

        void TickOrderCard(float dt)
        {
            var customer = player != null && player.Interactor != null
                ? player.Interactor.Current as Customer : null;
            bool show = customer != null && customer.Order != null && customer.State == CustomerState.Waiting
                && Game != null && Game.State is GameState.DayOperating or GameState.DayClosing
                && !IsAnyModalOpen();
            if (_orderCard == null) return;
            _orderCard.gameObject.SetActive(show);
            if (_carryBox != null) _carryBox.gameObject.SetActive(!show && Game != null &&
                Game.State is GameState.DayOperating or GameState.DayClosing);
            if (!show) { _orderCustomer = null; return; }
            if (_orderCustomer != customer)
            {
                _orderCustomer = customer;
                _orderReveal = 0f;
                _orderFood.sprite = customer.Order.icon;
                _orderFood.enabled = true;
                // Keep the graphic enabled for its child fallback label, but hide its white quad.
                _orderFood.color = customer.Order.icon != null ? Color.white : Color.clear;
                _orderFallback.gameObject.SetActive(customer.Order.icon == null);
                _orderName.text = customer.Order.displayName;
            }
            _orderReveal = Mathf.Min(1f, _orderReveal + dt / .18f);
            _orderCard.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, Mathf.SmoothStep(0f, 1f, _orderReveal));
            bool ready = player.Carry != null && player.Carry.HasItem && player.Carry.Held.recipe == customer.Order;
            _orderStatus.text = ready ? "SIAP DIANTAR · E / Spasi" :
                customer.PatienceLeft < .25f ? "SEGERA · Hampir habis" : "PESANAN · Menunggu";
        }
    }
}
