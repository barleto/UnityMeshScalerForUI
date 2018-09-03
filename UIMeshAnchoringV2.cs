using UnityEngine;
using System.Collections;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIMeshAnchoringV2 : MonoBehaviour
{
    [Header("If false, will scale only once, in Start().")]
    [Space(-10)]
    [Header("Turn off for Spine meshes.")]

    public bool ScaleAtUpdate = true;
    public Vector2 minAnchor = new Vector2(0.5f, 0.5f);
    public Vector2 maxAnchor = new Vector2(0.5f, 0.5f);

    [HideInInspector]
    public Vector2 minOffset = new Vector2(0, 0);
    [HideInInspector]
    public Vector2 maxOffset = new Vector2(0, 0);
    [HideInInspector]
    public Renderer refRenderer;
    [HideInInspector]
    public Bounds refBounds;
    [HideInInspector]
    public RectTransform refParentTransform;
    [HideInInspector]
    public Vector3 refScale = new Vector3(1, 1, 1);
    [HideInInspector]
    public Vector3[] refParentCorners = new Vector3[4];


    public Vector3 GetMinAnchorCurrentWorldPosition(Vector3[] corners)
    {
        Vector3 res = new Vector3(
                corners[0].x - minAnchor.x * (corners[0].x - corners[2].x),
                corners[0].y - minAnchor.y * (corners[0].y - corners[2].y),
                transform.position.z);
        return res;
    }

    public Vector3 GetMaxAnchorCurrentWorldPosition(Vector3[] corners)
    {
        Vector3 res = new Vector3(
            corners[0].x - maxAnchor.x * (corners[0].x - corners[2].x),
            corners[0].y - maxAnchor.y * (corners[0].y - corners[2].y),
            transform.position.z);
        return res;
    }

    private void CalculateMeshSize()
    {
        Vector3[] parentCorners = new Vector3[4];
        refParentTransform.GetWorldCorners(parentCorners);

        var min = (Vector2)GetMinAnchorCurrentWorldPosition(parentCorners) + minOffset;
        var max = (Vector2)GetMaxAnchorCurrentWorldPosition(parentCorners) + maxOffset;
        var currentRect = refRenderer.bounds;

        float xFactor, yFactor;
        xFactor = refBounds.extents.x != 0 ? (max.x - min.x) / (refBounds.extents.x*2) : 1;
        yFactor = refBounds.extents.y != 0 ? (max.y - min.y) / (refBounds.extents.y*2) : 1;
        transform.localScale = new Vector3(refScale.x * (xFactor),
            refScale.y * (yFactor),
            refScale.z);
        RecenterMesh(max, min, xFactor, yFactor);
        
    }

    private void RecenterMesh(Vector2 max, Vector2 min, float xFactor, float yFactor)
    {
        var mean = (max + min) / 2;
        transform.position = new Vector3(mean.x, mean.y, transform.position.z);
        transform.position += transform.position - refRenderer.bounds.center;
    }

    public void SetOffsets(Vector3[] corners)
    {
        minOffset = new Vector2(
            refBounds.min.x - GetMinAnchorCurrentWorldPosition(corners).x,
            refBounds.min.y - GetMinAnchorCurrentWorldPosition(corners).y);
        maxOffset = new Vector2(
            refBounds.max.x - GetMaxAnchorCurrentWorldPosition(corners).x,
            refBounds.max.y - GetMaxAnchorCurrentWorldPosition(corners).y);
    }

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        CalculateMeshSize();
    }

    private void Update()
    {
        if (ScaleAtUpdate)
        {
            CalculateMeshSize();
        }
    }

#if UNITY_EDITOR
    bool hasStarted = false;

    private void OnDrawGizmos()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }
        if (Selection.Contains(this.gameObject.GetInstanceID()))
        {
            RecordReferenceParameters();
            refParentTransform.GetWorldCorners(refParentCorners);

            if (!hasStarted)
            {
                SetOffsets(refParentCorners);
                hasStarted = true;
            }
            SetOffsets(refParentCorners);
        }
        else
        {
            CalculateMeshSize();
        }
    }

    private void RecordReferenceParameters()
    {
        if (refRenderer == null)
        {
            refRenderer = GetComponent<Renderer>();
        }
        if (refParentTransform == null)
        {
            refParentTransform = transform.parent as RectTransform;
        }
        refParentTransform = transform.parent as RectTransform;
        refBounds = refRenderer.bounds;
        refScale = transform.localScale;
    }

    void OnDrawGizmosSelected()
    {
        if (refParentTransform == null)
        {
            return;
        }

        if (Selection.Contains(this.gameObject.GetInstanceID()))
        {
            Vector3[] parentCorners = new Vector3[4];
            refParentTransform.GetWorldCorners(parentCorners);

            Gizmos.color = new Color(1,1,1,0.3f);
            Gizmos.DrawLine(parentCorners[0], parentCorners[1]);
            Gizmos.DrawLine(parentCorners[1], parentCorners[2]);
            Gizmos.DrawLine(parentCorners[2], parentCorners[3]);
            Gizmos.DrawLine(parentCorners[3], parentCorners[0]);

            /*float radius = (HandleUtility.GetHandleSize(new Vector3(0, 0, 0))) * 0.1f;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(new Vector3(refBounds.min.x, refBounds.min.y, transform.position.z), radius);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(refBounds.max.x, refBounds.max.y, transform.position.z), radius);*/

            var min = (Vector2)GetMinAnchorCurrentWorldPosition(parentCorners) + minOffset;
            var max = (Vector2)GetMaxAnchorCurrentWorldPosition(parentCorners) + maxOffset;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(GetMinAnchorCurrentWorldPosition(parentCorners), new Vector3(refBounds.min.x, refBounds.min.y, transform.position.z));

            Gizmos.color = Color.green;
            Gizmos.DrawLine(GetMaxAnchorCurrentWorldPosition(parentCorners), new Vector3(refBounds.max.x, refBounds.max.y, transform.position.z));

        }
    }
#endif

}

#if UNITY_EDITOR
[CustomEditor(typeof(UIMeshAnchoringV2))]
[CanEditMultipleObjects]
public class UIScalerEditorV2 : Editor
{

    public bool executeUpdate = false;

    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }

    protected virtual void OnSceneGUI()
    {
        UIMeshAnchoringV2 obj = (UIMeshAnchoringV2)target;
        Vector3[] corners = new Vector3[4];
        if (obj.refParentTransform == null)
        {
            return;
        }
        obj.refParentTransform.GetWorldCorners(corners);
        Vector3 minAnchor;
        Vector3 maxAnchor;

        float size = 1;
        float handleSize = HandleUtility.GetHandleSize(new Vector3(size, size, 1));
        Vector3 snap = Vector3.one * 0.5f;

        EditorGUI.BeginChangeCheck();
        
        minAnchor = Handles.PositionHandle(obj.GetMinAnchorCurrentWorldPosition(corners), Quaternion.identity);
        maxAnchor = Handles.PositionHandle(obj.GetMaxAnchorCurrentWorldPosition(corners), Quaternion.identity);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(obj, "Anchors Positions Changed");

            obj.minAnchor = new Vector2(
             Mathf.Clamp01( (corners[0].x - minAnchor.x) / (corners[0].x - corners[2].x) ),
             Mathf.Clamp01( (corners[0].y - minAnchor.y) / (corners[0].y - corners[2].y) ) );

            obj.maxAnchor = new Vector2(
                 Mathf.Clamp01( (corners[0].x - maxAnchor.x) / (corners[0].x - corners[2].x) ),
                 Mathf.Clamp01( (corners[0].y - maxAnchor.y) / (corners[0].y - corners[2].y) ) );
            obj.SetOffsets(corners);

        }
    }
}
#endif
 
