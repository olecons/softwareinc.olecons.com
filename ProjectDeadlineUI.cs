using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

namespace ProjectDeadline {
    class ProjectDeadlineUI : ModBehaviour {
        static Button modButton;
        static bool isButtonSpawned = false;

        static GUIWindow Window;
        static string titleWindow = "Project Deadline - v" + ProjectDeadlineMod.Version;
        static bool isUpdateWindowPosition = false;
        static Vector2 windowLastSize = new Vector2(600, 400);
        static Vector2 windowLastPosition = new Vector2(75, 445);
        static List<Toggle> listOfToggles = new List<Toggle>();
        static List<Text> listOfTexts = new List<Text>();
        static List<InputField> listOfInputFields = new List<InputField>();
        public override void OnDeactivate() {
            if (modButton != null)
                Destroy(modButton.gameObject);
            if (Window != null)
                Destroy(Window.gameObject);
            isButtonSpawned = false;
        }
        public override void OnActivate() {
            SpawnButton();
        }
        public static void SpawnButton() {
            if (SceneManager.GetActiveScene().name.Equals("MainScene")) {
                modButton = WindowManager.SpawnButton();
                modButton.GetComponentInChildren<Text>().text = "Deadline";
                modButton.onClick.AddListener(ShowWindow);
                modButton.name = "ProjectDeadlineButton";

                WindowManager.AddElementToElement(modButton.gameObject,
                                                  WindowManager.FindElementPath("MainPanel/Holder/FanPanel").gameObject,
                                                  new Rect(370, 10, 100, 30),
                                                  new Rect(0, 0, 0, 0));
                isButtonSpawned = true;
            }
        }

        public void LateUpdate() {
            if (!isButtonSpawned) {
                SpawnButton();
            }

            if (isUpdateWindowPosition) {
                Window.rectTransform.sizeDelta = windowLastSize;
                Window.rectTransform.localPosition = windowLastPosition;
                isUpdateWindowPosition = false;
            }

            if (Window != null && Window.isActiveAndEnabled) {
                RefreshWindow();
            }
        }
        public static void ShowWindow() {
            if (Window != null && Window.isActiveAndEnabled) {
                windowLastSize = Window.rectTransform.sizeDelta;
                windowLastPosition = Window.rectTransform.localPosition;
                ClearAllLists();
                Window.Close();
            } else {
                ClearAllLists();
                CreateWindow();
                isUpdateWindowPosition = true;
            }
        }

        private static void CreateWindow() {
            Window = WindowManager.SpawnWindow();
            Window.InitialTitle = Window.TitleText.text = Window.NonLocTitle = titleWindow;
            Window.MinSize.x = 600;
            Window.MinSize.y = 400;
            Window.name = "ProjectDeadLineWindow";
            Window.MainPanel.name = "ProjectDeadLineMainPanel";

            Text projectNameLabel = WindowManager.SpawnLabel();
            projectNameLabel.text = "Project Name";
            WindowManager.AddElementToWindow(projectNameLabel.gameObject, Window, new Rect(30, 30, 200, 25), new Rect(0, 0, 0, 0));

            Text projectIntervalLabel = WindowManager.SpawnLabel();
            projectIntervalLabel.text = "Next Release";
            WindowManager.AddElementToWindow(projectIntervalLabel.gameObject, Window, new Rect(230, 30, 100, 25), new Rect(0, 0, 0, 0));

            Text isActiveLabel = WindowManager.SpawnLabel();
            isActiveLabel.text = "Deadline";
            WindowManager.AddElementToWindow(isActiveLabel.gameObject, Window, new Rect(330, 30, 70, 25), new Rect(0, 0, 0, 0));
            for (int i = 0; i < ProjectDeadlineBehaviour.Instance.ReleaseInfos.Count; i++) {
                ProjectDeadlineBehaviour.ReleaseInfo releaseInfo = ProjectDeadlineBehaviour.Instance.ReleaseInfos.GetAt(i).Value;
                string projectName = ProjectDeadlineBehaviour.Instance.ReleaseInfos.GetAt(i).Key.Name;
                AddNewLineToWindow(projectName, releaseInfo, i);
            }
        }

        void RefreshWindow() {
            int lastIndex = 0;
            if (ProjectDeadlineBehaviour.Instance.ReleaseInfos.Count == 0) {
                ClearAllLists();
            }
            for (int i = 0; i < ProjectDeadlineBehaviour.Instance.ReleaseInfos.Count; i++) {
                ProjectDeadlineBehaviour.ReleaseInfo releaseInfo = ProjectDeadlineBehaviour.Instance.ReleaseInfos.GetAt(i).Value;
                string projectName = ProjectDeadlineBehaviour.Instance.ReleaseInfos.GetAt(i).Key.Name;

                if (i < listOfInputFields.Count) {
                    listOfTexts[i].text = projectName;
                    listOfInputFields[i].text = releaseInfo.Interval.ToString();
                    listOfToggles[i].isOn = releaseInfo.isActive;
                } else {
                    AddNewLineToWindow(projectName, releaseInfo, i);
                }
                lastIndex = i;
            }
            while (lastIndex < listOfToggles.Count - 1) {
                int last = listOfToggles.Count - 1;
                Destroy(listOfTexts[last].gameObject);
                Destroy(listOfInputFields[last].gameObject);
                Destroy(listOfToggles[last].gameObject);
                listOfTexts.RemoveAt(last);
                listOfInputFields.RemoveAt(last);
                listOfToggles.RemoveAt(last);
            }
        }
        static void AddNewLineToWindow(string projectName, ProjectDeadlineBehaviour.ReleaseInfo releaseInfo, int i) {
            Text projectNameText = WindowManager.SpawnLabel();
            projectNameText.text = projectName;
            listOfTexts.Add(projectNameText);
            WindowManager.AddElementToWindow(projectNameText.gameObject, Window, new Rect(30, 15 + 50 * (i + 1), 200, 25), new Rect(0, 0, 0, 0));

            InputField projectIntervalInput = WindowManager.SpawnInputbox();
            projectIntervalInput.text = releaseInfo.Interval.ToString();
            projectIntervalInput.characterValidation = InputField.CharacterValidation.Integer;
            UnityAction<string> inputEvent = (string s) => {
                WorkItem key = ProjectDeadlineBehaviour.Instance.ReleaseInfos.GetAt(i).Key;
                ProjectDeadlineBehaviour.ReleaseInfo val = ProjectDeadlineBehaviour.Instance.ReleaseInfos.GetAt(i).Value;
                ProjectDeadlineBehaviour.ReleaseInfo newReleaseInfo;
                if (s == "") {
                    newReleaseInfo.Interval = 1;
                } else {
                    newReleaseInfo.Interval = int.Parse(s);
                }
                if (newReleaseInfo.Interval < 1) {
                    newReleaseInfo.Interval = 1;
                }
                if (newReleaseInfo.Interval > 12 * 10) {
                    newReleaseInfo.Interval = 12 * 10;
                }
                newReleaseInfo.isActive = val.isActive;
                newReleaseInfo.isExist = val.isExist;
                ProjectDeadlineBehaviour.Instance.ReleaseInfos[key] = newReleaseInfo;
            };
            projectIntervalInput.onValueChanged.AddListener(inputEvent);
            listOfInputFields.Add(projectIntervalInput);
            WindowManager.AddElementToWindow(projectIntervalInput.gameObject, Window, new Rect(240, 10 + 50 * (i + 1), 75, 30), new Rect(0, 0, 0, 0));

            Toggle isActiveTogle = WindowManager.SpawnCheckbox();
            isActiveTogle.isOn = releaseInfo.isActive;
            UnityAction<bool> toggleEvent = (bool t) => {
                WorkItem key = ProjectDeadlineBehaviour.Instance.ReleaseInfos.GetAt(i).Key;
                ProjectDeadlineBehaviour.ReleaseInfo val = ProjectDeadlineBehaviour.Instance.ReleaseInfos.GetAt(i).Value;
                ProjectDeadlineBehaviour.ReleaseInfo newReleaseInfo;
                newReleaseInfo.Interval = val.Interval;
                newReleaseInfo.isActive = t;
                newReleaseInfo.isExist = val.isExist;
                ProjectDeadlineBehaviour.Instance.ReleaseInfos[key] = newReleaseInfo;
            };
            isActiveTogle.onValueChanged.AddListener(toggleEvent);
            isActiveTogle.GetComponentInChildren<UnityEngine.UI.Text>().text = "";
            listOfToggles.Add(isActiveTogle);
            WindowManager.AddElementToWindow(isActiveTogle.gameObject, Window, new Rect(350, 15 + 50 * (i + 1), 70, 25), new Rect(0, 0, 0, 0));
        }
        static void ClearAllLists() {
            while (listOfToggles.Count != 0) {
                int last = listOfToggles.Count - 1;
                Destroy(listOfTexts[last].gameObject);
                Destroy(listOfInputFields[last].gameObject);
                Destroy(listOfToggles[last].gameObject);
                listOfTexts.RemoveAt(last);
                listOfInputFields.RemoveAt(last);
                listOfToggles.RemoveAt(last);
            }
        }
    }
}
