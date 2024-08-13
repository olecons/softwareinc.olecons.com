using System.Collections.Generic;
using UnityEngine;

namespace Olecons {
    public class OleconsMod : ModMeta {
        public static bool GiveMeFreedom = true;
        public static string Version = "0.1";
        public static bool ModActive { get; set; }
        public static bool ComeOffice { get; set; }
        public static bool GoHome { get; set; }

        public override void ConstructOptionsScreen(RectTransform parent, bool inGame) {
            if (inGame) {
                var label = WindowManager.SpawnLabel();
                label.text = "Olecons v" + Version + " was created by Naveen Raghuvanshi.";
                WindowManager.AddElementToElement(label.gameObject, parent.gameObject, new Rect(0, 0, 400, 75),
                    new Rect(0, 0, 0, 0));
                
                
                List<GameObject> objs = new List<GameObject>();
            
                var buttonMoney = WindowManager.SpawnButton();
                buttonMoney.GetComponentInChildren<UnityEngine.UI.Text> ().text = "Come Office";
                buttonMoney.onClick.AddListener(() =>
                {
                    ComeOffice = true;
                });
                WindowManager.AddElementToElement(buttonMoney.gameObject, parent.gameObject, new Rect(0, 0, 400, 75),
                    new Rect(0, 0, 0, 0));
                
                
                var goHomeButton = WindowManager.SpawnButton();
                goHomeButton.GetComponentInChildren<UnityEngine.UI.Text> ().text = "Go Home";
                goHomeButton.onClick.AddListener(() =>
                {
                    GoHome = true;
                });
                WindowManager.AddElementToElement(goHomeButton.gameObject, parent.gameObject, new Rect(0, 50, 400, 75),
                    new Rect(0, 0, 0, 0));
            }
        }

        public override string Name {
            get { return "Olecons"; }
        }
    }
}
