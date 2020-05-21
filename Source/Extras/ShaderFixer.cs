using UnityEngine;

namespace MultiplayerMod.Extras
{
    public static class ShaderFixer
    {
        public static void Fix(GameObject target)
        {
            Shader VRStandard = Shader.Find("Valve/vr_standard");

            LineRenderer _lr = target.GetComponent<LineRenderer>();

            if (_lr)
            {
                foreach (Material m in _lr.sharedMaterials)
                {
                    m.shader = VRStandard;
                }
            }

            ParticleSystemRenderer _psr = target.GetComponent<ParticleSystemRenderer>();

            if (_psr)
            {
                foreach (Material m in _psr.sharedMaterials)
                {
                    m.shader = VRStandard;
                }
            }

            if (_lr)
            {
                foreach (Material m in _lr.sharedMaterials)
                {
                    m.shader = VRStandard;
                }
            }

            foreach (SkinnedMeshRenderer smr in target.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = VRStandard;
                }
            }

            foreach (MeshRenderer mr in target.GetComponentsInChildren<MeshRenderer>())
            {
                foreach (Material m in mr.sharedMaterials)
                {
                    m.shader = VRStandard;
                }
            }

            foreach (LineRenderer lr in target.GetComponentsInChildren<LineRenderer>())
            {
                foreach (Material m in lr.sharedMaterials)
                {
                    m.shader = VRStandard;
                }

                foreach (Material m in lr.materials)
                {
                    m.shader = VRStandard;
                }
            }

            foreach (TrailRenderer tr in target.GetComponentsInChildren<TrailRenderer>())
            {
                foreach (Material m in tr.sharedMaterials)
                {
                    m.shader = VRStandard;
                }

                foreach (Material m in tr.materials)
                {
                    m.shader = VRStandard;
                }
            }

            foreach (ParticleSystemRenderer psr in target.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                foreach (Material m in psr.sharedMaterials)
                {
                    m.shader = VRStandard;
                }

                foreach (Material m in psr.materials)
                {
                    m.shader = VRStandard;
                }
            }
        }
    }
}
