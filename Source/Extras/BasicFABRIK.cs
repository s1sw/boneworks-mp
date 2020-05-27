//#if UNITY_EDITOR
//using UnityEditor;
//#endif
//using UnityEngine;

///// <summary>
///// Fabrik IK Solver
///// </summary>
//public class BasicFABRIK : MonoBehaviour
//{
//    /// <summary>
//    /// Chain length of bones
//    /// </summary>
//    public int ChainLength = 2;

//    /// <summary>
//    /// Target the chain should bent to
//    /// </summary>
//    public Transform Target;
//    public Transform Pole;

//    /// <summary>
//    /// Solver iterations per update
//    /// </summary>
//    //[Header("Solver Parameters")]
//    public int Iterations = 10;

//    /// <summary>
//    /// Distance when the solver stops
//    /// </summary>
//    public float Delta = 0.001f;

//    /// <summary>
//    /// Strength of going back to the start position.
//    /// </summary>
//    //[Range(0, 1)]
//    public float SnapBackStrength = 1f;


//    protected float[] boneLengths; //Target to Origin
//    protected float totalLength;
//    protected Transform[] bones;
//    protected Vector3[] positions;
//    protected Vector3[] StartDirectionSucc;
//    protected Quaternion[] StartRotationBone;
//    protected Quaternion StartRotationTarget;
//    protected Transform Root;


//    // Start is called before the first frame update
//    void Awake()
//    {
//        Init();
//    }

//    void Init()
//    {
//        //initial array
//        bones = new Transform[ChainLength + 1];
//        positions = new Vector3[ChainLength + 1];
//        boneLengths = new float[ChainLength];
//        StartDirectionSucc = new Vector3[ChainLength + 1];
//        StartRotationBone = new Quaternion[ChainLength + 1];

//        //find root
//        Root = transform;
//        for (var i = 0; i <= ChainLength; i++)
//        {
//            if (Root == null)
//                throw new UnityException("The chain value is longer than the ancestor chain!");
//            Root = Root.parent;
//        }

//        //init target
//        if (Target == null)
//        {
//            Target = new GameObject(gameObject.name + " Target").transform;
//            SetPositionRootSpace(Target, GetPositionRootSpace(transform));
//        }
//        StartRotationTarget = GetRotationRootSpace(Target);


//        //init data
//        var current = transform;
//        totalLength = 0;
//        for (var i = bones.Length - 1; i >= 0; i--)
//        {
//            bones[i] = current;
//            StartRotationBone[i] = GetRotationRootSpace(current);

//            if (i == bones.Length - 1)
//            {
//                //leaf
//                StartDirectionSucc[i] = GetPositionRootSpace(Target) - GetPositionRootSpace(current);
//            }
//            else
//            {
//                //mid bone
//                StartDirectionSucc[i] = GetPositionRootSpace(bones[i + 1]) - GetPositionRootSpace(current);
//                boneLengths[i] = StartDirectionSucc[i].magnitude;
//                totalLength += boneLengths[i];
//            }

//            current = current.parent;
//        }



//    }

//    // Update is called once per frame
//    void LateUpdate()
//    {
//        ResolveIK();
//    }

//    private void ResolveIK()
//    {
//        if (Target == null)
//            return;

//        if (boneLengths.Length != ChainLength)
//            Init();

//        //Fabric

//        //  root
//        //  (bone0) (bonelen 0) (bone1) (bonelen 1) (bone2)...
//        //   x--------------------x--------------------x---...

//        //get position
//        for (int i = 0; i < bones.Length; i++)
//            positions[i] = GetPositionRootSpace(bones[i]);

//        var targetPosition = GetPositionRootSpace(Target);
//        var targetRotation = GetRotationRootSpace(Target);

//        //1st is possible to reach?
//        if ((targetPosition - GetPositionRootSpace(bones[0])).sqrMagnitude >= totalLength * totalLength)
//        {
//            //just strech it
//            var direction = (targetPosition - positions[0]).normalized;
//            //set everything after root
//            for (int i = 1; i < positions.Length; i++)
//                positions[i] = positions[i - 1] + direction * boneLengths[i - 1];
//        }
//        else
//        {
//            for (int i = 0; i < positions.Length - 1; i++)
//                positions[i + 1] = Vector3.Lerp(positions[i + 1], positions[i] + StartDirectionSucc[i], SnapBackStrength);

//            for (int iteration = 0; iteration < Iterations; iteration++)
//            {
//                //https://www.youtube.com/watch?v=UNoX65PRehA
//                //back
//                for (int i = positions.Length - 1; i > 0; i--)
//                {
//                    if (i == positions.Length - 1)
//                        positions[i] = targetPosition; //set it to target
//                    else
//                        positions[i] = positions[i + 1] + (positions[i] - positions[i + 1]).normalized * boneLengths[i]; //set in line on distance
//                }

//                //forward
//                for (int i = 1; i < positions.Length; i++)
//                    positions[i] = positions[i - 1] + (positions[i] - positions[i - 1]).normalized * boneLengths[i - 1];

//                //close enough?
//                if ((positions[positions.Length - 1] - targetPosition).sqrMagnitude < Delta * Delta)
//                    break;
//            }
//        }

//        //move towards pole
//        if (Pole != null)
//        {
//            var polePosition = GetPositionRootSpace(Pole);
//            for (int i = 1; i < positions.Length - 1; i++)
//            {
//                var plane = new Plane(positions[i + 1] - positions[i - 1], positions[i - 1]);
//                var projectedPole = plane.ClosestPointOnPlane(polePosition);
//                var projectedBone = plane.ClosestPointOnPlane(positions[i]);
//                var angle = Vector3.SignedAngle(projectedBone - positions[i - 1], projectedPole - positions[i - 1], plane.normal);
//                positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (positions[i] - positions[i - 1]) + positions[i - 1];
//            }
//        }

//        //set position & rotation
//        for (int i = 0; i < positions.Length; i++)
//        {
//            if (i == positions.Length - 1)
//                SetRotationRootSpace(bones[i], Quaternion.Inverse(targetRotation) * StartRotationTarget * Quaternion.Inverse(StartRotationBone[i]));
//            else
//                SetRotationRootSpace(bones[i], Quaternion.FromToRotation(StartDirectionSucc[i], positions[i + 1] - positions[i]) * Quaternion.Inverse(StartRotationBone[i]));
//            SetPositionRootSpace(bones[i], positions[i]);
//        }
//    }

//    private Vector3 GetPositionRootSpace(Transform current)
//    {
//        if (Root == null)
//            return current.position;
//        else
//            return Quaternion.Inverse(Root.rotation) * (current.position - Root.position);
//    }

//    private void SetPositionRootSpace(Transform current, Vector3 position)
//    {
//        if (Root == null)
//            current.position = position;
//        else
//            current.position = Root.rotation * position + Root.position;
//    }

//    private Quaternion GetRotationRootSpace(Transform current)
//    {
//        //inverse(after) * before => rot: before -> after
//        if (Root == null)
//            return current.rotation;
//        else
//            return Quaternion.Inverse(current.rotation) * Root.rotation;
//    }

//    private void SetRotationRootSpace(Transform current, Quaternion rotation)
//    {
//        if (Root == null)
//            current.rotation = rotation;
//        else
//            current.rotation = Root.rotation * rotation;
//    }

//    void OnDrawGizmos()
//    {
//#if UNITY_EDITOR
//        var current = this.transform;
//        for (int i = 0; i < ChainLength && current != null && current.parent != null; i++)
//        {
//            var scale = Vector3.Distance(current.position, current.parent.position) * 0.1f;
//            Handles.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector3.up, current.parent.position - current.position), new Vector3(scale, Vector3.Distance(current.parent.position, current.position), scale));
//            Handles.color = Color.green;
//            Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
//            current = current.parent;
//        }
//#endif
//    }

//}