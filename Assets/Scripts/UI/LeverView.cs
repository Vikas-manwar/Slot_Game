using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlotGame.UI
{
    public sealed class LeverView : MonoBehaviour, IPointerClickHandler
    {
        private static readonly int PullHash = Animator.StringToHash("Pull");

        [SerializeField] private Animator animator;

        public event Action Clicked;

        public bool Interactable { get; set; } = true;

        public void PlayPull()
        {
            if (animator != null) animator.SetTrigger(PullHash);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Interactable) Clicked?.Invoke();
        }
    }
}
