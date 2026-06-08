namespace MapRestart;

public partial class MapRestart
{
    public class Config
    {
        public bool DetailedLogging { get; set; } = false;

        public int MapRestartThresholdMinutes { get; set; } = 60;
    }
}
