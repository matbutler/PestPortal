using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Orchard.Environment;
using Orchard.Environment.Extensions.Models;
using Orchard.Logging;

using FileManager.Services;
using FileManager.Models;
using Orchard.ContentManagement;
using System;

namespace FileManager
{
    [UsedImplicitly]
    public class DefaulFilesUpdater : IFeatureEventHandler {
        private readonly ISettingsService _settingsService;
        private readonly IGroupsService _groupService;
        private readonly IContentManager _contentManager;


        public DefaulFilesUpdater(
            IGroupsService groupService, IContentManager contentManager, ISettingsService settingsService)
        {
            _groupService = groupService;
            _settingsService = settingsService;
            _contentManager = contentManager;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        void IFeatureEventHandler.Installing(Feature feature) {
            AddDefaultGroupForFeature(feature);
        }

        void IFeatureEventHandler.Installed(Feature feature) {
        }

        void IFeatureEventHandler.Enabling(Feature feature) {
            _groupService.Actualize(GroupRoot);
            _settingsService.CreateDefaultSettings();
        }

        void IFeatureEventHandler.Enabled(Feature feature) {
           _groupService.RegroupTree();
        }

        void IFeatureEventHandler.Disabling(Feature feature) {
        }

        void IFeatureEventHandler.Disabled(Feature feature) {
        }

        void IFeatureEventHandler.Uninstalling(Feature feature) {
        }

        void IFeatureEventHandler.Uninstalled(Feature feature) {
            
        }

        public void AddDefaultGroupForFeature(Feature feature)
        {
            _groupService.CreateRootGroup();
            GroupRoot = _groupService.GetGroups().Where(x => x.Level == 0).FirstOrDefault();
        }

        public GroupRecord GroupRoot { get; set; }
    }
}
