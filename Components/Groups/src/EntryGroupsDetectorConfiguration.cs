using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAAC.Groups
{
    public class EntryGroupsDetectorConfiguration
    {
        /// <summary>
        /// Gets or sets the threshold time (in second).
        /// </summary>
        public TimeSpan GroupFormationDelay { get; set; } = new TimeSpan(0, 0, 2);
    }
}
