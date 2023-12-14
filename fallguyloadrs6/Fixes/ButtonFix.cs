using FGClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace fallguyloadrold.Fixes
{
    public class ButtonFix : MonoBehaviour
    {
        COMMON_Button button;
        GlobalGameStateClient globalGameStateClient;

        public void Start()
        {
            button = GetComponentInParent<COMMON_Button>();
            globalGameStateClient = FindObjectOfType<GlobalGameStateClient>();
        }

        void OnCollisionStay(Collision collision)
        {
            if (button._currentButtonState == COMMON_Button.ButtonState.Primed)
            {
                button.PressButton(globalGameStateClient.GameStateView.SimulationFixedTime);
            }
            else if (button._currentButtonState == COMMON_Button.ButtonState.ReturningToPrimed)
            {
                button.TryApplyResetLaunchForce(collision);
            }
        }
    }
}
