namespace ProjectDeagle
{
    public class TeamResource
    {
        public Team team { get; internal set; }

        #region Strings
        public string teamName { get; internal set; } //m_szTeamname
        public string clanName { get; internal set; } //m_szClanTeamname
        public string flagImage { get; internal set; } //m_szTeamFlagImage
        //public string logoImage { get; internal set; } //m_szTeamLogoImage
        public string matchStat { get; internal set; } //m_szTeamMatchStat
        #endregion

        #region Integers
        public int teamNum { get; internal set; } //m_iTeamNum
        public int totalScore { get; internal set; } //m_scoreTotal
        public int firstHalfScore { get; internal set; } //m_scoreFirstHalf
        public int secondHalfScore { get; internal set; } //m_scoreSecondHalf
        public int clanID { get; internal set; } //m_iClanID
        #endregion

        #region Booleans
        public bool surrendered { get; internal set; } //m_bSurrendered
        #endregion

        public object[] player_array; //"player_array"

        internal TeamResource() { }
        internal TeamResource(TeamResource other)
        {
            team = other.team;

            #region Strings
            teamName = other.teamName;
            clanName = other.clanName;
            flagImage = other.flagImage;
            //logoImage = other.logoImage;
            matchStat = other.matchStat;
            #endregion

            #region Integers
            teamNum = other.teamNum;
            totalScore = other.totalScore;
            firstHalfScore = other.firstHalfScore;
            secondHalfScore = other.secondHalfScore;
            clanID = other.clanID;
            #endregion

            #region Booleans
            surrendered = other.surrendered;
            #endregion

            player_array = other.player_array;
        }
    }

    public enum Team
    {
        Unassigned,
        Spectator,
        Terrorist,
        CounterTerrorist,
    }
}
