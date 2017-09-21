using XamarinUniversity.Infrastructure;

namespace CastMyVote.ViewModels
{
    public class AnswerViewModel : SimpleViewModel
    {
        private bool isSelected;
        public string Text { get; private set; }
        public int Index { get; private set; }

        public AnswerViewModel(string choice, int index)
        {
            Text = choice;
            Index = index;
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { SetPropertyValue(ref isSelected, value); }
        }
    }
}