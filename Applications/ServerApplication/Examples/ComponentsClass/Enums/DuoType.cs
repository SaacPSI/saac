// <copyright file="DuoType.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Examples.ComponentsClass.Enums
{
    [System.Serializable]
    public enum DuoType
    {
        p01 = 0,
        p02 = 1,
        p12 = 2,
        /*        p10 = 3,
                p20 = 4,
                p21 = 5,*/
        trio = 5,
        nan = 6,
        all = 7,
    }
}
