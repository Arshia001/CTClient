using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Color = BackgammonLogic.Color;

namespace Backgammon
{
    public class Checker : MonoBehaviour
    {
        enum EMovementMode
        {
            None,
            Anim,
            Hover
        }


        const float AnimDuration = 0.5f;
        const float AnimHeight = 0.03f;
        const float HoverHeight = 0.05f;
        const float RotationFactor = 720.0f;
        const float MaxRotation = 45.0f;


        public AudioClip[] PlaceSound;

        EMovementMode MovementMode;

        float AnimStartTime;
        Vector3 AnimStartPosition;
        Quaternion AnimStartRotation;
        Queue<Tuple<Vector3, Quaternion, Action>> AnimQueue = new Queue<Tuple<Vector3, Quaternion, Action>>();

        int? Hovered;
        bool WasHovering;

        Vector3 TargetPosition;
        Quaternion TargetRotation;
        Quaternion BaseRotation;


        public void Initialize(CheckerStore Container)
        {
            var MyTransform = Container.AddChecker(this);
            TargetPosition = transform.position = MyTransform.Item1;
            TargetRotation = MyTransform.Item2;
            transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f), Space.World);
            BaseRotation = transform.rotation;
        }

        void Update()
        {
            switch (MovementMode)
            {
                case EMovementMode.Anim:
                    {
                        var T = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.time - AnimStartTime) / AnimDuration));

                        var NewPos = Vector3.Lerp(AnimStartPosition, TargetPosition, T);

                        var PeakY = Mathf.Max(AnimStartPosition.y, TargetPosition.y) + AnimHeight;
                        if (T < 0.5f)
                            NewPos.y = AnimStartPosition.y + (PeakY - AnimStartPosition.y) * (1 - (float)Math.Pow(2 * T - 1, 2));
                        else
                            NewPos.y = TargetPosition.y + (PeakY - TargetPosition.y) * (1 - (float)Math.Pow(2 * T - 1, 2));

                        transform.position = NewPos;

                        transform.rotation = Quaternion.Lerp(AnimStartRotation, TargetRotation * BaseRotation, T);

                        if (T >= 1)
                        {
                            PlayPlaceSound();
                            MovementMode = EMovementMode.None;
                        }
                    }
                    break;

                case EMovementMode.Hover:
                    {
                        WasHovering = true;
                        var P = ExtendedInputModule.GetPointerEventData();
                        if (Input.GetKey(KeyCode.Mouse0))
                        {
                            var Ray = Camera.main.ScreenPointToRay(P.position);
                            var Plane = new Plane(Vector3.up, -HoverHeight);
                            float Enter;
                            if (Plane.Raycast(Ray, out Enter))
                            {
                                var TargetPosition = Ray.GetPoint(Enter);

                                var DeltaPos = transform.position - TargetPosition;
                                var RotVector = new Vector3(Mathf.Clamp(-DeltaPos.z * RotationFactor, -MaxRotation, MaxRotation), 0, Mathf.Clamp(DeltaPos.x * RotationFactor, -MaxRotation, MaxRotation));

                                transform.position = Vector3.Lerp(transform.position, TargetPosition, 0.3f);
                                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(RotVector) * BaseRotation, 0.9f);
                            }

                            if (P != null)
                                foreach (var GO in P.hovered)
                                {
                                    var CS = GO.GetComponent<CheckerStore>();
                                    var ToIdx = CS?.GetToPointIndex();
                                    if (ToIdx != null)
                                    {
                                        Hovered = ToIdx;
                                        return;
                                    }
                                }
                        }
                        else
                        {
                            if (P != null)
                                foreach (var GO in P.hovered)
                                {
                                    var CS = GO.GetComponent<CheckerStore>();
                                    var ToIdx = CS?.GetToPointIndex();
                                    if (ToIdx != null)
                                    {
                                        Hovered = ToIdx;
                                        return;
                                    }
                                }

                            GameManager.Instance.OnHoverFinished(Hovered);

                            Hovered = null;
                        }
                    }
                    break;

                default:
                    {
                        if (AnimQueue.Count > 0 && Dice.Instance.IsAnimDone())
                        {
                            WasHovering = false;

                            MovementMode = EMovementMode.Anim;

                            AnimStartTime = Time.time;
                            AnimStartPosition = transform.position;
                            AnimStartRotation = transform.rotation;

                            var Target = AnimQueue.Dequeue();
                            TargetPosition = Target.Item1;
                            TargetRotation = Target.Item2;
                            Target.Item3?.Invoke();
                        }
                        else
                        {
                            transform.position = Vector3.Lerp(transform.position, TargetPosition, 0.15f);
                            transform.rotation = Quaternion.Lerp(transform.rotation, TargetRotation * BaseRotation, 0.2f);

                            if (WasHovering && Vector3.Distance(transform.position, TargetPosition) < 0.001f)
                            {
                                WasHovering = false;
                                PlayPlaceSound();
                            }
                        }
                    }
                    break;
            }
        }

        public bool IsAnimating()
        {
            return MovementMode == EMovementMode.Anim;
        }

        void PlayPlaceSound()
        {
            if (PlaceSound.Length > 0)
                AudioSource.PlayClipAtPoint(PlaceSound[UnityEngine.Random.Range(0, PlaceSound.Length)], transform.position);
        }

        public void Die()
        {
            Destroy(gameObject);
        }

        public void QueueMoveTo(CheckerStore NewContainer, Action OnAnimStarted)
        {
            var Transform = NewContainer.AddChecker(this);

            if (MovementMode == EMovementMode.Anim || !Dice.Instance.IsAnimDone())
                AnimQueue.Enqueue(new Tuple<Vector3, Quaternion, Action>(Transform.Item1, Transform.Item2, OnAnimStarted));
            else
            {
                TargetPosition = Transform.Item1;
                TargetRotation = Transform.Item2;
                OnAnimStarted?.Invoke();

                if (MovementMode == EMovementMode.None)
                {
                    MovementMode = EMovementMode.Anim;
                    AnimStartTime = Time.time;
                    AnimStartPosition = transform.position;
                    AnimStartRotation = transform.rotation;
                }
            }
        }

        public void StartHover()
        {
            Tuple<Vector3, Quaternion, Action> LastTarget = null;
            while (AnimQueue.Count > 0)
                LastTarget = AnimQueue.Dequeue();

            if (LastTarget != null)
            {
                TargetPosition = LastTarget.Item1;
                TargetRotation = LastTarget.Item2;
                LastTarget.Item3?.Invoke();
            }

            MovementMode = EMovementMode.Hover;
        }

        public void EndHover()
        {
            MovementMode = EMovementMode.None;
        }
    }
}