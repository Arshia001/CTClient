using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Backgammon
{
    public class SelectionMarker : MonoBehaviour
    {
        Material Mat;
        bool Visible;
        CheckerStore Store;


        void Start()
        {
            Mat = GetComponent<Renderer>().material;
        }

        public void Init(CheckerStore Store)
        {
            this.Store = Store;
        }

        void Update()
        {
            transform.Rotate(0, -90 * Time.deltaTime, 0, Space.World);

            var Checker = Store.PeekTopChecker();
            var FinalPosition = Store.GetFinalCheckerPosition().Item1;
            var Color = Mat.color;

            if (Mathf.Approximately(Color.a, 0))
                transform.position = FinalPosition;

            // We'll hide the indicator if it's in the wrong position
            bool Shown = Visible && GameManager.Instance.IsInteractionAllowed() && transform.position == FinalPosition && Checker != null && Vector3.Distance(Checker.transform.position, FinalPosition) < 0.001;

            var A = Mathf.MoveTowards(Color.a, Shown ? 1 : 0, Time.deltaTime * 7);
            if (Color.a != A)
            {
                Color.a = A;
                Mat.color = Color;
            }
        }

        public void Show()
        {
            transform.position = Store.GetFinalCheckerPosition().Item1;
            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
        }
    }
}