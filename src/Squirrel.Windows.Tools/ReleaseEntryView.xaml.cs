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
using System.Reactive.Linq;

namespace Squirrel.Windows.Tools
{
    public class ReleaseEntryActionToStringConverter : IBindingTypeConverter
    {
        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            if (fromType == typeof(ReleaseEntryActions) && toType == typeof(object)) return 100;
            if (fromType == typeof(object) && toType == typeof(ReleaseEntryActions)) return 100;

            return 0;
        }

        public bool TryConvert(object from, Type toType, object conversionHint, out object result)
        {
            if (toType == typeof(ReleaseEntryActions)) {
                var str = from as string;
                if (String.IsNullOrWhiteSpace(str)) {
                    result = ReleaseEntryActions.None;
                    return true;
                }

                result = Enum.Parse(typeof(ReleaseEntryActions), str.Replace(" ", ""));
                return true;
            } else {
                var actions = (ReleaseEntryActions)from;
                if (actions == ReleaseEntryActions.None) {
                    result = "";
                    return true;
                }

                result = Enum.GetName(typeof(ReleaseEntryActions), actions)
                    .Replace("InstallAndPause", "Install and Pause");
                return true;
            }
        }
    }

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

            this.WhenAnyValue(x => x.Actions.SelectedValue)
                .BindTo(this, x => x.ViewModel.CurrentAction);
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
