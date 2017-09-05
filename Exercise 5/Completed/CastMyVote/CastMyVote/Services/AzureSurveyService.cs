using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CastMyVote.Interfaces;
using CastMyVote.Models;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Plugin.Connectivity;
using System.Diagnostics;

namespace CastMyVote.Services
{
    public class AzureSurveyService : ISurveyQuestionService
    {
        const string AzureUrl = "https://xamu-voter.azurewebsites.net";
        MobileServiceClient client;
        IMobileServiceSyncTable<SurveyQuestion> questionsTable;
        IMobileServiceSyncTable<SurveyResponse> responseTable;
        string lastQuestionId;

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

            if (CrossConnectivity.Current.IsConnected) {
                try
                {
                    await client.SyncContext.PushAsync ();
                    await questionsTable.PullAsync (
                        "allQuestions", questionsTable.CreateQuery ());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine ("Got exception: {0}", ex.Message);
                }
            }
        }

        async Task SynchronizeResponsesAsync(string questionId)
        {
            if (!CrossConnectivity.Current.IsConnected)
                return;

            try {

                await responseTable.PullAsync ("syncResponses"+questionId, 
                                                  responseTable.Where (r => r.SurveyQuestionId == questionId));

            } catch (Exception ex) {
                // TODO: handle error
                Debug.WriteLine ("Got exception: {0}", ex.Message);
            }
        }

        public async Task AddOrUpdateSurveyResponseAsync (SurveyResponse response)
        {
            await InitializeAsync ();
            if (string.IsNullOrEmpty(response.Id)) {
                await responseTable.InsertAsync (response);
            }

            await responseTable.UpdateAsync (response);
            await SynchronizeResponsesAsync (response.SurveyQuestionId);
        }

        public async Task DeleteSurveyResponseAsync (SurveyResponse response)
        {
            await InitializeAsync ();
            await responseTable.DeleteAsync(response);
            await SynchronizeResponsesAsync (response.SurveyQuestionId);
        }

        public async Task<IEnumerable<SurveyQuestion>> GetQuestionsAsync ()
        {
            await InitializeAsync ();
            return await questionsTable.ReadAsync ();
        }

        public async Task<SurveyResponse> GetResponseForSurveyAsync (string questionId, string name)
        {
            await InitializeAsync ();

            if (lastQuestionId != questionId) {
                // Get the latest responses for this question.
                await SynchronizeResponsesAsync (questionId);
                lastQuestionId = questionId;
            }

            return (await responseTable.Where (
                r => r.SurveyQuestionId == questionId && r.Name == name)
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
