// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiStudioHelloPlugin
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.PsiStudio.PipelinePlugin;

    /// <summary>
    /// Minimal pipeline plugin entrypoint for PsiStudio.
    /// Must derive directly from <see cref="Window"/> to be discovered by PsiStudioPipelineAssemblyHandler.
    /// </summary>
    public partial class HelloPipelinePluginWindow : Window, IPsiStudioPipeline
    {
        public HelloPipelinePluginWindow()
        {
            // Avoid XAML load failures from missing dependencies by making the window usable
            // even if InitializeComponent fails (PsiStudio loads plugins via reflection).
            try
            {
                this.InitializeComponent();
            }
            catch
            {
                this.Title = "SAAC Hello Pipeline Plugin";
                this.Width = 520;
                this.Height = 260;
                this.Content = new SaaCHelloWorld.Core.HelloWorldView();
            }
        }

        // PsiStudioPipelineAssemblyHandler requires these methods by name.

        public Dataset GetDataset() => null;

        public void RunPipeline(TimeInterval timeInterval)
        {
            // For now, just show the UI; later this is where you'll open the .pds and start the replay pipeline.
            if (!this.IsVisible)
            {
                this.Show();
            }
        }

        public void StopPipeline()
        {
            if (this.IsVisible)
            {
                this.Close();
            }
        }

        public DateTime GetStartTime() => DateTime.UtcNow;

        public PipelineReplaybleMode GetReplaybleMode() => PipelineReplaybleMode.Not;

        public new void Dispose()
        {
            // Called by handler. Keep it simple and safe.
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(() => this.Dispose());
                return;
            }

            if (this.IsVisible)
            {
                this.Close();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Prevent PsiStudio from being blocked by a lingering window.
            base.OnClosing(e);
        }
    }
}

