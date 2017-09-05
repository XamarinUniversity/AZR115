using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CastMyVote.Interfaces;
using CastMyVote.Models;
using Microsoft.WindowsAzure.MobileServices;

namespace CastMyVote.Services
{
    public class AzureSurveyService : ISurveyQuestionService
    {
        const string AzureUrl = "https://xamu-voter.azurewebsites.net/";
        MobileServiceClient client;
        IMobileServiceTable<SurveyQuestion> questionsTable;
        IMobileServiceTable<SurveyResponse> responseTable;

        void Initialize()
        {
            if (client != null)
                return;

            client = new MobileServiceClient (AzureUrl);
            questionsTable = client.GetTable<SurveyQuestion> ();
            responseTable = client.GetTable<SurveyResponse> ();
        }

        public Task AddOrUpdateSurveyResponseAsync (SurveyResponse response)
        {
            Initialize ();
            if (string.IsNullOrEmpty(response.Id)) {
                return responseTable.InsertAsync (response);
            }
            return responseTable.UpdateAsync (response);
        }

        public Task DeleteSurveyResponseAsync (SurveyResponse response)
        {
            Initialize ();
            return responseTable.DeleteAsync (response);
        }

        public Task<IEnumerable<SurveyQuestion>> GetQuestionsAsync ()
        {
            Initialize ();
            return questionsTable.ReadAsync ();
        }

        public async Task<SurveyResponse> GetResponseForSurveyAsync (string questionId, string name)
        {
            Initialize ();
            return (await responseTable.Where (r => r.SurveyQuestionId == questionId && r.Name == name)
                    .ToEnumerableAsync ()).FirstOrDefault ();

        }

        public async Task<IEnumerable<SurveyResponse>> GetResponsesForSurveyAsync (string questionId)
        {
            Initialize ();
            return await responseTable
                .Where(r => r.SurveyQuestionId == questionId)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(100).ToEnumerableAsync();
        }
    }
}
