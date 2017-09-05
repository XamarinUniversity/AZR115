using System.Collections.Generic;
using System.Threading.Tasks;
using CastMyVote.Models;

namespace CastMyVote.Interfaces
{
    public interface ISurveyQuestionService
    {
        Task AddOrUpdateSurveyResponseAsync(SurveyResponse response);
        Task DeleteSurveyResponseAsync(SurveyResponse response);
        Task<IEnumerable<SurveyQuestion>> GetQuestionsAsync();
        Task<IEnumerable<SurveyResponse>> GetResponsesForSurveyAsync(string questionId);
        Task<SurveyResponse> GetResponseForSurveyAsync(string questionId, string name);
    }
}