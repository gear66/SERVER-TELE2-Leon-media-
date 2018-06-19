using Newtonsoft.Json;

public class Message
{
    [JsonProperty("payload")]
    public Payload payload { get; set; }

    [JsonProperty("command")]
    public Command command { get; set; }

    [JsonProperty("target")]
    public string target { get; set; }

}
