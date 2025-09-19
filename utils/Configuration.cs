using Newtonsoft.Json;

namespace Intellimix_Template.utils
{
    public class Configuration
    {
    }


    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class FilterRule
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("pattern", NullValueHandling = NullValueHandling.Ignore)]
        public string Pattern { get; set; }

        [JsonProperty("replacement", NullValueHandling = NullValueHandling.Ignore)]
        public string Replacement { get; set; }

        [JsonProperty("enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool Enabled { get; set; }
    }

    public class Logging
    {
        [JsonProperty("enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool Enabled { get; set; }

        [JsonProperty("serverUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerUrl { get; set; }

        [JsonProperty("apiKey", NullValueHandling = NullValueHandling.Ignore)]
        public string ApiKey { get; set; }

        [JsonProperty("encryptionKey", NullValueHandling = NullValueHandling.Ignore)]
        public string EncryptionKey { get; set; }

        [JsonProperty("logContent", NullValueHandling = NullValueHandling.Ignore)]
        public bool LogContent { get; set; }
    }

    public class Root
    {
        [JsonProperty("enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool Enabled { get; set; }

        [JsonProperty("replaceOriginalPaste", NullValueHandling = NullValueHandling.Ignore)]
        public bool ReplaceOriginalPaste { get; set; }

        [JsonProperty("customHotkey", NullValueHandling = NullValueHandling.Ignore)]
        public string CustomHotkey { get; set; }

        [JsonProperty("filterOnCopy", NullValueHandling = NullValueHandling.Ignore)]
        public bool FilterOnCopy { get; set; }

        [JsonProperty("localLog", NullValueHandling = NullValueHandling.Ignore)]
        public bool LocalLog { get; set; }

        [JsonProperty("filterOnlyForApps", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> FilterOnlyForApps { get; set; }

        [JsonProperty("logging", NullValueHandling = NullValueHandling.Ignore)]
        public Logging Logging { get; set; }

        [JsonProperty("filterRules", NullValueHandling = NullValueHandling.Ignore)]
        public List<FilterRule> FilterRules { get; set; }
    }


}
