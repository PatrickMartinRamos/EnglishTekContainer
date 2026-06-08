using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Tek.Core
{
    [DisallowMultipleComponent]
    public class DebugMenuDropdown : MonoBehaviour
    {
        [SerializeField] private bool enableDebugMenu = true;
        [Header("Dropdown References")]
        [SerializeField] private TMP_Dropdown tekDropdown;
        [SerializeField] private TMP_Dropdown gradeDropdown;

        [Header("Dropdown Options")]
        [Tooltip("Customize the available Tek options (e.g., englishtek, filipinotek, etc.)")]
        [SerializeField] private List<string> tekOptions = new List<string> { "englishtek", "sciencetek", "filipinotek", "mathtek", "aptek" };
        [Tooltip("Customize the available grades.")]
        [SerializeField] private List<string> gradeOptions = new List<string> { "Grade 1", "Grade 2", "Grade 3" };

        [Header("Optional: Refresh Targets")]
        [Tooltip("Assign to auto-refresh interactives when ApplyAndRefresh is called.")]
        [SerializeField] private InteractiveController interactiveController;

        public string SelectedTek
        {
            get
            {
                int index = tekDropdown != null ? tekDropdown.value : 0;
                index = Mathf.Clamp(index, 0, tekOptions.Count - 1);
                return tekOptions.Count > 0 ? tekOptions[index] : string.Empty;
            }
        }

        public string SelectedGrade
        {
            get
            {
                int index = gradeDropdown != null ? gradeDropdown.value : 0;
                index = Mathf.Clamp(index, 0, gradeOptions.Count - 1);
                return gradeOptions.Count > 0 ? gradeOptions[index] : string.Empty;
            }
        }

        private void Awake()
        {
            if (!enableDebugMenu)
            {
                gameObject.SetActive(false);
                return;
            }

            if (tekDropdown == null || gradeDropdown == null)
            {
                Debug.LogWarning("[DebugMenuDropdown] Assign both Tek and Grade TMP_Dropdown references.");
                return;
            }

            SetupTekDropdown();
            SetupGradeDropdown();
        }

        public void ApplyAndRefresh()
        {
            string selectedTek = SelectedTek;
            string selectedGrade = SelectedGrade;
            Debug.Log($"[DebugMenuDropdown] Selection -> Tek: {selectedTek}, Grade: {selectedGrade}");
            if (interactiveController != null)
            {
                interactiveController.SetGrade(selectedGrade);
                interactiveController.RefreshCatalog();
                Debug.Log("[DebugMenuDropdown] Set grade and called RefreshCatalog() on InteractiveController.");
            }
            else
            {
                Debug.LogWarning("[DebugMenuDropdown] No InteractiveController assigned. Assign one in the Inspector to enable refresh.");
            }
        }

        private void SetupTekDropdown()
        {
            tekDropdown.ClearOptions();
            tekDropdown.AddOptions(tekOptions);
            tekDropdown.SetValueWithoutNotify(0);
            tekDropdown.RefreshShownValue();
        }

        private void SetupGradeDropdown()
        {
            gradeDropdown.ClearOptions();
            gradeDropdown.AddOptions(gradeOptions);
            gradeDropdown.SetValueWithoutNotify(0);
            gradeDropdown.RefreshShownValue();
        }
    }
}
