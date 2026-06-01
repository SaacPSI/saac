// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiStudioHelloPlugin
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.PsiStudio.PipelinePlugin;

    /// <summary>
    /// Interface required by PsiStudio's pipeline plugin loader.
    /// The loader checks the interface name via reflection ("IPsiStudioPipeline").
    /// </summary>
    public interface IPsiStudioPipeline
    {
        Dataset GetDataset();

        void RunPipeline(TimeInterval timeInterval);

        void StopPipeline();

        void Dispose();

        DateTime GetStartTime();

        PipelineReplaybleMode GetReplaybleMode();
    }
}

