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
using Newtonsoft.Json.Linq;

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

            } 
            catch (MobileServicePushFailedException ex)
            {
                if (ex.PushResult != null) {
                    foreach (var result in ex.PushResult.Errors)
                    {
                        await ResolveError (result);
                    }
                }
            }
            catch (Exception ex) {
                // TODO: handle error
                Debug.WriteLine ("Got exception: {0}", ex.Message);
            }
        }

        async Task ResolveError (MobileServiceTableOperationError result)
        {
            // Ignore if we can't see both sides.
            if (result.Result == null || result.Item == null)
                return;

            var serverItem = result.Result.ToObject<SurveyResponse> ();
            var localItem = result.Item.ToObject<SurveyResponse> ();

            if (serverItem.Name == localItem.Name
                && serverItem.ResponseIndex == localItem.ResponseIndex
                && serverItem.SurveyQuestionId == localItem.SurveyQuestionId) 
            {
                // Items are the same, so ignore the conflict
                await result.CancelAndDiscardItemAsync ();
            } 
            else 
            {
                // Always take the client
                localItem.AzureVersion = serverItem.AzureVersion;
                await result.UpdateOperationAsync (JObject.FromObject(localItem));
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
                .OrderByDescending(r => r.UpdatedAt)
                .Take(100).ToEnumerableAsync();
        }
    }
}
