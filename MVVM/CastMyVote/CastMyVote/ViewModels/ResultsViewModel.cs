using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CastMyVote.Interfaces;
using CastMyVote.Models;
using XamarinUniversity.Infrastructure;

namespace CastMyVote.ViewModels
{
    public class OneResultViewModel
    {
        public string Name { get; set; }
        public string Answer { get; set; }
    }

    public class ResultsViewModel
    {
        public SurveyQuestion Question { get; private set; }
        public IList<OneResultViewModel> Results { get; private set; }

        public ResultsViewModel(ISurveyQuestionService service, SurveyQuestion selectedQuestion)
        {
            Question = selectedQuestion;
            Results = new ObservableCollection<OneResultViewModel> ();
            GatherResults (service, selectedQuestion).IgnoreResult ();
        }

        private async Task GatherResults(ISurveyQuestionService service, SurveyQuestion question)
        {
            string[] answers = question.Answers.Split ('|');
            var responses = await service.GetResponsesForSurveyAsync(question.Id);
            foreach (var r in responses)
            {
                Results.Add (new OneResultViewModel {
                    Name = r.Name,
                    Answer = answers [r.ResponseIndex]
                });
            }
        }
    }
}
