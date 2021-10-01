namespace UVOCBot.Plugins.Planetside
{
    public static class EventStreamConstants
    {
        public const string CENSUS_EVENTSTREAM_CLIENT_NAME = "main";

        /// <summary>
        /// Pushed when a facility has changed hands.
        /// </summary>
        public const string FACILITY_CONTROL_EVENT = "FacilityControl";

        /// <summary>
        /// Pushed when a metagame event changes state.
        /// </summary>
        public const string METAGAME_EVENT_EVENT = "MetagameEvent";
    }
}
