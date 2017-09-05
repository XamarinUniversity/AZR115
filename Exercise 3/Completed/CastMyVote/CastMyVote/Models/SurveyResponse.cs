using Newtonsoft.Json;
using System;
using Microsoft.WindowsAzure.MobileServices;

namespace CastMyVote.Models
{
    [JsonObject(Title="responses")]
    public class SurveyResponse
    {
        public string Id { get; set; }
        [JsonProperty("questionId")]
        public string SurveyQuestionId { get; set; }
        public string Name { get; set; }
        [JsonProperty ("answer")]
        public int ResponseIndex { get; set; }
        [UpdatedAt]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
