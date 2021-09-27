namespace UVOCBot.Plugins.Planetside
{
    public record PlanetsidePluginOptions
    {
        /// <summary>
        /// Gets or sets the endpoint at which the fisu's API can be found.
        /// </summary>
        public string FisuApiEndpoint { get; init; }

        /// <summary>
        /// Gets or sets the key used to connect to Daybreak Game's Census API.
        /// </summary>
        public string CensusApiKey { get; init; }

        public PlanetsidePluginOptions()
        {
            FisuApiEndpoint = "https://ps2.fisu.pw/api";
            CensusApiKey = "example";
        }
    }
}
