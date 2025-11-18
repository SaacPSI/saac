namespace SAAC.AudioRecording
{
    public class User
    {
        public string Id { get; private set; }
        public string Microphone { get; private set; }
        public int Channel { get; private set; }

        public User(string id, string microphone, int channel)
        {
            Id = id;
            Microphone = microphone;
            Channel = channel;
        }
    }
}