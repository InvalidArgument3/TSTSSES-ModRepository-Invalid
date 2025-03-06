using Math0424.AnimationCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Math0424.AnimationCore
{
    interface IKSolver
    {
        bool Solve(Vector3 dest, int iterations);
    }

    enum IKSolverEnum
    {
        FABRIK,
        CCDIK,
    }

}
