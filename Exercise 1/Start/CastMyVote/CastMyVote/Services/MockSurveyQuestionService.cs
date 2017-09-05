using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CastMyVote.Interfaces;
using CastMyVote.Models;

namespace CastMyVote.Services
{
    public class MockSurveyQuestionService : ISurveyQuestionService
    {
        List<SurveyResponse> responses;
        List<SurveyQuestion> questions;

        async Task Initialize()
        {
            await Task.Delay(1000);

            if (questions != null)
                return;

            responses = new List<SurveyResponse>();

            questions = new List<SurveyQuestion>
            {
                new SurveyQuestion {Id = "1", Question = "Where should we have lunch?", Answers = "McDonalds|Sushi King|Olive Garden" },
                new SurveyQuestion {Id = "2", Question = "PARTY! Which date do you prefer?", Answers = "Sunday|Monday|Tuesday|Wednesday" }
            };
        }

        public async Task<IEnumerable<SurveyQuestion>> GetQuestionsAsync()
        {
            await Initialize();
            return await Task.FromResult<IEnumerable<SurveyQuestion>>(questions);
        }

        public async Task<IEnumerable<SurveyResponse>> GetResponsesForSurveyAsync(string questionId)
        {
            await Initialize();
            return await Task.FromResult(
                responses.Where(s => s.SurveyQuestionId == questionId));
        }

        public async Task<SurveyResponse> GetResponseForSurveyAsync(string questionId, string name)
        {
            await Initialize();
            return await Task.FromResult(
                responses.FirstOrDefault(s => s.SurveyQuestionId == questionId && s.Name == name));
        }

        public async Task AddOrUpdateSurveyResponseAsync(SurveyResponse response)
        {
            await Initialize();

            if (response.Id == null)
            {
                response.Id = new Guid ().ToString ();
            }

            var existing = responses.SingleOrDefault (s => s.SurveyQuestionId == response.SurveyQuestionId
                                                      && s.Name == response.Name);
            if (existing != null)
                responses.Remove (existing);

            responses.Add (response);
        }

        public async Task DeleteSurveyResponseAsync(SurveyResponse response)
        {
            await Initialize();
            responses.Remove(response);
        }
    }
}
