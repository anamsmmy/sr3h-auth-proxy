using System.Windows;

namespace MacroApp.Views
{
    public partial class InstructionsWindow : Window
    {
        public InstructionsWindow()
        {
            InitializeComponent();
            
            // تعيين الثقافة للأرقام الإنجليزية
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}