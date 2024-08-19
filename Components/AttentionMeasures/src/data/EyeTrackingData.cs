using System.IO;

namespace SAAC.AttentionMeasures
{
    public enum ETData
    {
        LeftEyePosition,
        LeftEyeRotation,
        LeftGaze,
        RightEyePosition,
        RightEyeRotation,
        RightGaze,
        HeadDirection,
        CenterEyePosition,
        AverageGaze,
        IsGazingAtSomething,
        FirstIntersectionPoint,
        GazedObjectID,
        GazedObjectName,
        HasEyeTrackingTags,
        EyeTrackingTagsList
    }


    public interface IEyeTracking
    {
        abstract Type GetType();
        abstract void Write(BinaryWriter writer);
        abstract IEyeTracking Read(BinaryReader reader);
    }

    public class EyeTrackingInt : IEyeTracking
    {

        public int content;
        public EyeTrackingInt() { content = 0; }
        public EyeTrackingInt(int i) { content = i; }
        public new Type GetType() { return typeof(EyeTrackingInt); }
        public void Write(BinaryWriter writer) { writer.Write(content); }
        public IEyeTracking Read(BinaryReader reader) { return new EyeTrackingInt(reader.ReadInt32()); }
        public override string ToString() { return content.ToString(); }
        public bool Compare(EyeTrackingInt other) { return content == other.content; }
    }

    public class EyeTrackingBool : IEyeTracking
    {
        public bool content;
        public EyeTrackingBool() { content = false; }
        public EyeTrackingBool(bool b) { content = b; }
        public new Type GetType() { return typeof(EyeTrackingBool); }
        public void Write(BinaryWriter writer) { writer.Write(content); }
        public IEyeTracking Read(BinaryReader reader) { return new EyeTrackingBool(reader.ReadBoolean()); }
        public override string ToString() { return content.ToString(); }
        public bool Compare(EyeTrackingBool other) { return content == other.content; }
    }

    public class EyeTrackingString : IEyeTracking
    {
        public string content;
        public EyeTrackingString() { content = ""; }
        public EyeTrackingString(string s) { content = s; }
        public new Type GetType() { return typeof(EyeTrackingString); }
        public void Write(BinaryWriter writer) { writer.Write(content); }
        public IEyeTracking Read(BinaryReader reader) { return new EyeTrackingString(reader.ReadString()); }
        public override string ToString() { return content; }
        public bool Compare(EyeTrackingString other) { return content == other.content; }
    }

    public class EyeTrackingVector3 : IEyeTracking
    {

        public System.Numerics.Vector3 content;

        public EyeTrackingVector3() { content.X = 0; content.Y = 0; content.Z = 0; }
        public EyeTrackingVector3(float a, float b, float c) { content.X = a; content.Y = b; content.Z = c; }

        public new Type GetType() { return typeof(EyeTrackingVector3); }
        public void Write(BinaryWriter writer) { writer.Write((double)content.X); writer.Write((double)content.Y); writer.Write((double)content.Z); }
        public IEyeTracking Read(BinaryReader reader) { return new EyeTrackingVector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()); }
        public override string ToString() { return content.ToString(); }
        public bool Compare(EyeTrackingVector3 other) { return content == other.content; }
    }

    public class EyeTrackingStringList : IEyeTracking
    {
        public List<string> content;
        public EyeTrackingStringList() { content = new List<string>(); }
        public EyeTrackingStringList(List<string> l) { content = l; }
        public new Type GetType() { return typeof(EyeTrackingStringList); }
        public void Write(BinaryWriter writer) { writer.Write(content.Count()); foreach (string s in content) writer.Write(s); }
        public IEyeTracking Read(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<string> l = new List<string>(count);
            for (int i = 0; i < count; i++) { l.Add(reader.ReadString()); }
            return new EyeTrackingStringList(l);
        }
        public override string ToString()
        {
            string l = "";
            foreach (string s in content) { l += s + " ; "; }
            return l;
        }
        public bool Compare(EyeTrackingStringList other) { return content == other.content; }
    }


    public class EyeTrackingTemplate : IEyeTracking
    {
        public Dictionary<ETData, IEyeTracking> content;

        public EyeTrackingTemplate()
        {
            content = new Dictionary<ETData, IEyeTracking>()
        {
            {ETData.LeftEyePosition, new EyeTrackingVector3() },
            {ETData.LeftEyeRotation, new EyeTrackingVector3() },
            {ETData.LeftGaze, new EyeTrackingVector3() },
            {ETData.RightEyePosition, new EyeTrackingVector3() },
            {ETData.RightEyeRotation, new EyeTrackingVector3() },
            {ETData.RightGaze, new EyeTrackingVector3() },
            {ETData.HeadDirection, new EyeTrackingVector3() },
            {ETData.CenterEyePosition, new EyeTrackingVector3() },
            {ETData.AverageGaze, new EyeTrackingVector3() },
            {ETData.IsGazingAtSomething, new EyeTrackingBool() },
            {ETData.FirstIntersectionPoint, new EyeTrackingVector3() },
            {ETData.GazedObjectID, new EyeTrackingInt() },
            {ETData.GazedObjectName, new EyeTrackingString() },
            {ETData.HasEyeTrackingTags, new EyeTrackingBool() },
            {ETData.EyeTrackingTagsList, new EyeTrackingStringList() }
        };

        }
        public new Type GetType() { return typeof(EyeTrackingTemplate); }
        public void Write(BinaryWriter writer) { }
        public IEyeTracking Read(BinaryReader reader) { return new EyeTrackingTemplate(); }
    }
}