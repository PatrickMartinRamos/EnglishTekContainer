using UnityEngine;

namespace EnglishTek.Core
{
    /// <summary>
    /// Extends InteractiveCatalogMenu with animated UIGroup transitions.
    /// Use this component instead of InteractiveCatalogMenu when you want
    /// category → unit → entry navigation animations.
    /// </summary>
    public class CatalogMenuNavigator : InteractiveCatalogMenu
    {
        [Header("Navigation Groups")]
        [Tooltip("Container holding category buttons. Shown first.")]
        [SerializeField] private UIGroup categoryGroup = null;

        [Tooltip("Container holding unit buttons. Shown after a category is picked.")]
        [SerializeField] private UIGroup unitGroup = null;

        [Tooltip("Container holding entry / interactive buttons. Shown after a unit is picked.")]
        [SerializeField] private UIGroup entryGroup = null;

        // ------------------------------------------------------------------ overrides

        protected override void OnCategoryApplied()
        {
            if (categoryGroup != null)
            {
                categoryGroup.Hide(() =>
                {
                    if (unitGroup != null) { unitGroup.Show(); }
                });
            }
            else if (unitGroup != null)
            {
                unitGroup.Show();
            }
        }

        protected override void OnUnitSelected()
        {
            if (unitGroup != null)
            {
                SetUnitButtonsInteractable(false);
                unitGroup.Hide(() =>
                {
                    if (entryGroup != null) { entryGroup.Show(); }
                });
            }
            else if (entryGroup != null)
            {
                entryGroup.Show();
            }
        }

        // ------------------------------------------------------------------ back navigation

        /// <summary>
        /// Go back one step: entry → unit, or unit → category.
        /// Wire to a back button's onClick in the Inspector.
        /// </summary>
        public override void GoBack()
        {
            if (entryGroup != null && entryGroup.IsVisible)
            {
                HideHomeBackground();
                entryGroup.Hide(() =>
                {
                    SetUnitButtonsInteractable(true);
                    if (unitGroup != null) { unitGroup.Show(); }
                });
                return;
            }

            if (unitGroup != null && unitGroup.IsVisible)
            {
                unitGroup.Hide(() =>
                {
                    if (categoryGroup != null) { categoryGroup.Show(); }
                });
            }
        }

        /// <summary>
        /// Jump straight back to category view, hiding both entry and unit groups.
        /// Wire this to your back button's onClick in the Inspector.
        /// </summary>
        public void GoToCategories()
        {
            HideHomeBackground();
            SetUnitButtonsInteractable(true);

            if (entryGroup != null)
            {
                entryGroup.HideImmediate();
            }

            if (unitGroup != null)
            {
                unitGroup.HideWith(UIGroupAnimation.Fade, () =>
                {
                    if (categoryGroup != null) { categoryGroup.Show(); }
                });
            }
            else if (categoryGroup != null)
            {
                categoryGroup.Show();
            }
        }
    }
}
