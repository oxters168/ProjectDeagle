namespace ProjectDeagle
{
    public struct DemoHeader
    {
        public string header; //8 characters, should be "HL2DEMO"+NULL
        public int demoProtocol; //Demo protocol version
        public int networkProtocol; //Network protocol version number
        public string serverName; //260 characters long
        public string clientName; //260 characters long
        public string mapName; //260 characters long
        public string gameDirectory; //260 characters long
        public float playbackTime; //The length of the demo, in seconds
        public int ticks; //The number of ticks in the demo
        public int frames; //The number of frames in the demo
        public int signOnLength; //Length of the signon data(Init for first frame)
    }
}