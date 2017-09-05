using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CastMyVote.Interfaces;
using CastMyVote.Models;
using Microsoft.WindowsAzure.MobileServices;

namespace CastMyVote.Services
{
    public class AzureSurveyService : ISurveyQuestionService
    {
        const string AzureUrl = "https://xamu-voter.azurewebsites.net";
        MobileServiceClient client;

        void Initialize()
        {
            if (client != null)
                return;

            client = new MobileServiceClient (AzureUrl);
        }

        public Task AddOrUpdateSurveyResponseAsync (SurveyResponse response)
        {
            Initialize ();
            throw new NotImplementedException ();
        }

        public Task DeleteSurveyResponseAsync (SurveyResponse response)
        {
            Initialize ();
            throw new NotImplementedException ();
        }

        public Task<IEnumerable<SurveyQuestion>> GetQuestionsAsync ()
        {
            Initialize ();
            throw new NotImplementedException ();
        }

        public Task<SurveyResponse> GetResponseForSurveyAsync (string questionId, string name)
        {
            Initialize ();
            throw new NotImplementedException ();
        }

        public Task<IEnumerable<SurveyResponse>> GetResponsesForSurveyAsync (string questionId)
        {
            Initialize ();
            throw new NotImplementedException ();
        }
    }
}
