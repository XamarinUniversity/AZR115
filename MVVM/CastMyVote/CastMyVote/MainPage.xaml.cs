using Xamarin.Forms;

namespace CastMyVote
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnTurnOffSelection(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
                ((ListView) sender).SelectedItem = null;
        }
    }
}
