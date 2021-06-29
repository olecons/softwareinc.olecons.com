using System.Collections.Generic;

namespace ProjectDeadline {
    class ProjectDeadlineBehaviour : ModBehaviour {

        public static ProjectDeadlineBehaviour Instance;

        public struct ReleaseInfo {
            public int Interval;
            public bool isActive;
            public bool isExist;
        }

        int cleanupInterval = 10;
        int currentFrame = 0;

        public Dictionary<WorkItem, ReleaseInfo> ReleaseInfos = new Dictionary<WorkItem, ReleaseInfo>();

        private void Awake() {
            Instance = this;
        }

        public override void OnActivate() {
            ProjectDeadlineMod.ModActive = true;

            if (ProjectDeadlineMod.ModActive && GameSettings.Instance != null && HUD.Instance != null) {
                HUD.Instance.AddPopupMessage("Mod ProjectDeadline has been activated.", "Cogs", PopupManager.PopUpAction.None,
                    0, PopupManager.NotificationSound.Neutral, 0f, PopupManager.PopupIDs.None, 0);
            }
        }

        public override void OnDeactivate() {
            ProjectDeadlineMod.ModActive = true;
        }

        private void Start() {
            if (!ProjectDeadlineMod.ModActive || !isActiveAndEnabled) {
                return;
            }
        }

        public void Update() {
            if (ProjectDeadlineMod.ModActive) {
                if (GameSettings.Instance != null) {
                    CleanUp();
                    foreach (WorkItem workItem in GameSettings.Instance.MyCompany.WorkItems) {
                        if (workItem.GetWorkTypeName() == "Project management") {
                            AutoDevWorkItem autoDevWorkItem = (AutoDevWorkItem)workItem;
                            // There are no intervals => new game or loaded
                            // Cannot load and save my data in mode
                            // So either way assume that the current interval is THE interval
                            if (!ReleaseInfos.ContainsKey(workItem)) {
                                ReleaseInfos[workItem] = CalcReleaseInfo(autoDevWorkItem);
                            }
                            foreach (AutoDevWorkItem.AutoDevItem autoDevItem in autoDevWorkItem.Items) {
                                if (autoDevItem.ReleaseDateText == "None" && ReleaseInfos[workItem].isActive) {
                                    autoDevItem.MonthsToSpend = ReleaseInfos[workItem].Interval;
                                }
                            }
                        }
                    }
                }
            }
        }

        static ReleaseInfo CalcReleaseInfo(AutoDevWorkItem autoDevWorkItem) {
            ReleaseInfo releaseInfo;
            releaseInfo.isExist = true;
            releaseInfo.isActive = true;
            releaseInfo.Interval = 24;
            if (autoDevWorkItem.Items.Count == 0) {
                // none
            } else if (autoDevWorkItem.Items.Count == 1) {
                releaseInfo.Interval = (int)autoDevWorkItem.Items[0].MonthsToSpend;
            } else {
                for (int i = 1; i < autoDevWorkItem.Items.Count; i++) {
                    if ((int)autoDevWorkItem.Items[i - 1].MonthsToSpend != (int)autoDevWorkItem.Items[i].MonthsToSpend) {
                        releaseInfo.isActive = false;
                        break;
                    }
                }
                if (releaseInfo.isActive) {
                    releaseInfo.Interval = (int)autoDevWorkItem.Items[0].MonthsToSpend;
                }
            }
            return releaseInfo;
        }
        void CleanUp() {
            currentFrame++;
            if (currentFrame > cleanupInterval && ReleaseInfos.Count > 0) {
                for (int i = 0; i < ReleaseInfos.Count; i++) {
                    ReleaseInfo releaseInfo = ReleaseInfos.GetAt(i).Value;
                    releaseInfo.isExist = false;
                    ReleaseInfos[ReleaseInfos.GetAt(i).Key] = releaseInfo;
                }

                foreach (WorkItem workItem in GameSettings.Instance.MyCompany.WorkItems) {
                    if (workItem.GetWorkTypeName() == "Project management") {
                        if (ReleaseInfos.ContainsKey(workItem)) {
                            ReleaseInfo releaseInfo = ReleaseInfos[workItem];
                            releaseInfo.isExist = true;
                            ReleaseInfos[workItem] = releaseInfo;
                        }
                    }
                }

                List<int> IndexesToDelete = new List<int>();

                for (int i = ReleaseInfos.Count - 1; i > -1; i--) {
                    if (!ReleaseInfos.GetAt(i).Value.isExist) {
                        IndexesToDelete.Add(i);
                    }
                }

                for (int i = 0; i < IndexesToDelete.Count; i++) {
                    ReleaseInfos.Remove(ReleaseInfos.GetAt(IndexesToDelete[i]).Key);
                }
                currentFrame = 0;
            }
        }
    }
}
