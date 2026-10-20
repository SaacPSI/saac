// <copyright file="OT.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication.Examples.ComponentsClass.Structures
{
    public abstract class OT
    {
        public DateTime originatingTime;

        public OT(DateTime originatingTime)
        {
            this.originatingTime = originatingTime;
        }
    }
}
