using System;
using Newtonsoft.Json;

public class TimeTrackingData
{
    [JsonProperty("url")]
    public string Url { get; set; }
    
    [JsonProperty("title")]
    public string Title { get; set; }
    
    [JsonProperty("icon")]
    public string Icon { get; set; }
    
    [JsonProperty("domain")]
    public string Domain { get; set; }
    
    [JsonProperty("startTime")]
    public string StartTime { get; set; }
    
    [JsonProperty("endTime")]
    public string EndTime { get; set; }
    
    [JsonProperty("duration")]
    public long Duration { get; set; } // 毫秒
    
    public DateTime StartTimeAsDateTime => DateTime.Parse(StartTime);
    public DateTime? EndTimeAsDateTime => string.IsNullOrEmpty(EndTime) ? null : DateTime.Parse(EndTime);
    public TimeSpan DurationAsTimeSpan => TimeSpan.FromMilliseconds(Duration);
}
