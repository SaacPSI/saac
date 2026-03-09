using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CASPERAnalysis
{
    /// <summary>
    /// Represents information about a topic/stream
    /// </summary>
    public class TopicInfo : INotifyPropertyChanged
    {
        private string topicName;
        private string topicType;
        private long messageCount;
        private bool isAvailable;
        private bool isAnalyzed;

        public event PropertyChangedEventHandler PropertyChanged;

        public string TopicName
        {
            get => topicName;
            set => SetProperty(ref topicName, value);
        }

        public string TopicType
        {
            get => topicType;
            set => SetProperty(ref topicType, value);
        }

        public long MessageCount
        {
            get => messageCount;
            set => SetProperty(ref messageCount, value);
        }

        public bool IsAvailable
        {
            get => isAvailable;
            set => SetProperty(ref isAvailable, value);
        }

        public bool IsAnalyzed
        {
            get => isAnalyzed;
            set => SetProperty(ref isAnalyzed, value);
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

