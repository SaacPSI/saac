// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

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
        public int Content;

        public EyeTrackingInt()
        {
            this.Content = 0;
        }

        public EyeTrackingInt(int i)
        {
            this.Content = i;
        }

        public new Type GetType()
        {
            return typeof(EyeTrackingInt);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.Content);
        }

        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingInt(reader.ReadInt32());
        }

        public override string ToString()
        {
            return this.Content.ToString();
        }

        public bool Compare(EyeTrackingInt other)
        {
            return this.Content == other.Content;
        }
    }

    public class EyeTrackingBool : IEyeTracking
    {
        public bool Content;

        public EyeTrackingBool()
        {
            this.Content = false;
        }

        public EyeTrackingBool(bool b)
        {
            this.Content = b;
        }

        public new Type GetType()
        {
            return typeof(EyeTrackingBool);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.Content);
        }

        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingBool(reader.ReadBoolean());
        }

        public override string ToString()
        {
            return this.Content.ToString();
        }

        public bool Compare(EyeTrackingBool other)
        {
            return this.Content == other.Content;
        }
    }

    public class EyeTrackingString : IEyeTracking
    {
        public string Content;

        public EyeTrackingString()
        {
            this.Content = string.Empty;
        }

        public EyeTrackingString(string s)
        {
            this.Content = s;
        }

        public new Type GetType()
        {
            return typeof(EyeTrackingString);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.Content);
        }

        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingString(reader.ReadString());
        }

        public override string ToString()
        {
            return this.Content;
        }

        public bool Compare(EyeTrackingString other)
        {
            return this.Content == other.Content;
        }
    }

    public class EyeTrackingVector3 : IEyeTracking
    {
        public System.Numerics.Vector3 Content;

        public EyeTrackingVector3()
        {
            this.Content.X = 0;
            this.Content.Y = 0;
            this.Content.Z = 0;
        }

        public EyeTrackingVector3(float a, float b, float c)
        {
            this.Content.X = a;
            this.Content.Y = b;
            this.Content.Z = c;
        }

        public new Type GetType()
        {
            return typeof(EyeTrackingVector3);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((double)this.Content.X);
            writer.Write((double)this.Content.Y);
            writer.Write((double)this.Content.Z);
        }

        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingVector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble());
        }

        public override string ToString()
        {
            return this.Content.ToString();
        }

        public bool Compare(EyeTrackingVector3 other)
        {
            return this.Content == other.Content;
        }
    }

    public class EyeTrackingStringList : IEyeTracking
    {
        public List<string> Content;

        public EyeTrackingStringList()
        {
            this.Content = new List<string>();
        }

        public EyeTrackingStringList(List<string> l)
        {
            this.Content = l;
        }

        public new Type GetType()
        {
            return typeof(EyeTrackingStringList);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.Content.Count());
            foreach (string s in this.Content)
            {
                writer.Write(s);
            }
        }

        public IEyeTracking Read(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<string> l = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                l.Add(reader.ReadString());
            }
            return new EyeTrackingStringList(l);
        }

        public override string ToString()
        {
            string l = string.Empty;
            foreach (string s in this.Content)
            {
                l += s + " ; ";
            }
            return l;
        }

        public bool Compare(EyeTrackingStringList other)
        {
            return this.Content == other.Content;
        }
    }

    public class EyeTrackingTemplate : IEyeTracking
    {
        public Dictionary<ETData, IEyeTracking> Content;

        public EyeTrackingTemplate()
        {
            this.Content = new Dictionary<ETData, IEyeTracking>()
        {
            { ETData.LeftEyePosition, new EyeTrackingVector3() },
            { ETData.LeftEyeRotation, new EyeTrackingVector3() },
            { ETData.LeftGaze, new EyeTrackingVector3() },
            { ETData.RightEyePosition, new EyeTrackingVector3() },
            { ETData.RightEyeRotation, new EyeTrackingVector3() },
            { ETData.RightGaze, new EyeTrackingVector3() },
            { ETData.HeadDirection, new EyeTrackingVector3() },
            { ETData.CenterEyePosition, new EyeTrackingVector3() },
            { ETData.AverageGaze, new EyeTrackingVector3() },
            { ETData.IsGazingAtSomething, new EyeTrackingBool() },
            { ETData.FirstIntersectionPoint, new EyeTrackingVector3() },
            { ETData.GazedObjectID, new EyeTrackingInt() },
            { ETData.GazedObjectName, new EyeTrackingString() },
            { ETData.HasEyeTrackingTags, new EyeTrackingBool() },
            { ETData.EyeTrackingTagsList, new EyeTrackingStringList() }
        };
        }

        public new Type GetType()
        {
            return typeof(EyeTrackingTemplate);
        }

        public void Write(BinaryWriter writer)
        {
        }

        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingTemplate();
        }
    }
}
