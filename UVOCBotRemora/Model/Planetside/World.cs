namespace UVOCBotRemora.Model.Planetside
{
    public record World
    {
        public WorldType WorldId { get; init; }
        public string State { get; init; }
        public TranslationProperty Name { get; init; }

        public World()
        {
            State = string.Empty;
            Name = new TranslationProperty();
        }
    }
}
