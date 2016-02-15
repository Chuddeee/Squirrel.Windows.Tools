using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reflection;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Squirrel.Windows.Tools
{
    public class MainWindowViewModel : ReactiveObject
    {
        public ReactiveCommand<List<ReleaseEntry>> CheckRemoteUpdateInfo { get; private set; }

        public ReactiveCommand<Unit> DoIt { get; private set; }

        public ReactiveList<ReleaseEntryViewModel> ReleasesList { get; private set; }

        string releaseLocation;
        public string ReleaseLocation {
            get { return releaseLocation; }
            set { this.RaiseAndSetIfChanged(ref releaseLocation, value); }
        }

        string releasesListHint;
        public string ReleasesListHint {
            get { return releasesListHint; }
            set { this.RaiseAndSetIfChanged(ref releasesListHint, value); }
        }


        public MainWindowViewModel()
        {
            ReleasesListHint = "Type in a release location URL or path to files";
            ReleasesList = new ReactiveList<ReleaseEntryViewModel>();

            CheckRemoteUpdateInfo = ReactiveCommand.CreateAsyncTask(
                this.WhenAny(x => x.ReleaseLocation, x => !String.IsNullOrWhiteSpace(x.Value)),
                async _ => {
                    ReleasesListHint = "";

                    var releaseData = default(String);
                    if (Regex.IsMatch(ReleaseLocation, "^https?://", RegexOptions.IgnoreCase)) {
                        var wc = new WebClient();
                        releaseData = await wc.DownloadStringTaskAsync(ReleaseLocation + "/RELEASES");
                    } else {
                        releaseData = File.ReadAllText(releaseData, Encoding.UTF8);
                    }

                    var ret = releaseData.Split('\n')
                        .Select(x => ReleaseEntry.ParseReleaseEntry(x))
                        .ToList();

                    return ret;
                });

            CheckRemoteUpdateInfo.ThrownExceptions
                .Subscribe(ex => ReleasesListHint = "Failed to check for updates: " + ex.Message);

            CheckRemoteUpdateInfo
                .Subscribe(x => {
                    var vms = x.Select(y => new ReleaseEntryViewModel(y));
                    using (ReleasesList.SuppressChangeNotifications()) {
                        ReleasesList.Clear();
                        ReleasesList.AddRange(vms);
                    }
                });

            this.WhenAnyValue(x => x.ReleaseLocation)
                .Throttle(TimeSpan.FromMilliseconds(750), RxApp.MainThreadScheduler)
                .InvokeCommand(CheckRemoteUpdateInfo);

            DoIt = ReactiveCommand.CreateAsyncTask(
                this.WhenAny(x => x.ReleasesList, x => x.Value != null && x.Value.Count > 0),
                async _ => { });
        }
    }
    public enum ReleaseEntryActions {
        None = 0,
        Start,
        Install,
        Skip,
        InstallAndPause,
        End
    }

    public class ReleaseEntryViewModel : ReactiveObject
    {
        public ReleaseEntry Model { get; private set; }

        public string VersionString { get; private set; }

        ReleaseEntryActions currentAction;
        public ReleaseEntryActions CurrentAction {
            get { return currentAction; }
            set { this.RaiseAndSetIfChanged(ref currentAction, value); }
        }

        public ReleaseEntryViewModel(ReleaseEntry model)
        {
            this.Model = model;
            var name = model.Filename.Split('-')[0];

            VersionString = String.Format("{0} {1} ({2})", 
                name, model.Version, model.IsDelta ? "Delta" : "Full");
        }
    }
}
