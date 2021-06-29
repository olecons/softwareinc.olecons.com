using UnityEngine;

namespace ProjectDeadline {
    public class ProjectDeadlineMod : ModMeta {
        public static string Version = "0.1";
        public static bool ModActive { get; set; }

        public override void ConstructOptionsScreen(RectTransform parent, bool inGame) {
            if (inGame) {
                var label = WindowManager.SpawnLabel();
                label.text = "ProjectDeadline v" + Version + " was created by Evedel. https://github.com/Evedel/SoftwareInc-ProjectDeadline";
                WindowManager.AddElementToElement(label.gameObject, parent.gameObject, new Rect(0, 0, 400, 75),
                    new Rect(0, 0, 0, 0));
            }
        }

        public override string Name {
            get { return "ProjectDeadline"; }
        }
    }
}
