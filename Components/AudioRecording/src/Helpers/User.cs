namespace SAAC.AudioRecording
{
    public class User
    {
        public int id;
        public Microphone microphone;
        public User(int i, Microphone mic)
        {
            id = i;
            microphone = mic;
        }
    }
}