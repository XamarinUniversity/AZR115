using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CastMyVote.Interfaces;
using CastMyVote.Models;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using Microsoft.WindowsAzure.MobileServices.Sync;

namespace CastMyVote.Services
{
    public class AzureSurveyService : ISurveyQuestionService
    {
        const string AzureUrl = "https://xamu-voter.azurewebsites.net/";
        MobileServiceClient client;
        IMobileServiceSyncTable<SurveyQuestion> questionsTable;
        IMobileServiceSyncTable<SurveyResponse> responseTable;

        async Task InitializeAsync()
        {
            if (client != null)
                return;

            var store = new MobileServiceSQLiteStore ("survey.db");
            store.DefineTable<SurveyQuestion> ();
            store.DefineTable<SurveyResponse> ();

            client = new MobileServiceClient (AzureUrl);
            await client.SyncContext.InitializeAsync (store, new MobileServiceSyncHandler ());

            questionsTable = client.GetSyncTable<SurveyQuestion> ();
            responseTable = client.GetSyncTable<SurveyResponse> ();
        }

        public async Task AddOrUpdateSurveyResponseAsync (SurveyResponse response)
        {
            await InitializeAsync ();
            if (string.IsNullOrEmpty(response.Id)) {
                await responseTable.InsertAsync (response);
            }
            await responseTable.UpdateAsync (response);
        }

        public async Task DeleteSurveyResponseAsync (SurveyResponse response)
        {
            await InitializeAsync ();
            await responseTable.DeleteAsync (response);
        }

        public async Task<IEnumerable<SurveyQuestion>> GetQuestionsAsync ()
        {
            await InitializeAsync ();
            return await questionsTable.ReadAsync ();
        }

        public async Task<SurveyResponse> GetResponseForSurveyAsync (string questionId, string name)
        {
            await InitializeAsync ();
            return (await responseTable.Where (r => r.SurveyQuestionId == questionId && r.Name == name)
                    .ToEnumerableAsync ()).FirstOrDefault ();
        }

        public async Task<IEnumerable<SurveyResponse>> GetResponsesForSurveyAsync (string questionId)
        {
            await InitializeAsync ();
            return await responseTable
                .Where(r => r.SurveyQuestionId == questionId)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(100).ToEnumerableAsync();
        }
    }
}
