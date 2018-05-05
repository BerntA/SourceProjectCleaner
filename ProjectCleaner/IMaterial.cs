//=========       Copyright © Reperio Studios 2013-2018 @ Bernt Andreas Eide!       ============//
//
// Purpose: IMATERIAL - All materials implement these:
//
//=============================================================================================//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCleaner
{
    public interface IMaterial
    {
        string getShader();
        string getParam(string key);
        string getBaseTexture1();
        string getBaseTexture2();
        string surfaceprop();
        string getBumpMap();
        string[] getProxies();
    }
}
