using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CastMyVote.Interfaces;
using CastMyVote.Models;
using System.Diagnostics;
using XamarinUniversity.Infrastructure;
using XamarinUniversity.Interfaces;

namespace CastMyVote.ViewModels
{
    /// <summary>
    /// Primary view model which drives the application.
    /// </summary>
    public class MainViewModel : SimpleViewModel
    {
        /// <summary>
        /// Command to refresh the questions
        /// </summary>
        public ICommand RefreshQuestions
        {
            get {
                return refreshQuestionsCommand ??
                       (refreshQuestionsCommand = new AsyncDelegateCommand(() => questions.RefreshAsync()));
            }
        }

        /// <summary>
        /// Command to select an answer for a specific question
        /// </summary>
        public ICommand SelectAnswer
        {
            get
            {
                return selectAnswerCommand ??
                       (selectAnswerCommand = new AsyncDelegateCommand<AnswerViewModel>(OnSelectAnswer, _ => !string.IsNullOrEmpty(Name)));
            }
        }

        /// <summary>
        /// Command to remove an answer and set it back to "unanswered"
        /// </summary>
        public ICommand RemoveAnswer
        {
            get
            {
                return deleteAnswerCommand ??
                       (deleteAnswerCommand = new AsyncDelegateCommand(OnDeleteAnswer));
            }
        }

        /// <summary>
        /// Command to navigate to the results view
        /// </summary>
        public ICommand ShowSurveyResults
        {
            get
            {
                return showSurveyResultsCommand ??
                       (showSurveyResultsCommand = new AsyncDelegateCommand(OnShowSurveyResults));
            }
        }

        /// <summary>
        /// Client's name
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (SetPropertyValue(ref name, value))
                {
                    OnNameChanged().IgnoreResult();
                }
            }
        }

        /// <summary>
        /// List of questions
        /// </summary>
        public IList<SurveyQuestion> Questions
        {
            get
            {
                questions.RefreshAsync().IgnoreResult(ex =>
                {
                    dependencyService.Get<IMessageVisualizerService>()
                        .ShowMessage("Error", "Failed to retrieve questions from server: " + ex.Message, "OK");
                });
                return questions;
            }
        }

        /// <summary>
        /// List of possible answers for the selected question
        /// </summary>
        public IList<AnswerViewModel> Answers
        {
            get { return answers; }
        }

        /// <summary>
        /// The currently selected question presented to the user.
        /// </summary>
        public SurveyQuestion SelectedQuestion
        {
            get { return selectedQuestion; }
            set
            {
                if (SetPropertyValue(ref selectedQuestion, value))
                {
                    // When the question changes, load our answers + results.
                    answers.RefreshAsync().IgnoreResult();
                }
            }
        }

        /// <summary>
        /// True if the user has typed in their name.
        /// </summary>
        public bool HasName
        {
            get { return !string.IsNullOrEmpty(Name); }
        }

        /// <summary>
        /// True if we are loading data from the server.
        /// </summary>
        public bool IsLoadingData
        {
            get { return isLoadingData; }
            set { SetPropertyValue(ref isLoadingData, value); }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="surveyService">Service to retrieve data from</param>
        /// <param name="dependencyService">Service Locator</param>
        public MainViewModel(ISurveyQuestionService surveyService, IDependencyService dependencyService)
        {
            this.surveyService = surveyService;
            this.dependencyService = dependencyService;

            // Setup our collection of questions. This is an async collection populated by our 
            // survey service. The single constructor parameter is an async function to load the data.
            questions = new RefreshingCollection<SurveyQuestion>(surveyService.GetQuestionsAsync)
            {
                // What to do if the refresh fails..
                RefreshFailed = async (s, ex) =>
                {
                    IsLoadingData = false;
                    await Task.Delay (1000); // get out of the binding.
                    await dependencyService.Get<IMessageVisualizerService>()
                        .ShowMessage("Error", "Failed to get questions: " + ex.Message, "OK");
                },

                // Called before a refresh occurs to save off the current selection.
                BeforeRefresh = s =>
                {
                    IsLoadingData = true;
                    return SelectedQuestion;
                },

                // Called after a refresh completes successfully; restores the selection.
                AfterRefresh = (s, sq) =>
                {
                    IsLoadingData = false;
                    if (sq == null)
                        sq = questions.FirstOrDefault();
                    if (sq != null)
                    {
                        var newSelection = questions.SingleOrDefault(q => q.Id == ((SurveyQuestion)sq).Id);
                        SelectedQuestion = newSelection;
                    }
                }
            };

            // Setup the collection of answers; this is refreshed each time the
            // selected question changes.
            answers = new RefreshingCollection<AnswerViewModel>(async () =>
            {
                IsLoadingData = true;

                try
                {
                    var id = SelectedQuestion.Id;
                    string [] choices = SelectedQuestion.Answers.Split ('|');

                    if (HasName)
                        response = await surveyService.GetResponseForSurveyAsync(id, Name);
                    else
                        response = null;

                    int answerIndex = response == null ? -1 : response.ResponseIndex;

                    return Enumerable.Range (0, choices.Length)
                         .Select (i => new AnswerViewModel (choices [i], i) {
                             IsSelected = answerIndex == i 
                    });
                }
                catch (Exception ex)
                {
                    await dependencyService.Get<IMessageVisualizerService>()
                        .ShowMessage("Error", "Failed to get answers/responses: " + ex.Message, "OK");
                    return null;
                }
                finally
                {
                    IsLoadingData = false;
                }
            });
        }

        private async Task OnNameChanged()
        {
            selectAnswerCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(() => HasName);
            if (HasName)
            {
                try
                {
                    response = await surveyService.GetResponseForSurveyAsync(SelectedQuestion.Id, Name);
                }
                catch (Exception ex)
                {
                    await dependencyService.Get<IMessageVisualizerService>()
                        .ShowMessage("Error", "Failed to get survey responses: " + ex.Message, "OK");
                }
            }
            else
                response = null;
            UpdateAnswerSelection();
        }

        private async Task OnSelectAnswer(AnswerViewModel answer)
        {
            if (string.IsNullOrEmpty(Name) || answer.IsSelected)
                return;

            try
            {
                if (response == null) {
                    response = new SurveyResponse {
                        Name = this.Name,
                        ResponseIndex = answer.Index,
                        SurveyQuestionId = SelectedQuestion.Id
                    };
                }
                else {
                    Debug.Assert (response.Name == Name);
                    Debug.Assert (response.SurveyQuestionId == SelectedQuestion.Id);
                    response.ResponseIndex = answer.Index;
                }
                await surveyService.AddOrUpdateSurveyResponseAsync (response);
                UpdateAnswerSelection();
            }
            catch (Exception ex)
            {
                await dependencyService.Get<IMessageVisualizerService>()
                    .ShowMessage("Error", "Failed to update your answer: " + ex.Message, "OK");
            }
        }

        private void UpdateAnswerSelection()
        {
            // Remove any outdated data.
            foreach (var entry in answers.Where(a => a.IsSelected))
                entry.IsSelected = false;

            // Select the proper answer
            if (response != null && response.ResponseIndex >= 0)
                answers [response.ResponseIndex].IsSelected = true;
        }

        private async Task OnShowSurveyResults()
        {
            await dependencyService.Get<INavigationService>()
                .NavigateAsync(AppPages.Responses,
                    new ResultsViewModel(surveyService, SelectedQuestion));
        }

        private async Task OnDeleteAnswer()
        {
            if (response != null)
            {
                await surveyService.DeleteSurveyResponseAsync(response);
                response = null;
                UpdateAnswerSelection();
            }
        }

        #region Private Data

        private string name;
        private readonly ISurveyQuestionService surveyService;
        private readonly IDependencyService dependencyService;

        private readonly RefreshingCollection<SurveyQuestion> questions;
        private SurveyResponse response;
        private readonly RefreshingCollection<AnswerViewModel> answers;
        private SurveyQuestion selectedQuestion;

        private AsyncDelegateCommand refreshQuestionsCommand;
        private AsyncDelegateCommand<AnswerViewModel> selectAnswerCommand;
        private AsyncDelegateCommand deleteAnswerCommand;
        private AsyncDelegateCommand showSurveyResultsCommand;
        private bool isLoadingData;

        #endregion
    }
}
