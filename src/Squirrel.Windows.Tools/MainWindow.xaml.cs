using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewFor<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Bind(ViewModel, x => x.ReleaseLocation, x => x.ReleaseLocation.Text);
            this.OneWayBind(ViewModel, x => x.ReleaseLocation, x => x.ReleaseLocation.Text);
            this.OneWayBind(ViewModel, x => x.ReleasesListHint, x => x.ReleasesListHint.Content);

            this.WhenAny(x => x.ViewModel.ReleasesListHint, x => !String.IsNullOrWhiteSpace(x.Value))
                .BindTo(this, x => x.ReleasesListHint.Visibility);

            this.OneWayBind(ViewModel, x => x.ReleasesList, x => x.ReleasesList.ItemsSource);

            this.BindCommand(ViewModel, x => x.DoIt, x => x.DoIt);

            ViewModel = new MainWindowViewModel();

            UserError.RegisterHandler<YesNoUserError>(error => {
                var result = MessageBox.Show(error.ErrorCauseOrResolution, error.ErrorMessage, MessageBoxButton.YesNoCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes) return Observable.Return(RecoveryOptionResult.RetryOperation);
                if (result == MessageBoxResult.No) return Observable.Return(RecoveryOptionResult.FailOperation);

                return Observable.Return(RecoveryOptionResult.CancelOperation);
            });

            UserError.RegisterHandler<OkUserError>(error => {
                var result = MessageBox.Show(error.ErrorCauseOrResolution, error.ErrorMessage, MessageBoxButton.OK, MessageBoxImage.Error);
                return Observable.Return(RecoveryOptionResult.FailOperation);
            });
        }


        public MainWindowViewModel ViewModel {
            get { return (MainWindowViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(MainWindowViewModel), typeof(MainWindow), new PropertyMetadata(null));

        object IViewFor.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (MainWindowViewModel)value; }
        }
    }
}
