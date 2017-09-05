using Newtonsoft.Json;

namespace CastMyVote.Models
{
    [JsonObject(Title = "questions")]
    public class SurveyQuestion
    {
        public string Id { get; set; }
        [JsonProperty("text")]
        public string Question { get; set; }
        public string Answers { get; set; }

        public override string ToString()
        {
            return Question;
        }
    }
}
