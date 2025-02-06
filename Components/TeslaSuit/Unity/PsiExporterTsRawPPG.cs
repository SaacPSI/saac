using Microsoft.Psi.Common;
using Microsoft.Psi.Serialization;
using System.Collections.Generic;
using TsSDK;
using UnityEngine;
using SAAC.PsiFormat;

public class RawPPGSerializer : PsiASerializer<List<RawPpgNodeData>>
{
    public override void Serialize(BufferWriter writer, List<RawPpgNodeData> instance, SerializationContext context) { }
    public override void Deserialize(BufferReader reader, ref List<RawPpgNodeData> target, SerializationContext context) { }
}

public class RawPPGNodeSerializer : PsiASerializer<RawPpgNodeData>
{
    public override void Serialize(BufferWriter writer, RawPpgNodeData instance, SerializationContext context) { }
    public override void Deserialize(BufferReader reader, ref RawPpgNodeData target, SerializationContext context) { }
}

public class PsiExporterTsRawPPG : PsiExporter<List<RawPpgNodeData>>
{
    private TsDeviceBehaviour tsDeviceBehaviour;

    // Start is called before the first frame update
    override public void Start()
    {
        base.Start();
        PsiManager.Serializers.Register<List<RawPpgNodeData>, RawPPGSerializer>();
        PsiManager.Serializers.Register<RawPpgNodeData, RawPPGNodeSerializer>();
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
            device.Device.Biometry.Ppg.RawUpdated += TSDevicePpgRawUpdated;
            device.Device.Biometry.Ppg.Start();
        }
        else
            device.Device.Biometry.Ppg.RawUpdated -= TSDevicePpgRawUpdated;
    }

    private void TSDevicePpgRawUpdated(TsSDK.IRawPpgData data)
    {
        if (CanSend())
        {
            List<RawPpgNodeData> listData = new List<RawPpgNodeData>();
            foreach (RawPpgNodeData info in data.NodesData)
                listData.Add(info);
            Out.Post(listData, Timestamp);
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<List<RawPpgNodeData>> GetSerializer()
    {
        return PsiFormatTsRawPPG.GetFormat();
    }
#endif
}
