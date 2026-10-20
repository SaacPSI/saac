using System;
using System.Collections.Generic;
using Microsoft.Psi.Data;
using Microsoft.Psi.Serialization;
using SAAC.PsiFormats;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Timing information published by the task server with every task status.
    /// Same fields, in the same order, as SerializableClass.TaskInfo: the stores recorded
    /// with that assembly are read with this type (see <see cref="LegacyStoreTypes"/>).
    /// </summary>
    public class TaskInfo
    {
        public float timeRemaining;
        public float distance;
        public float speed;
    }

    /// <summary>
    /// State of a puzzle, published when a puzzle is completed (currentPiecesNumber is the
    /// number of pieces used) and again when the next one is presented (currentPiecesNumber
    /// is 0). Mirror of SerializableClass.PuzzleStatus.
    /// </summary>
    public class PuzzleStatus : TaskInfo
    {
        public bool status;
        public int puzzleID;
        public int optimalPiecesNumber;
        public int currentPiecesNumber;
        public List<string> activeTopPoint;
        public List<string> activeBottomPoint;
    }

    /// <summary>Kinds of action on a piece generator. Mirror of SerializableClass.GenInteraction.</summary>
    public enum GenInteraction
    {
        ChangePage = 1,
        Spawnrequest = 2,
        CancelRequest = 3,
    }

    /// <summary>Action of a participant on a piece generator. Mirror of SerializableClass.GeneratorInteraction.</summary>
    public class GeneratorInteraction : IDs
    {
        public GenInteraction interactionType;
        public int generatorID;
    }

    /// <summary>
    /// Reads the stores recorded with the SerializableClass assembly (studies recorded before
    /// these types moved to SAAC) with the SAAC types.
    ///
    /// A \psi store keeps the assembly qualified name of the type of each stream. Opening a
    /// stream as a type of another assembly only works once that name has been mapped onto
    /// the new type, for the type of the stream and for every type it contains:
    ///
    /// <code>
    /// var importer = PsiStore.Open(pipeline, "Individual Task Logs", path).WithLegacyTypes();
    /// var pieces = importer.OpenStream&lt;PieceStatus&gt;("Quest1-PieceState");
    /// </code>
    ///
    /// The mapping must be registered before the first stream is opened. It relies on the
    /// SAAC types having the same fields as the recorded ones.
    /// </summary>
    public static class LegacyStoreTypes
    {
        /// <summary>Assembly the legacy types were recorded with.</summary>
        public const string LegacyAssembly = "SerializableClass, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        /// <summary>
        /// The SAAC types that replace a type of the same name of the legacy assembly: the types
        /// of the raw streams of a session. The types of the streams computed by the legacy
        /// pipeline are not in the list: the SAAC versions of TTData, TimeData and
        /// SpeakingTimeIDData renamed their fields and cannot read the recorded ones.
        /// </summary>
        public static readonly IReadOnlyList<Type> Types = new[]
        {
            typeof(IDs), typeof(State), typeof(Location), typeof(ObjectGazeEvent), typeof(PieceStatus),
            typeof(TaskInfo), typeof(PuzzleStatus), typeof(GenInteraction), typeof(GeneratorInteraction),
        };

        /// <summary>Maps the legacy type names onto the SAAC types for one store.</summary>
        public static void Register(KnownSerializers serializers)
        {
            if (serializers == null)
            {
                throw new ArgumentNullException(nameof(serializers));
            }

            foreach (Type type in Types)
            {
                serializers.Register(type, $"SerializableClass.{type.Name}, {LegacyAssembly}");
            }
        }

        /// <summary>Maps the legacy type names onto the SAAC types for this store, and returns the importer.</summary>
        public static Importer WithLegacyTypes(this Importer importer)
        {
            Register(importer.Serializers);
            return importer;
        }
    }
}
