using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Squirrel.Windows.Tools
{
    /// <summary>
    /// Interaction logic for ReleaseEntryView.xaml
    /// </summary>
    public partial class ReleaseEntryView : UserControl, IViewFor<ReleaseEntryViewModel>
    {
        public ReleaseEntryView()
        {
            InitializeComponent();

            this.OneWayBind(ViewModel, x => x.VersionString, x => x.VersionString.Text);

            var actions = new ReactiveList<string>(new string[] {
                "",
                "Start",
                "Install",
                "Skip",
                "Install and Wait",
                "End"
            });

            Actions.ItemsSource = actions;
        }

        public ReleaseEntryViewModel ViewModel {
            get { return (ReleaseEntryViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (ReleaseEntryViewModel)value; }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(ReleaseEntryViewModel), typeof(ReleaseEntryView), new PropertyMetadata(null));
    }
}
