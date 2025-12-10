using SAAC.PipelineServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi.Data;
using Microsoft.Psi;
using Microsoft.Psi.Media;
using Microsoft.Psi.Interop;
using Microsoft.Psi.Imaging;
using SharpDX.MediaFoundation.DirectX;
using System.Windows.Media.Animation;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Interop.Format;

namespace Casper_Gathering
{
    internal class Program
    {
        public static string Status = "";
        public static Pipeline videoP = null;
        //public static Subpipeline videoP = null;
        private static bool _isPsiPipelineStarted;
        private static bool _isServerInitialize;
        private static bool _isQuest1RawInitialize;
        private static bool _isQuest2RawInitialize;
        private static bool _isVideoInitialize;
        private static bool _isPipelineInitialize;
        private static Subpipeline audioP = null;

        //static RendezVousPipeline server;
        static void Main(string[] args)
        {
            RendezVousPipelineConfiguration configurationRDV = SetupRendezVousPipelineConfiguration();
            RendezVousPipeline server = new RendezVousPipeline(configurationRDV, "Server"/*, null, (log) => { Status += $"{log}\n"; }*/);
            server.AddNewProcessEvent(CheckAllProcessAreInitialized);

            server.Start();
            server?.TriggerNewProcessEvent("PsiPipeline");

            Console.WriteLine("Press any key to stop the application.");
            Console.ReadLine();

            server?.Stop();
            server?.Dispose();
        }

        private static RendezVousPipelineConfiguration SetupRendezVousPipelineConfiguration()
        {
            RendezVousPipelineConfiguration configurationRDV = new RendezVousPipelineConfiguration();
            configurationRDV.AutomaticPipelineRun = true;
            configurationRDV.Debug = false;
            configurationRDV.StoreMode = RendezVousPipeline.StoreMode.Dictionnary;
            configurationRDV.SessionName = "Unity";
            configurationRDV.DatasetName = "Casper.pds";
            configurationRDV.DatasetPath = @"C:\Users\dapi\Desktop\TestSession\Casper\Stores\";
            configurationRDV.RendezVousHost = "localhost";
            configurationRDV.ClockPort = 11520;

            SpecifyTopicTypeForEachStream(configurationRDV);

            return configurationRDV;
        }
        private static void SpecifyTopicTypeForEachStream(RendezVousPipelineConfiguration configuration)
        {
            
        }

        private static void CheckAllProcessAreInitialized(object sender, (string, Dictionary<string, Dictionary<string, ConnectorInfo>>) e)
        {
            RendezVousPipeline server = sender as RendezVousPipeline;


            switch (e.Item1)
            {
                case "PsiPipeline":
                    if (!_isPsiPipelineStarted) _isPsiPipelineStarted = true;
                    else _isPsiPipelineStarted = false;
                    break;
                case "Server":
                    if (!_isServerInitialize && _isPsiPipelineStarted) _isServerInitialize = true;
                    else if (_isServerInitialize && !_isPsiPipelineStarted) _isServerInitialize = false;
                    break;
                case "Quest1":
                    if (!_isQuest1RawInitialize && _isPsiPipelineStarted) _isQuest1RawInitialize = true;
                    else if (_isQuest1RawInitialize && !_isPsiPipelineStarted) _isQuest1RawInitialize = false;
                    break;
                case "Quest2":
                    if (!_isQuest2RawInitialize && _isPsiPipelineStarted) _isQuest2RawInitialize = true;
                    else if (_isQuest2RawInitialize && !_isPsiPipelineStarted) _isQuest2RawInitialize = false;
                    break;
                case "VideoServer":
                    if (!_isVideoInitialize) _isVideoInitialize = true;
                    else _isVideoInitialize = false;
                    break;
                case "PipelineProcessInitialized":
                    if (!_isPipelineInitialize && _isPsiPipelineStarted) _isPipelineInitialize = true;
                    else if (_isPipelineInitialize && !_isPsiPipelineStarted) _isPipelineInitialize = false;
                    break;

                default:
                    break;
            }
        }
    }
}
