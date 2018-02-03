using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Web;

using FileManager.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using Orchard.Logging;
using FileManager.Services;
using FileManager.ViewModels;
using FileManager.Helpers;

using Orchard.UI.Notify;
using Orchard.Utility.Extensions;
using Orchard.Themes;
using Orchard.UI.Admin;
using Orchard;
using Orchard.Users.Models;
using Orchard.Roles.Services;
using Orchard.Data;
using Orchard.Roles.Models;

namespace FileManager.Drivers
{
    public class FileManagerDriver : ContentPartDriver<FileManagerPart>
    {
        private readonly IFilesService _fileService;
        private readonly IGroupsService _groupService;
        private readonly IFileManagerService _fileManagerService;
        private readonly ISettingsService _settingsService;
        private readonly IOrchardServices _orchardServices;
        private readonly IRoleService _roleService;
        private readonly IRepository<UserRolesPartRecord> _userRolesRepository;
        private readonly IPermissionService _permissionService;
        
        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public FileManagerDriver(IOrchardServices services, IFilesService fileService, IGroupsService groupService,
            IFileManagerService fileManagerService, ISettingsService settingsService, IOrchardServices orchardServices,
            IRoleService roleService, IRepository<UserRolesPartRecord> userRolesRepository, IPermissionService permissionService)
        {
            Services = services;
            _fileService = fileService;
            _groupService = groupService;
            _fileManagerService = fileManagerService;
            _settingsService = settingsService;
            _orchardServices = orchardServices;
            _roleService = roleService;
            _userRolesRepository = userRolesRepository;
            _permissionService = permissionService;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }



        protected override DriverResult Display(FileManagerPart part, string displayType, dynamic shapeHelper)
        {
            var model = BuildDisplayViewModel(part);
            if (model.Groups == null) model.Groups = new List<List<GroupRecord>>();
            return ContentShape("Parts_Files", () => shapeHelper.Parts_Files(GroupRoot: model.GroupRoot,
                Groups: model.Groups, Files: model.Files, SelectedPath: model.SelectedPath, ShowType: part.ShowType,
                HideGroups: part.HideGroups, HideFilterPanel: part.HideFilterPanel, SelectedOrder: model.SelectedOrder, SearchText: model.SearchText,
                SelectedGroup: model.SelectedGroup, AccessDenied: model.AccessDenied));
        }

        protected override void Exporting(FileManagerPart part, Orchard.ContentManagement.Handlers.ExportContentContext context)
        {
            base.Exporting(part, context);
        }

        protected override void Importing(FileManagerPart part, Orchard.ContentManagement.Handlers.ImportContentContext context)
        {
            base.Importing(part, context);
        }

        //GET
        protected override DriverResult Editor(FileManagerPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_Files_Edit",
                () => shapeHelper.EditorTemplate(TemplateName: "Parts/Files", Model: BuildEditorViewModel(part), Prefix: Prefix));
        }
        //POST
        protected override DriverResult Editor(FileManagerPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            var model = new EditModuleViewModel();
            GroupRecord groupRoot = new GroupRecord();
            updater.TryUpdateModel(model, Prefix, null, null);
            if (model.selectedGroup.HasValue)
                groupRoot = _groupService.GetGroup((int)model.selectedGroup);
            if (part.ContentItem.Id != 0 && groupRoot != null)
            {
                _fileManagerService.UpdateModule(part.ContentItem.Id, groupRoot, model.selectedShowType ?? (int)DisplayType.inline,
                                            model.HideGroups, model.HideFilterPanel);
            }

            foreach (var item in model.GroupList)
            {
                if (item.Value == model.selectedGroup.ToString())
                    item.Selected = true;
            }

            foreach (var item in model.ShowTypes)
            {
                if (item.Value == model.selectedShowType.ToString())
                    item.Selected = true;
            }

            return ContentShape("Parts_Files_Edit",
                                () => shapeHelper.EditorTemplate(TemplateName: "Parts/Files", Model: model, Prefix: Prefix));
        }



        private EditModuleViewModel BuildEditorViewModel(FileManagerPart part)
        {
            int? selectedGroup = null;
            int selectedType = (int)DisplayType.inline;

            if (part.ParentGroup != null)
                selectedGroup = part.ParentGroup.Id;
            if (part.ShowType.HasValue)
                selectedType = (int)part.ShowType;

            return new EditModuleViewModel
            {
                GroupList = _groupService.GetTreeListItem(selectedGroup),
                selectedGroup = null,
                HideFilterPanel = part.HideFilterPanel ?? false,
                HideGroups = part.HideGroups ?? false,
                selectedShowType = null,
                ShowTypes = FilesGroupsHelpers.GetShowTypesList(selectedType)
            };
        }

        private DisplayModuleViewModel BuildDisplayViewModel(FileManagerPart part)
        {
            int? sgid = null;
            if (!part.ParentGroup.Active) return new DisplayModuleViewModel();
            try
            {
                var sgidParam = HttpContext.Current.Request.QueryString["sgid"];
                if (sgidParam != null)
                {
                    sgid = Convert.ToInt32(sgidParam);
                }
            }
            catch { sgid = null; }

            var sortAscParam = HttpContext.Current.Request.QueryString[FilesGroupsHelpers.DisplaySortUrlParam];
            var sortTypeParam = HttpContext.Current.Request.QueryString[FilesGroupsHelpers.DisplaySortTypeUrlParam];
            var searchText = HttpContext.Current.Request.QueryString[FilesGroupsHelpers.DisplaySearchTextUrlParam];

            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = FilesGroupsHelpers.TtrimName(searchText);
                searchText = searchText.ToLower();
            }

            var user = Services.WorkContext.CurrentUser;

            List<int> rolesActUsrId = new List<int>();
            bool isSuperAdministrator = false;
            if (user != null)
            {
                rolesActUsrId.Add(_roleService.GetRoleByName("Authenticated").Id);

                if (string.Equals(user.UserName, Services.WorkContext.CurrentSite.SuperUser, StringComparison.OrdinalIgnoreCase))
                    isSuperAdministrator = true;
                else
                {
                    var rolesIds = _userRolesRepository.Table.Where(x => x.UserId == user.Id).Select(x => x.Role.Id).ToList();
                    if (rolesIds != null && rolesIds.Count > 0)
                        rolesActUsrId = rolesActUsrId.Concat(rolesIds).Distinct().ToList();
                }
            }
            else 
            {
                rolesActUsrId.Add(_roleService.GetRoleByName("Anonymous").Id);
            }

            var ret = new DisplayModuleViewModel();
            ret.SearchText = "";
            ret.AccessDenied = false;
            ret.SelectedOrder = new SelectedOrder();
            ret.GroupRoot = part.ParentGroup;
            var selectedGroup = sgid.HasValue ? _groupService.GetGroup(sgid.Value) : part.ParentGroup;
            if (selectedGroup == null || !selectedGroup.Active || selectedGroup.Wlft < part.ParentGroup.Wlft || selectedGroup.Wrgt > part.ParentGroup.Wrgt)
                selectedGroup = part.ParentGroup;

            var parent = selectedGroup;
            List<GroupRecord> breadcrumbs = new List<GroupRecord>();
            if (parent != null)
            {
                var breadcrumbsQ = new List<GroupRecord>();
                while (parent != null)
                {
                    breadcrumbsQ.Add(parent);
                    parent = parent.Parent;
                }
                breadcrumbs = breadcrumbsQ.OrderBy(x => x.Level).ToList();
            }

            string path = "";
            foreach (var item in breadcrumbs)
            {
                path = Path.Combine(path, item.Name);
            }

            if (string.IsNullOrEmpty(searchText))
            {
                ret.Files = _fileService.GetFiles().Where(x => x.Groups.Any(y => y.GroupRecord.Id == selectedGroup.Id && y.GroupRecord.Active) && x.Active)
                    .Select(x => new FileDisplayEntry() { File = x, Path = _fileService.FilePublicPath(Path.Combine(path, x.Name)), FileType = _settingsService.GetFileType(x.Ext) }).ToList();
            }
            else
            {
                var selFiles = _fileService.GetFiles().Where(x => x.Groups.Any(y => y.GroupRecord.Wlft >= selectedGroup.Wlft && y.GroupRecord.Wrgt <= selectedGroup.Wrgt && y.GroupRecord.Active) && x.Active).ToList();

                if (!string.IsNullOrEmpty(searchText) && selFiles != null)
                {
                    string separator = " ";
                    var splitSearchText = searchText.Split(separator.ToArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in splitSearchText)
                    {
                        selFiles = selFiles.Where(x => (!string.IsNullOrEmpty(x.Name) && x.Name.ToLower().Contains(item)) || (!string.IsNullOrEmpty(x.Description) && x.Description.ToLower().Contains(item))).ToList();
                    }
                    ret.SearchText = searchText;

                    if (selFiles != null)
                    {
                        ret.Files = selFiles
                            .Select(x => new FileDisplayEntry()
                            {
                                File = x,
                                Path = _fileService.FilePublicPath(Path.Combine(_groupService.GetGroupPath(x.Groups.FirstOrDefault().GroupRecord), x.Name)),
                                FileType = _settingsService.GetFileType(x.Ext),
                                SearchParams = new SearchFileParams() { Gid = x.Groups.Where(y => y.GroupRecord.Wlft >= selectedGroup.Wlft && y.GroupRecord.Wrgt <= selectedGroup.Wrgt && y.GroupRecord.Active).FirstOrDefault().GroupRecord.Id, PathToCurentRoot = PathToCurentRootFolder(part.ParentGroup, x.Groups.Where(y => y.GroupRecord.Wlft >= selectedGroup.Wlft && y.GroupRecord.Wrgt <= selectedGroup.Wrgt && y.GroupRecord.Active).FirstOrDefault().GroupRecord) },
                            }).ToList();
                    }
                    else
                    {
                        ret.Files = new List<FileDisplayEntry>();
                    }
                }  
            }

            if (ret.Files == null) ret.Files = new List<FileDisplayEntry>();

            ret.Files = ret.Files.OrderBy(x => x.File.Name).ToList();
            var groupsList = new List<List<GroupRecord>>();
            var groups = _groupService.GetGroups();
            ret.SelectedPath = new List<int>();


            parent = selectedGroup;
            while (true)
            {
                ret.SelectedPath.Add(parent.Id);
                var groupLevel = groups.Where(x => x.Parent == parent && x.Active).ToList();
                if (groupLevel != null || groupLevel.Count < 1)
                    groupsList.Insert(0, groupLevel);
                if (parent.Parent == null || parent == part.ParentGroup) break;
                parent = parent.Parent;
            }
            ret.Groups = groupsList;

            

            

            if (!string.IsNullOrEmpty(sortAscParam) && string.IsNullOrEmpty(sortTypeParam))
            {
                if (sortAscParam == "true")
                    ret.Files = ret.Files.OrderBy(x => x.File.Name).ToList();
                else
                    ret.Files = ret.Files.OrderByDescending(x => x.File.Name).ToList();
            }
            if (!string.IsNullOrEmpty(sortTypeParam))
            {
                switch (sortTypeParam)
                {
                    case "Name":
                        ret.SelectedOrder.Name = true;
                        if (!string.IsNullOrEmpty(sortAscParam) && sortAscParam == "false")
                            ret.Files = ret.Files.OrderByDescending(x => x.File.Name).ToList();
                        else
                            ret.Files = ret.Files.OrderBy(x => x.File.Name).ToList();
                        break;
                    case "UpdateDate":
                        ret.SelectedOrder.UpdateDate = true;
                        if (!string.IsNullOrEmpty(sortAscParam) && sortAscParam == "false")
                            ret.Files = ret.Files.OrderByDescending(x => x.File.UpdateDate).ToList();
                        else
                            ret.Files = ret.Files.OrderBy(x => x.File.UpdateDate).ToList();
                        break;
                    case "CreateDate":
                        ret.SelectedOrder.CreateDate = true;
                        if (!string.IsNullOrEmpty(sortAscParam) && sortAscParam == "false")
                            ret.Files = ret.Files.OrderByDescending(x => x.File.CreateDate).ToList();
                        else
                            ret.Files = ret.Files.OrderBy(x => x.File.CreateDate).ToList();
                        break;
                    case "Size":
                        ret.SelectedOrder.Size = true;
                        if (!string.IsNullOrEmpty(sortAscParam) && sortAscParam == "false")
                            ret.Files = ret.Files.OrderByDescending(x => x.File.Size).ToList();
                        else
                            ret.Files = ret.Files.OrderBy(x => x.File.Size).ToList();
                        break;
                    default: 
                        if (!string.IsNullOrEmpty(sortAscParam) && sortAscParam == "false")
                            ret.Files = ret.Files.OrderByDescending(x => x.File.Name).ToList();
                        else
                            ret.Files = ret.Files.OrderBy(x => x.File.Name).ToList();
                        break;
                }
            }

            ret.SelectedGroup = selectedGroup;

            if (!isSuperAdministrator) 
            {
                var rootPerms = _permissionService.GetPermissionsForGroup(ret.GroupRoot.Id);
                if (rootPerms != null && rootPerms.Count > 0 && !rootPerms.Any(x => rolesActUsrId.Contains(x)))
                {
                    ret.AccessDenied = true;
                    ret.Files = new List<FileDisplayEntry>();
                    ret.Groups = new List<List<GroupRecord>>();
                    ret.SelectedGroup = null;
                }

                if (ret.Files != null && ret.Files.Count > 0)
                {

                    var fileIds = ret.Files.Select(x => x.File.Id).ToList();
                    var permFiles = _permissionService.GetPermissions().Where(x => fileIds.Contains(x.RecordId) && x.SystemType == (int)PermisionRecordType.FileRecordType).ToList();


                    for (int i = (ret.Files.Count - 1); i >= 0; i--)
                    {

                        int parentGroupId = ret.Files[i].File.Groups.FirstOrDefault().GroupRecord.Id;
                        var parGroupRoles = _permissionService.GetPermissionsForGroup(parentGroupId);
                        if (permFiles != null && permFiles.Count > 0 && permFiles.Any(x => x.RecordId == ret.Files[i].File.Id))
                        {
                            if (!permFiles.Any(x => x.RecordId == ret.Files[i].File.Id && rolesActUsrId.Contains(x.RoleId)))
                            {
                                ret.Files.RemoveAt(i);
                                continue;
                            }
                        }
                        if (parGroupRoles != null && parGroupRoles.Count > 0 && parGroupRoles.Any(x => !rolesActUsrId.Contains(x)))
                        {
                            ret.Files.RemoveAt(i);
                            continue;
                        }
                    }

                }

                if (ret.Groups != null && ret.Groups.Count > 0)
                {
                    for (int i = (ret.Groups.Count - 1); i >= 0 ; i--)
                    {
                        for (int j = (ret.Groups[i].Count - 1); j >= 0 ; j--)
                        {
                            var groupRoles = _permissionService.GetPermissionsForGroup(ret.Groups[i][j].Id);
                            if (groupRoles != null && groupRoles.Count > 0 && !groupRoles.Any(x => rolesActUsrId.Contains(x)))
                            {
                                if (ret.Groups[i][j].Id == selectedGroup.Id)
                                {
                                    ret.SelectedGroup = null;
                                    ret.AccessDenied = true;
                                }

                                ret.Groups[i].RemoveAt(j);
                                continue;
                            }
                        }
                        if (ret.Groups[i].Count == 0)
                        {
                            ret.Groups.RemoveAt(i);
                            continue;
                        }
                    }
                }
            }
                
            return ret;
        }

        private string PathToCurentRootFolder(GroupRecord root, GroupRecord selectedgroup)
        {
            string ret = "";
            var curent = selectedgroup;
            while (root.Id != curent.Id && curent != null)
            {
                ret = "/" + curent.Name + ret;
                curent = curent.Parent;
            }
            return "/" + root.Name + ret;
        }
    }
}
