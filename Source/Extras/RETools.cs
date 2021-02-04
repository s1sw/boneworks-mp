using MelonLoader;
using StressLevelZero.AI;
using StressLevelZero.Combat;
using StressLevelZero.Pool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Object;

using MultiplayerMod.Representations;

namespace MultiplayerMod
{
    class RETools
    {
        // Prints the properties of a given type
        public static void PrintProps<T>(T t)
        {
            MelonLogger.Log("====== Type " + t.ToString() + "======");

            System.Reflection.PropertyInfo[] props = typeof(T).GetProperties();

            foreach (var pi in props)
            {
                //if (pi.PropertyType.IsPrimitive)
                try
                {
                    var val = pi.GetValue(t);
                    if (val != null)
                        MelonLogger.Log(pi.Name + ": " + val.ToString());
                    else
                        MelonLogger.Log(pi.Name + ": null");
                }
                catch
                {
                    MelonLogger.LogError("Error tring to get property " + pi.Name);
                }
            }
        }

        // Prints the properties of a given component type
        public static void PrintComponentProps<T>(GameObject go)
        {
            try
            {
                if (go == null)
                    MelonLogger.LogError("go was null???");

                T t = go.GetComponent<T>();

                if (t == null)
                    MelonLogger.LogError("Couldn't find component " + t.GetType().Name);

                MelonLogger.Log("====== Component type " + t.ToString() + "======");

                System.Reflection.PropertyInfo[] props = typeof(T).GetProperties();

                foreach (var pi in props)
                {
                    //if (pi.PropertyType.IsPrimitive)
                    try
                    {
                        var val = pi.GetValue(t);
                        if (val != null)
                            MelonLogger.Log(pi.Name + ": " + val.ToString());
                        else
                            MelonLogger.Log(pi.Name + ": null");
                    }
                    catch
                    {
                        MelonLogger.LogError("Error tring to get property " + pi.Name);
                    }
                }
            }
            catch
            {
                MelonLogger.LogError("i don't know anymore");
            }
        }

        // Prints the heirarchy of a given object, with changeable depth
        private void PrintChildHierarchy(GameObject parent, int currentDepth = 0)
        {
            string offset = "";

            for (int j = 0; j < currentDepth; j++)
            {
                offset += "\t";
            }

            MelonLogger.Log(offset + " Has components:");

            foreach (Component c in parent.GetComponents<Component>())
            {
                MelonLogger.Log(offset + c.ToString());
            }

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                GameObject child = parent.transform.GetChild(i).gameObject;



                MelonLogger.Log(offset + "-" + child.name);



                PrintChildHierarchy(child, currentDepth + 1);


            }
        }

#if DEBUG
        private bool useTestModel = false;
        private PlayerRep testRep;
#endif

        public static void DbgUpdate()
        {
            //#if DEBUG
            //if (Input.GetKeyDown(KeyCode.N))
            //{
            //    useTestModel = true;
            //    testRep = new PlayerRep(SteamClient.Name, SteamClient.SteamId);
            //    //smallPlayerIds.Add(SteamClient.SteamId, byte.MaxValue);
            //    playerObjects.Add(byte.MaxValue, testRep);
            //    HandGunChangeMessage hgcm = new HandGunChangeMessage
            //    {
            //        isForOtherPlayer = false,
            //        playerId = byte.MaxValue,
            //        type = GunType.EDER22,
            //        destroy = false
            //    };
            //    serverId = SteamClient.SteamId;

            //    //SendToServer(hgcm, P2PSend.Reliable);
            //    SteamNetworking.SendP2PPacket(SteamClient.SteamId, hgcm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);
            //}

            //if (Input.GetKeyDown(KeyCode.L))
            //{
            //    foreach (var cs in FindObjectsOfType<ClaimedSpawner>())
            //    {
            //        MelonLogger.Log(cs.spawnObject.title + ": " + cs.spawnObject.prefab.GetInstanceID());
            //    }
            //}

            //// Horrid janky testing for bodies
            //// Requires the server to be started first!

            //if (Input.GetKeyDown(KeyCode.M))
            //{
            //    GameObject go = Instantiate(FindObjectFromInstanceID(22072)).Cast<GameObject>();
            //    go.transform.position = Camera.current.transform.position + Camera.current.transform.forward;
            //    MelonLogger.Log(go.name);
            //    Gun gun = go.GetComponent<Gun>();
            //    for (int i = 0; i < 15; i++)
            //    {
            //        GameObject magazineObj = Instantiate(gun.spawnableMagazine.prefab);
            //        magazineObj.transform.position = Camera.current.transform.position + Camera.current.transform.forward + Vector3.up;
            //    }
            //}

            if (Input.GetKeyDown(KeyCode.P))
            {
                Pool nullbodyPool = null;
                foreach (Pool p in FindObjectsOfType<Pool>())
                {
                    if (p.Prefab != null && p.Prefab.name.ToLowerInvariant().Contains("nullbody"))
                    {
                        //MelonLogger.Log("Found nullbody pool");
                        nullbodyPool = p;
                        //MelonLogger.Log("Set nullbody pool");
                    }
                }

                //if (nullbodyPool == null)
                //{
                //    MelonLogger.LogError("Couldn't find the nullbody pool :(");
                //}
                //else
                //{

                for (int i = 0; i < nullbodyPool.transform.childCount; i++)
                {
                    GameObject childNullbody = nullbodyPool.transform.GetChild(i).gameObject;
                    //PrintChildHierarchy(childNullbody);
                    //BoneworksRigTransforms brt = BWUtil.GetHumanoidRigTransforms(childNullbody.transform.Find("brettEnemy@neutral").gameObject);
                    //PrintProps(childNullbody.GetComponent<AIBrain>().behaviour.health);
                    var brain = childNullbody.GetComponent<AIBrain>();
                    //if (!brain.isDead)
                    MelonLogger.Log("A: " + childNullbody.name);
                }
                //}

                foreach (AIBrain brain in FindObjectsOfType<AIBrain>())
                {
                    MelonLogger.Log("B: " + brain.gameObject.name);
                    AIBrain brain2 = brain.gameObject.GetComponent<AIBrain>();
                    Attack attack = new Attack();
                    attack.damage = 0.3f;
                    MelonLogger.Log("pain");
                    brain2.behaviour.health.TakeDamage(1, attack);

                    brain2.behaviour.sfx.Pain(50.0f);
                }

                //nullbodyPool.DespawnAll(true);
            }

            //if (useTestModel)
            //{
            //    Vector3 offsetVec = new Vector3(0.0f, 0.0f, 1.0f);

            //    FullRigTransformMessage frtm = new FullRigTransformMessage
            //    {
            //        posMain = localRigTransforms.main.position + offsetVec,
            //        posRoot = localRigTransforms.root.position + offsetVec,
            //        posLHip = localRigTransforms.lHip.position + offsetVec,
            //        posRHip = localRigTransforms.rHip.position + offsetVec,
            //        posLKnee = localRigTransforms.lKnee.position + offsetVec,
            //        posRKnee = localRigTransforms.rKnee.position + offsetVec,
            //        posLAnkle = localRigTransforms.lAnkle.position + offsetVec,
            //        posRAnkle = localRigTransforms.rAnkle.position + offsetVec,

            //        posSpine1 = localRigTransforms.spine1.position + offsetVec,
            //        posSpine2 = localRigTransforms.spine2.position + offsetVec,
            //        posSpineTop = localRigTransforms.spineTop.position + offsetVec,
            //        posLClavicle = localRigTransforms.lClavicle.position + offsetVec,
            //        posRClavicle = localRigTransforms.rClavicle.position + offsetVec,
            //        posNeck = localRigTransforms.neck.position + offsetVec,
            //        posLShoulder = localRigTransforms.lShoulder.position + offsetVec,
            //        posRShoulder = localRigTransforms.rShoulder.position + offsetVec,
            //        posLElbow = localRigTransforms.lElbow.position + offsetVec,
            //        posRElbow = localRigTransforms.rElbow.position + offsetVec,
            //        posLWrist = localRigTransforms.lWrist.position + offsetVec,
            //        posRWrist = localRigTransforms.rWrist.position + offsetVec,

            //        rotMain = localRigTransforms.main.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRoot = localRigTransforms.root.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLHip = localRigTransforms.lHip.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRHip = localRigTransforms.rHip.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLKnee = localRigTransforms.lKnee.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRKnee = localRigTransforms.rKnee.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLAnkle = localRigTransforms.lAnkle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRAnkle = localRigTransforms.rAnkle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotSpine1 = localRigTransforms.spine1.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotSpine2 = localRigTransforms.spine2.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotSpineTop = localRigTransforms.spineTop.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLClavicle = localRigTransforms.lClavicle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRClavicle = localRigTransforms.rClavicle.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotNeck = localRigTransforms.neck.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLShoulder = localRigTransforms.lShoulder.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRShoulder = localRigTransforms.rShoulder.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLElbow = localRigTransforms.lElbow.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRElbow = localRigTransforms.rElbow.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotLWrist = localRigTransforms.lWrist.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back),
            //        rotRWrist = localRigTransforms.rWrist.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.back)
            //    };

            //    serverId = SteamClient.SteamId;
            //    SteamNetworking.SendP2PPacket(SteamClient.SteamId, frtm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);

            //    testRep.UpdateNameplateFacing(Camera.current.transform);
            //}

            //#endif
        }
    }
}
