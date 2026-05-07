using UnityEngine;

namespace Tek.Core
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

        [Header("Back Button")]
        [Tooltip("Main-menu back button group. Shown when lesson navigation (unit or entry) is visible.")]
        [SerializeField] private UIGroup lessonBackButtonGroup = null;

        [Tooltip("Optional fallback if your back button is not a UIGroup.")]
        [SerializeField] private GameObject lessonBackButtonObject = null;

        // ------------------------------------------------------------------ overrides

        protected override void OnCategoryApplied()
        {
            if (categoryGroup != null)
            {
                categoryGroup.Hide(() =>
                {
                    if (unitGroup != null)
                    {
                        unitGroup.Show(() => SyncLessonBackButtonVisibility());
                    }
                    else
                    {
                        SyncLessonBackButtonVisibility();
                    }
                });
            }
            else if (unitGroup != null)
            {
                unitGroup.Show(() => SyncLessonBackButtonVisibility());
            }
            else
            {
                SyncLessonBackButtonVisibility();
            }
        }

        protected override void OnUnitSelected()
        {
            if (unitGroup != null)
            {
                SetUnitButtonsInteractable(false);
                unitGroup.Hide(() =>
                {
                    if (entryGroup != null)
                    {
                        entryGroup.Show(() => SyncLessonBackButtonVisibility());
                    }
                    else
                    {
                        SyncLessonBackButtonVisibility();
                    }
                });
            }
            else if (entryGroup != null)
            {
                entryGroup.Show(() => SyncLessonBackButtonVisibility());
            }
            else
            {
                SyncLessonBackButtonVisibility();
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
                    if (unitGroup != null)
                    {
                        unitGroup.Show(() => SyncLessonBackButtonVisibility());
                    }
                    else
                    {
                        SyncLessonBackButtonVisibility();
                    }
                });
                return;
            }

            if (unitGroup != null && unitGroup.IsVisible)
            {
                unitGroup.Hide(() =>
                {
                    if (categoryGroup != null)
                    {
                        categoryGroup.Show(() => SyncLessonBackButtonVisibility());
                    }
                    else
                    {
                        SyncLessonBackButtonVisibility();
                    }
                });
                return;
            }

            SyncLessonBackButtonVisibility();
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
                    if (categoryGroup != null)
                    {
                        categoryGroup.Show(() => SyncLessonBackButtonVisibility());
                    }
                    else
                    {
                        SyncLessonBackButtonVisibility();
                    }
                });
            }
            else if (categoryGroup != null)
            {
                categoryGroup.Show(() => SyncLessonBackButtonVisibility());
            }
            else
            {
                SyncLessonBackButtonVisibility();
            }
        }

        private void SyncLessonBackButtonVisibility()
        {
            bool showBack = (unitGroup != null && unitGroup.IsVisible)
                || (entryGroup != null && entryGroup.IsVisible);

            if (lessonBackButtonGroup != null)
            {
                if (showBack)
                {
                    lessonBackButtonGroup.ShowImmediate();
                }
                else
                {
                    lessonBackButtonGroup.HideImmediate();
                }
            }

            if (lessonBackButtonObject != null)
            {
                lessonBackButtonObject.SetActive(showBack);
            }
        }
    }
}
