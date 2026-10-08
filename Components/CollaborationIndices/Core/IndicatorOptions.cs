// <copyright file="IndicatorOptions.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using Newtonsoft.Json;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Options shared by every indicator. An indicator derives from it to add its own; the
    /// properties must stay serializable, since they are also set from a configuration file.
    /// </summary>
    public class IndicatorOptions
    {
        /// <summary>
        /// Gets or sets the name of the indicator inside the instance, which is also the name
        /// of its outputs. Null keeps the default name of the indicator. Set it to declare the
        /// same kind of indicator twice.
        /// </summary>
        public string? Name { get; set; } = null;

        /// <summary>
        /// Gets or sets the sliding window of this indicator. Null uses the window of the builder.
        /// </summary>
        public TimeSpan? WindowDuration { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the indicator feeds the collaboration score.
        /// It still is computed, published and exported when false.
        /// </summary>
        public bool IncludeInScore { get; set; } = true;
    }

    /// <summary>
    /// Options of an indicator built on one component: the serializable options, plus a hook
    /// reaching every setting of the component configuration from code.
    /// </summary>
    /// <typeparam name="TConfiguration">Configuration type of the component.</typeparam>
    public class ComponentIndicatorOptions<TConfiguration> : IndicatorOptions
    {
        /// <summary>
        /// Gets or sets an action applied to the component configuration once the options
        /// have been copied into it. It is not serialized: use it from code for the settings
        /// that are not exposed as options (normalizers, measures, aggregators).
        /// </summary>
        [JsonIgnore]
        public Action<TConfiguration>? Configure { get; set; } = null;
    }
}
