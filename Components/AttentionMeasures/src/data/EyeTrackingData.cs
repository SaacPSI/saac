// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.IO;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Interface for eye tracking data types.
    /// </summary>
    public interface IEyeTracking
    {
        /// <summary>
        /// Enumeration of eye tracking data types.
        /// </summary>
        public enum ETData
        {
            /// <summary>Left eye position.</summary>
            LeftEyePosition,

            /// <summary>Left eye rotation.</summary>
            LeftEyeRotation,

            /// <summary>Left gaze direction.</summary>
            LeftGaze,

            /// <summary>Right eye position.</summary>
            RightEyePosition,

            /// <summary>Right eye rotation.</summary>
            RightEyeRotation,

            /// <summary>Right gaze direction.</summary>
            RightGaze,

            /// <summary>Head direction.</summary>
            HeadDirection,

            /// <summary>Center eye position.</summary>
            CenterEyePosition,

            /// <summary>Average gaze direction.</summary>
            AverageGaze,

            /// <summary>Indicates if gazing at something.</summary>
            IsGazingAtSomething,

            /// <summary>First intersection point.</summary>
            FirstIntersectionPoint,

            /// <summary>Gazed object ID.</summary>
            GazedObjectID,

            /// <summary>Gazed object name.</summary>
            GazedObjectName,

            /// <summary>Indicates if has eye tracking tags.</summary>
            HasEyeTrackingTags,

            /// <summary>Eye tracking tags list.</summary>
            EyeTrackingTagsList
        }

        /// <summary>
        /// Gets the type of the eye tracking data.
        /// </summary>
        /// <returns>The type.</returns>
        public Type GetType();

        /// <summary>
        /// Writes the eye tracking data to a binary writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        public void Write(BinaryWriter writer);

        /// <summary>
        /// Reads the eye tracking data from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns>The read eye tracking data.</returns>
        public IEyeTracking Read(BinaryReader reader);
    }

    /// <summary>
    /// Eye tracking data containing an integer value.
    /// </summary>
    public class EyeTrackingInt : IEyeTracking
    {
        /// <summary>
        /// Gets or sets the integer content.
        /// </summary>
        public int Content { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingInt"/> class.
        /// </summary>
        public EyeTrackingInt()
        {
            this.Content = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingInt"/> class.
        /// </summary>
        /// <param name="i">The integer value.</param>
        public EyeTrackingInt(int i)
        {
            this.Content = i;
        }

        /// <inheritdoc/>
        public new Type GetType()
        {
            return typeof(EyeTrackingInt);
        }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            writer.Write(this.Content);
        }

        /// <inheritdoc/>
        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingInt(reader.ReadInt32());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Content.ToString();
        }

        /// <summary>
        /// Compares this instance with another EyeTrackingInt.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns>True if equal; otherwise false.</returns>
        public bool Compare(EyeTrackingInt other)
        {
            return this.Content == other.Content;
        }
    }

    /// <summary>
    /// Eye tracking data containing a boolean value.
    /// </summary>
    public class EyeTrackingBool : IEyeTracking
    {
        /// <summary>
        /// Gets or sets the boolean content.
        /// </summary>
        public bool Content { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingBool"/> class.
        /// </summary>
        public EyeTrackingBool()
        {
            this.Content = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingBool"/> class.
        /// </summary>
        /// <param name="b">The boolean value.</param>
        public EyeTrackingBool(bool b)
        {
            this.Content = b;
        }

        /// <inheritdoc/>
        public new Type GetType()
        {
            return typeof(EyeTrackingBool);
        }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            writer.Write(this.Content);
        }

        /// <inheritdoc/>
        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingBool(reader.ReadBoolean());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Content.ToString();
        }

        /// <summary>
        /// Compares this instance with another EyeTrackingBool.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns>True if equal; otherwise false.</returns>
        public bool Compare(EyeTrackingBool other)
        {
            return this.Content == other.Content;
        }
    }

    /// <summary>
    /// Eye tracking data containing a string value.
    /// </summary>
    public class EyeTrackingString : IEyeTracking
    {
        /// <summary>
        /// Gets or sets the string content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingString"/> class.
        /// </summary>
        public EyeTrackingString()
        {
            this.Content = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingString"/> class.
        /// </summary>
        /// <param name="s">The string value.</param>
        public EyeTrackingString(string s)
        {
            this.Content = s;
        }

        /// <inheritdoc/>
        public new Type GetType()
        {
            return typeof(EyeTrackingString);
        }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            writer.Write(this.Content);
        }

        /// <inheritdoc/>
        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingString(reader.ReadString());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Content;
        }

        /// <summary>
        /// Compares this instance with another EyeTrackingString.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns>True if equal; otherwise false.</returns>
        public bool Compare(EyeTrackingString other)
        {
            return this.Content == other.Content;
        }
    }

    /// <summary>
    /// Eye tracking data containing a Vector3 value.
    /// </summary>
    public class EyeTrackingVector3 : IEyeTracking
    {
        /// <summary>
        /// Gets or sets the Vector3 content.
        /// </summary>
        public System.Numerics.Vector3 Content { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingVector3"/> class.
        /// </summary>
        public EyeTrackingVector3()
        {
            this.Content = new System.Numerics.Vector3(0, 0, 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingVector3"/> class.
        /// </summary>
        /// <param name="a">The X component.</param>
        /// <param name="b">The Y component.</param>
        /// <param name="c">The Z component.</param>
        public EyeTrackingVector3(float a, float b, float c)
        {
            this.Content = new System.Numerics.Vector3(a, b, c);
        }

        /// <inheritdoc/>
        public new Type GetType()
        {
            return typeof(EyeTrackingVector3);
        }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            writer.Write((double)this.Content.X);
            writer.Write((double)this.Content.Y);
            writer.Write((double)this.Content.Z);
        }

        /// <inheritdoc/>
        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingVector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Content.ToString();
        }

        /// <summary>
        /// Compares this instance with another EyeTrackingVector3.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns>True if equal; otherwise false.</returns>
        public bool Compare(EyeTrackingVector3 other)
        {
            return this.Content == other.Content;
        }
    }

    /// <summary>
    /// Eye tracking data containing a list of strings.
    /// </summary>
    public class EyeTrackingStringList : IEyeTracking
    {
        /// <summary>
        /// Gets or sets the list of strings content.
        /// </summary>
        public List<string> Content { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingStringList"/> class.
        /// </summary>
        public EyeTrackingStringList()
        {
            this.Content = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingStringList"/> class.
        /// </summary>
        /// <param name="l">The list of strings.</param>
        public EyeTrackingStringList(List<string> l)
        {
            this.Content = l;
        }

        /// <inheritdoc/>
        public new Type GetType()
        {
            return typeof(EyeTrackingStringList);
        }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            writer.Write(this.Content.Count());
            foreach (string s in this.Content)
            {
                writer.Write(s);
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override string ToString()
        {
            string l = string.Empty;
            foreach (string s in this.Content)
            {
                l += s + " ; ";
            }

            return l;
        }

        /// <summary>
        /// Compares this instance with another EyeTrackingStringList.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns>True if equal; otherwise false.</returns>
        public bool Compare(EyeTrackingStringList other)
        {
            return this.Content == other.Content;
        }
    }

    /// <summary>
    /// Eye tracking template containing a dictionary of eye tracking data.
    /// </summary>
    public class EyeTrackingTemplate : IEyeTracking
    {
        /// <summary>
        /// Gets or sets the dictionary of eye tracking data.
        /// </summary>
        public Dictionary<IEyeTracking.ETData, IEyeTracking> Content { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingTemplate"/> class.
        /// </summary>
        public EyeTrackingTemplate()
        {
            this.Content = new Dictionary<IEyeTracking.ETData, IEyeTracking>()
            {
                { IEyeTracking.ETData.LeftEyePosition, new EyeTrackingVector3() },
                { IEyeTracking.ETData.LeftEyeRotation, new EyeTrackingVector3() },
                { IEyeTracking.ETData.LeftGaze, new EyeTrackingVector3() },
                { IEyeTracking.ETData.RightEyePosition, new EyeTrackingVector3() },
                { IEyeTracking.ETData.RightEyeRotation, new EyeTrackingVector3() },
                { IEyeTracking.ETData.RightGaze, new EyeTrackingVector3() },
                { IEyeTracking.ETData.HeadDirection, new EyeTrackingVector3() },
                { IEyeTracking.ETData.CenterEyePosition, new EyeTrackingVector3() },
                { IEyeTracking.ETData.AverageGaze, new EyeTrackingVector3() },
                { IEyeTracking.ETData.IsGazingAtSomething, new EyeTrackingBool() },
                { IEyeTracking.ETData.FirstIntersectionPoint, new EyeTrackingVector3() },
                { IEyeTracking.ETData.GazedObjectID, new EyeTrackingInt() },
                { IEyeTracking.ETData.GazedObjectName, new EyeTrackingString() },
                { IEyeTracking.ETData.HasEyeTrackingTags, new EyeTrackingBool() },
                { IEyeTracking.ETData.EyeTrackingTagsList, new EyeTrackingStringList() }
            };
        }

        /// <inheritdoc/>
        public new Type GetType()
        {
            return typeof(EyeTrackingTemplate);
        }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
        }

        /// <inheritdoc/>
        public IEyeTracking Read(BinaryReader reader)
        {
            return new EyeTrackingTemplate();
        }
    }
}
