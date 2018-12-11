using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalableAnimationObject : MonoBehaviour
{
    public enum EScaleAxes
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        XY = X | Y,
        XZ = X | Z,
        YZ = Y | Z,
        XYZ = X | Y | Z
    }


    public Vector3 AnimationClipEnd;

    GameObject Object;
    Transform ObjectPreviousParent;
    float? EndTime;
    System.Action OnFinished;


    public void Animate(GameObject Object, Vector3 From, Vector3 To, EScaleAxes ScaleAxes, System.Action OnFinished)
    {
        this.Object = Object;
        ObjectPreviousParent = Object.transform.parent;
        this.OnFinished = OnFinished;

        var Delta = To - From;

        transform.position = From;
        transform.localScale = new Vector3(
            (ScaleAxes | EScaleAxes.X) != EScaleAxes.None ? 1 : Delta.x / AnimationClipEnd.x,
            (ScaleAxes | EScaleAxes.Y) != EScaleAxes.None ? 1 : Delta.y / AnimationClipEnd.y,
            (ScaleAxes | EScaleAxes.Z) != EScaleAxes.None ? 1 : Delta.z / AnimationClipEnd.z);

        var AnimNode = transform.GetChild(0);
        Object.transform.position = AnimNode.position;
        Object.transform.rotation = AnimNode.rotation;
        Object.transform.SetParent(AnimNode, true);

        var Anim = GetComponentInChildren<Animation>();
        Anim.Play();
        EndTime = Time.time + Anim.clip.length;
    }

    void Update()
    {
        if (EndTime != null && Time.time >= EndTime)
        {
            EndTime = null;
            Object.transform.SetParent(ObjectPreviousParent);
            OnFinished?.Invoke();
            Destroy(gameObject);
        }
    }
}
