// <copyright file="TimeData.cs" company="SAAC">
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
    public class TimeData : OT
    {
        public DateTime startOriginatingTime;
        public DateTime endOriginatingTime;
        public double durationTime;
        public string text;

        public TimeData(DateTime startot, DateTime endot, double time, string txt) : base(startot)
        {
            //originatingTime = startot;
            startOriginatingTime = startot;
            endOriginatingTime = endot;
            durationTime = time;
            text = txt;
        }
    }
}
