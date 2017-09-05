namespace CastMyVote.Models
{
    public class SurveyQuestion
    {
        public string Id { get; set; }
        public string Question { get; set; }
        public string Answers { get; set; }

        public override string ToString()
        {
            return Question;
        }
    }
}
