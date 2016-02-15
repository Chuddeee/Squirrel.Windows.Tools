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
            ReleasesList.ChangeTrackingEnabled = true;

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
                    var vms = x
                        .Where(y => y.IsDelta == false)
                        .Select(y => new ReleaseEntryViewModel(y));

                    using (ReleasesList.SuppressChangeNotifications()) {
                        ReleasesList.Clear();
                        ReleasesList.AddRange(vms);
                    }
                });

            this.WhenAnyValue(x => x.ReleaseLocation)
                .Throttle(TimeSpan.FromMilliseconds(750), RxApp.MainThreadScheduler)
                .InvokeCommand(CheckRemoteUpdateInfo);

            ReleasesList.ItemChanged
                .Where(x => x.PropertyName == "CurrentAction")
                .Subscribe(x => updateStartsAndEnds(x.Sender));

            DoIt = ReactiveCommand.CreateAsyncTask(
                this.WhenAny(x => x.ReleasesList.Count, x => x.Value > 0),
                async _ => {
                    var releasesToApply = ReleasesList.Where(x => x.Enabled).ToList();
                    if (releasesToApply.Count < 1 || releasesToApply.All(x => x.CurrentAction == ReleaseEntryActions.None)) {
                        await UserError.Throw(new OkUserError("Nothing to do", "At least one release must have the 'Start' action"));
                        return;
                    }

                    var appName = ReleasesList[0].Name;
                    var rootAppDir = Environment.ExpandEnvironmentVariables("%LocalAppData%\\" + appName);

                    if (Directory.Exists(rootAppDir)) {
                        var result = await UserError.Throw(new YesNoUserError(
                            "App already installed",
                            String.Format("App '{0}' is already installed, remove it before running install?", appName)));

                        if (result == RecoveryOptionResult.CancelOperation) {
                            return;
                        }

                        if (result == RecoveryOptionResult.RetryOperation) {
                            using (var mgr = new UpdateManager(null, appName, Environment.ExpandEnvironmentVariables("%LocalAppData%"))) {
                                await mgr.FullUninstall();
                            }
                        }
                    }

                    foreach (var release in releasesToApply) {
                        release.ReleaseLocation = this.ReleaseLocation;
                        await release.ApplyThisRelease.ExecuteAsync(null);
                    }
                });
        }

        void updateStartsAndEnds(ReleaseEntryViewModel changedObject)
        {
            // First scan for invalid scenarios
            int startIdx = -1;
            int endIdx = -1;
            int currentIdx = -1;

            for (int i=0; i < ReleasesList.Count; i++) {
                var current = ReleasesList[i];
                if (changedObject == current) currentIdx = i;

                if (current.CurrentAction == ReleaseEntryActions.Start) {
                    if (startIdx >= 0) {
                        ReleasesList[startIdx].CurrentAction = ReleaseEntryActions.None;
                        startIdx = i;
                    }

                    startIdx = i;
                    continue;
                }

                if (current.CurrentAction == ReleaseEntryActions.End) {
                    if (endIdx >= 0) {
                        current.CurrentAction = ReleaseEntryActions.None;
                    } else {
                        endIdx = i;
                    }

                    endIdx = i;
                    continue;
                }
            }

            if (endIdx < startIdx && endIdx >= 0) goto bogus;

            foreach (var v in ReleasesList) { v.Enabled = true; }

            if (startIdx >= 0) {
                for (int i=0; i < startIdx; i++) { ReleasesList[i].Enabled = false; }
            }

            if (endIdx >= 0) {
                for (int i=endIdx+1; i < ReleasesList.Count; i++) { ReleasesList[i].Enabled = false; }
            }

            if (startIdx >=0 || endIdx >= 0) {
                foreach (var v in ReleasesList) {
                    if (!v.Enabled) continue;
                    if (v.CurrentAction != ReleaseEntryActions.None) continue;

                    v.CurrentAction = ReleaseEntryActions.Install;
                }
            }

            return;

        bogus:
            foreach (var v in ReleasesList) { v.Enabled = true; }
        }
    }

    public class YesNoUserError : UserError
    {
        public YesNoUserError(string errorMessage, string errorCauseOrResolution) : base(errorMessage, errorCauseOrResolution) { }
    }

    public class OkUserError : UserError
    {
        public OkUserError(string errorMessage, string errorCauseOrResolution) : base(errorMessage, errorCauseOrResolution) { }
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

        public string Name { get; private set; }

        public string ReleaseLocation { get; set; }

        bool enabled;
        public bool Enabled {
            get { return enabled; }
            set { this.RaiseAndSetIfChanged(ref enabled, value); }
        }

        public ReactiveCommand<Unit> ApplyThisRelease { get; protected set; }

        ReleaseEntryActions currentAction;
        public ReleaseEntryActions CurrentAction {
            get { return currentAction; }
            set { this.RaiseAndSetIfChanged(ref currentAction, value); }
        }

        public ReleaseEntryViewModel(ReleaseEntry model)
        {
            this.Model = model;
            Enabled = true;

            Name = model.Filename.Split('-')[0];
            VersionString = String.Format("{0} {1}", Name, model.Version);

            this.WhenAnyValue(x => x.Enabled)
                .Where(x => x == false)
                .Subscribe(x => CurrentAction = ReleaseEntryActions.None);

            ApplyThisRelease = ReactiveCommand.CreateAsyncTask(async _ => {
            });
        }
    }
}
