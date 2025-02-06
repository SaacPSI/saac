using Microsoft.Psi.Common;
using Microsoft.Psi.Serialization;
using System.Collections.Generic;
using TsSDK;
using UnityEngine;
using SAAC.PsiFormat;

public class PPGSerializer : PsiASerializer<List<ProcessedPpgNodeData>>
{
    public override void Serialize(BufferWriter writer, List<ProcessedPpgNodeData> instance, SerializationContext context) { }
    public override void Deserialize(BufferReader reader, ref List<ProcessedPpgNodeData> target, SerializationContext context) { }
}

public class ProcessedPPGSerializer : PsiASerializer<TsSDK.ProcessedPpgNodeData>
{
    public override void Serialize(BufferWriter writer, TsSDK.ProcessedPpgNodeData instance, SerializationContext context) { }
    public override void Deserialize(BufferReader reader, ref TsSDK.ProcessedPpgNodeData target, SerializationContext context) { }
}

public class PsiExporterTsPPG : PsiExporter<List<ProcessedPpgNodeData>>
{
    private TsDeviceBehaviour tsDeviceBehaviour;

    // Start is called before the first frame update
    override public void Start()
    {
        base.Start();
        PsiManager.Serializers.Register<List<ProcessedPpgNodeData>, PPGSerializer>();
        PsiManager.Serializers.Register<TsSDK.ProcessedPpgNodeData, ProcessedPPGSerializer>();
        tsDeviceBehaviour = FindAnyObjectByType<TsDeviceBehaviour>();
        if (tsDeviceBehaviour == null)
        {
            Debug.LogError("Could not found tsDeviceBehaviour script ! Have you put it in your scene ?");
            return;
        }
        tsDeviceBehaviour.ConnectionStateChanged += TsDeviceConnectionStateChanged;
        if (tsDeviceBehaviour.IsConnected)
            TsDeviceConnectionStateChanged(tsDeviceBehaviour, true);
    }

    private void TsDeviceConnectionStateChanged(TsDeviceBehaviour device, bool isConnected)
    {
        if (isConnected)
        {
            device.Device.Biometry.Ppg.ProcessedUpdated += TSDevicePpgUpdated;
            device.Device.Biometry.Ppg.Start();
        }
        else
            device.Device.Biometry.Ppg.ProcessedUpdated -= TSDevicePpgUpdated;
    }

    private void TSDevicePpgUpdated(TsSDK.IProcessedPpgData data)
    {
        if (CanSend())
        {
            List<ProcessedPpgNodeData> listData = new List<ProcessedPpgNodeData>();
            foreach(ProcessedPpgNodeData info in data.NodesData)
                listData.Add(info);
            Out.Post(listData, Timestamp);
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<List<ProcessedPpgNodeData>> GetSerializer()
    {
        return PsiFormatTsPPG.GetFormat();
    }
#endif
}
