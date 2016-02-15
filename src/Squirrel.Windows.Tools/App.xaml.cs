using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Squirrel.Windows.Tools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        App()
        {
            Locator.CurrentMutable.Register(() => new ReleaseEntryView(), typeof(IViewFor<ReleaseEntryViewModel>));
            Locator.CurrentMutable.RegisterConstant(new ReleaseEntryActionToStringConverter(), typeof(IBindingTypeConverter));
        }
    }
}
