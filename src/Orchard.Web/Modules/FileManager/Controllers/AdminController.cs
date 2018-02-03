using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Routing;
using System;
using System.IO;

using Orchard.Core.Contents.Controllers;
using Orchard.Localization;
using Orchard.Logging;
using FileManager.Models;
using FileManager.Services;
using FileManager.ViewModels;
using FileManager.Helpers;

using Orchard.Users.Models;
using Orchard.UI.Notify;
using Orchard.Utility.Extensions;
using Orchard.Themes;
using Orchard.UI.Admin;
using Orchard.ContentManagement;
using Orchard;
using Orchard.DisplayManagement;
using Orchard.UI.Navigation;
using Orchard.Settings;
using System.Web.UI;
using System.Web;
using System.ComponentModel;
using System.Web.Security;
using Orchard.Mvc;
using System.Text;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using Orchard.Roles.Services;
using Orchard.Media;


namespace FileManager.Controllers
{

    [ValidateInput(false)]
    public class AdminController : Controller
    {
        private readonly IFilesService _fileService;
        private readonly IGroupsService _groupService;
        private readonly ISettingsService _settingsService;
        private readonly ISiteService _siteService;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }
        dynamic Shape { get; set; }
        private readonly string CookieNameFile = "FileManagerSelectedIdsFile";
        private readonly string CookieNameGroup = "FileManagerSelectedIdsGroup";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminController(IOrchardServices services, IFilesService fileService, IGroupsService groupService, 
            IShapeFactory shapeFactory, ISiteService siteService, ISettingsService settingsService, IRoleService roleService,
            IPermissionService permissionService, IHttpContextAccessor httpContextAccessor)
        {
            Services = services;
            _fileService = fileService;
            _groupService = groupService;
            _siteService = siteService;
            _settingsService = settingsService;
            _roleService = roleService;
            _permissionService = permissionService;
            _httpContextAccessor = httpContextAccessor;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
            Shape = shapeFactory;
        }

        #region Index

        public ActionResult Index(int? gid, bool? regroup, UserIndexOptions options, PagerParameters pagerParameters, bool? sel, bool? lcoo)
        {
            try
            {
                if (HttpContext.Request.RequestType == "POST")
                {
                    try
                    {
                        var viewModel = new FileGroupIndexViewModel
                        {
                            GroupRecords = new List<GroupEntry>(),
                            FileRecords = new List<FileEntry>(),
                            BulkAction = new FileGroupsAdminIndexBulkAction(),
                            BreadRecord = new List<GroupRecord>(),
                            Gid = 0,
                            DeselectAll = false,
                            SelectAll = false
                        };

                        if (TryUpdateModel(viewModel))
                        {
                            GetSelectedCookieValues(viewModel);
                        }
                        else
                        {
                            GetSelectedFromCookie();
                        }
                    }
                    catch
                    {
                        GetSelectedFromCookie();
                    }
                }
                else 
                {
                    GetSelectedFromCookie();
                }

                if ((!gid.HasValue && (pagerParameters == null || (pagerParameters.Page == null && pagerParameters.PageSize == null))) 
                    || (pagerParameters == null || (pagerParameters.Page == null && pagerParameters.PageSize == null)))
                {
                    if (string.IsNullOrEmpty(options.SearchText) && (lcoo.HasValue && lcoo.Value == false))
                    {
                        SelectedFileIds = new List<int>();
                        SelectedGroupIds = new List<int>();
                    }
                }

                var pager = new Pager(_siteService.GetSiteSettings(), pagerParameters);

                if (regroup.HasValue && regroup == true)
                    _groupService.RegroupTree();

                if (options == null)
                    options = new UserIndexOptions();

                IEnumerable<GroupRecord> groupsQ = _groupService.GetGroups();
                GroupRecord parent = null;

                if (gid.HasValue)
                    parent = groupsQ.Where(x => x.Id == gid).FirstOrDefault();

                if (!gid.HasValue || parent == null)
                    gid = groupsQ.OrderBy(x => x.Level).Select(y => y.Id).FirstOrDefault();

                parent = groupsQ.Where(x => x.Id == gid).FirstOrDefault();


                var breadcrumbsQ = new List<GroupRecord>();
                while (parent.Parent != null)
                {
                    breadcrumbsQ.Add(parent);
                    parent = parent.Parent;
                }

                var breadcrumbs = breadcrumbsQ.OrderBy(x => x.Level).ToList();
                var groupEntries = groupsQ.Where(x => x.Parent != null && x.Parent.Id == gid).Select(FilesGroupsHelpers.CreateGroupEntry).ToList();
                groupEntries = groupEntries.Where(x => x != null).ToList();

                IEnumerable<FileRecord> filesQ = _fileService.GetFiles().Where(x => x.Groups.Any(y => y.FileRecord.Id == x.Id && y.GroupRecord.Id == gid));
                var fileEntries = filesQ.Select(FilesGroupsHelpers.CreateFileEntry).ToList();
                fileEntries = fileEntries.Where(x => x != null).ToList();

                if (!String.IsNullOrWhiteSpace(options.SearchText))
                {
                    var groupRoot = groupsQ.Where(x => x.Id == gid).FirstOrDefault();
                    groupEntries = groupsQ.Where(x => x.Wlft > groupRoot.Wlft && x.Wrgt < groupRoot.Wrgt).Select(FilesGroupsHelpers.CreateGroupEntry).ToList();

                    fileEntries = _fileService.GetFiles().Where(x => x.Groups.Any(y => y.FileRecord.Id == x.Id 
                        &&  y.GroupRecord.Wlft >= groupRoot.Wlft && y.GroupRecord.Wrgt <= groupRoot.Wrgt))
                        .Select(FilesGroupsHelpers.CreateFileEntry).ToList();

                    
                    var searchArr = options.SearchText.ToLower().Split(' ').ToList();

                    foreach (var searchText in searchArr)
                    {
                        fileEntries = fileEntries.Where(x => x.File.Name != null 
                            && x.File.Name.ToLower().Contains(searchText)).ToList();
                        groupEntries = groupEntries.Where(x => x.Group.Name != null 
                            && x.Group.Name.ToLower().Contains(searchText)).ToList();
                    }

                                        
                    foreach (var item in fileEntries)
                    {
                        item.SearchParams = new SearchFileDisplayParams() { GroupName = PathToCurentRootFolder(item.File.Groups.FirstOrDefault().GroupRecord) };
                    }

                    foreach (var item in groupEntries)
                    {
                        item.SearchParams = new SearchFileDisplayParams() { GroupName = PathToCurentRootFolder(item.Group.Parent) };
                    }

                }

                foreach (var item in groupEntries)
                {
                    item.Size = _groupService.GetGroupSize(item.Group);
                }

                SelectedGroupIds = groupEntries.Select(x => x.Group.Id).ToList().Intersect(SelectedGroupIds).ToList();
                SelectedFileIds = fileEntries.Select(x => x.File.Id).ToList().Intersect(SelectedFileIds).ToList();

                var totalItemsCount = fileEntries.Count() + groupEntries.Count();
                var pagerShape = Shape.Pager(pager).TotalItemCount(totalItemsCount);
                
                
                switch (options.OrderBy)
                {
                    case FileGroupsAdminIndexOrderBy.Name:
                        if (options.OrderDesc)
                        {
                            fileEntries = fileEntries.OrderByDescending(x => x.File.Name).ToList();
                            groupEntries = groupEntries.OrderByDescending(x => x.Group.Name).ToList();
                        }
                        else
                        {
                            fileEntries = fileEntries.OrderBy(x => x.File.Name).ToList();
                            groupEntries = groupEntries.OrderBy(x => x.Group.Name).ToList();
                        }
                        break;
                    case FileGroupsAdminIndexOrderBy.CreateDate:
                        if (options.OrderDesc)
                        {
                            fileEntries = fileEntries.OrderByDescending(x => x.File.CreateDate).ToList();
                            groupEntries = groupEntries.OrderByDescending(x => x.Group.CreateDate).ToList();
                        }
                        else
                        {
                            fileEntries = fileEntries.OrderBy(x => x.File.CreateDate).ToList();
                            groupEntries = groupEntries.OrderBy(x => x.Group.CreateDate).ToList();
                        }
                        break;
                    case FileGroupsAdminIndexOrderBy.UpdateDate:
                        if (options.OrderDesc)
                        {
                            fileEntries = fileEntries.OrderByDescending(x => x.File.UpdateDate).ToList();
                            groupEntries = groupEntries.OrderByDescending(x => x.Group.UpdateDate).ToList();
                        }
                        else
                        {
                            fileEntries = fileEntries.OrderBy(x => x.File.UpdateDate).ToList();
                            groupEntries = groupEntries.OrderBy(x => x.Group.UpdateDate).ToList();
                        }
                        break;
                    case FileGroupsAdminIndexOrderBy.Size:
                        if (options.OrderDesc)
                        {
                            fileEntries = fileEntries.OrderByDescending(x => x.File.Size).ToList();
                            groupEntries = groupEntries.OrderByDescending(x => x.Group.Name).ToList();
                        }
                        else
                        {
                            fileEntries = fileEntries.OrderBy(x => x.File.Size).ToList();
                            groupEntries = groupEntries.OrderBy(x => x.Group.Name).ToList();
                        }
                        break;
                    default:
                        fileEntries = fileEntries.OrderBy(x => x.File.Name).ToList();
                        groupEntries = groupEntries.OrderBy(x => x.Group.Name).ToList();
                        break;
                }

                int take = pager.PageSize == 0 ? 999999999 : pager.PageSize;
                int startIndex = pager.GetStartIndex();

                foreach (var file in fileEntries)
                {
                    if (SelectedFileIds.Contains(file.File.Id))
                        file.IsChecked = true;
                    else
                        file.IsChecked = false;
                }

                foreach (var group in groupEntries)
                {
                    if (SelectedGroupIds.Contains(group.Group.Id))
                        group.IsChecked = true;
                    else
                        group.IsChecked = false;
                }

                if (startIndex > (groupEntries.Count() - 1))
                {
                    if (groupEntries.Count() > 0)
                        startIndex = startIndex - (groupEntries.Count());
                    if (startIndex < 0) startIndex = 0;
                    groupEntries = new List<GroupEntry>();
                }
                else
                {
                    groupEntries = groupEntries.Skip(startIndex).Take(take).ToList();
                    startIndex = 0;
                    take = take - groupEntries.Count();
                }
                fileEntries = fileEntries.Skip(startIndex).Take(take).ToList();
                
                var selectedAll = sel.HasValue ? sel.Value : false;

                var pasteGroupList = _groupService.GetTreeListItem(null);
                var model = new FileGroupIndexViewModel
                {
                    FileRecords = fileEntries,
                    GroupRecords = groupEntries,
                    BreadRecord = breadcrumbs,
                    Gid = (int)gid,
                    Options = options,
                    Pager = pagerShape,
                    PasteGroupList = pasteGroupList,
                    SelectAll = selectedAll,
                    TotalItemsCount = totalItemsCount
                };

                UpdateCookieValues();

                var routeData = new RouteData();
                routeData.Values.Add("Options.OrderBy", options.OrderBy);
                if (options.OrderDesc)
                    routeData.Values.Add("Options.OrderDesc", options.OrderDesc);
                routeData.Values.Add("Options.SearchText", options.SearchText);

                pagerShape.RouteData(routeData);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Load file index failed.");
                Services.Notifier.Error(T("Load file index failed: {0}.", ex));
            }
            
            return View(new FileGroupIndexViewModel());
        }

        [HttpPost]
        [FormValueRequired("submit.BulkEdit")]
        public ActionResult Index(FormCollection input)
        {
            var viewModel = new FileGroupIndexViewModel { GroupRecords = new List<GroupEntry>(), FileRecords = new List<FileEntry>(),  BulkAction = new FileGroupsAdminIndexBulkAction(), BreadRecord = new List<GroupRecord>(), Gid = 0 };
            var routeValues = new RouteValueDictionary();

            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't manage media folder")))
                return new HttpUnauthorizedResult();

            if (!TryUpdateModel(viewModel))
            {
                return View(viewModel);
            }
            try
            {
                routeValues.Add("gid", viewModel.Gid);
                routeValues.Add("lcoo", "true");
                if (!string.IsNullOrEmpty(viewModel.Options.SearchText))
                    routeValues.Add("Options.SearchText", viewModel.Options.SearchText);


                GetSelectedCookieValues(viewModel);

                bool regroup = false;

                switch (viewModel.BulkAction)
                {
                    case FileGroupsAdminIndexBulkAction.None:
                        break;
                    case FileGroupsAdminIndexBulkAction.Delete:

                        foreach (var entryGroup in SelectedGroupIds)
                        {
                            _groupService.DeleteGroup(entryGroup);
                        }

                        foreach (var entryFile in SelectedFileIds)
                        {
                            var groupId = _fileService.GetFile(entryFile).Groups.FirstOrDefault().GroupRecord.Id;
                            _fileService.DeleteFile(entryFile, groupId); //TOTO Ondro: vyber skupiny z ktorej zmazat
                        }
                        Services.Notifier.Information(T("Entries delete successfuly"));
                        break;
                    case FileGroupsAdminIndexBulkAction.Publish:

                        foreach (var entryGroup in SelectedGroupIds)
                        {
                            _groupService.ChangeActiveGroup(entryGroup, true);
                        }

                        foreach (var entryFile in SelectedFileIds)
                        {
                            _fileService.ChangeActiveFile(entryFile, true);
                        }

                        Services.Notifier.Information(T("Entries update successfuly"));
                        break;
                    case FileGroupsAdminIndexBulkAction.Unpublish:

                        foreach (var entryGroup in SelectedGroupIds)
                        {
                            _groupService.ChangeActiveGroup(entryGroup, false);
                        }

                        foreach (var entryFile in SelectedFileIds)
                        {
                            _fileService.ChangeActiveFile(entryFile, false);
                        }

                        Services.Notifier.Information(T("Entries update successfuly"));
                        break;
                    case FileGroupsAdminIndexBulkAction.Copy:

                        foreach (var entryGroup in SelectedGroupIds)
                        {
                            regroup = true;
                            var groupId = _groupService.GetGroup(entryGroup).Parent.Id;
                            _groupService.CopyGroup(entryGroup, groupId); // TODO Ondro: odkial kopirovat 
                        }

                        foreach (var entryFile in SelectedFileIds)
                        {
                            var groupId = _fileService.GetFile(entryFile).Groups.FirstOrDefault().GroupRecord.Id;
                            _fileService.CopyFile(entryFile, groupId, groupId); // TODO Ondro: odkial kopirovat 
                        }

                        Services.Notifier.Information(T("Entries copy successfuly"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                //if (regroup)
                //    _groupService.RegroupTree();
                
                routeValues.Add("regroup", true);

                UpdateCookieValues();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Entries update failed.");
                Services.Notifier.Error(T("Entries update failed: {0}.", ex));
            }

            return RedirectToAction("Index", routeValues);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submitCut")]
        public ActionResult IndexCut()
        {
            var viewModel = new FileGroupIndexViewModel { GroupRecords = new List<GroupEntry>(), FileRecords = new List<FileEntry>(), BulkAction = new FileGroupsAdminIndexBulkAction(), BreadRecord = new List<GroupRecord>(), Gid = 0 };
            var routeValues = new RouteValueDictionary();

            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't manage media folder")))
                return new HttpUnauthorizedResult();

            if (!TryUpdateModel(viewModel))
            {
                return View(viewModel);
            }
            try
            {
                routeValues.Add("gid", viewModel.Gid);
                
                GetSelectedCookieValues(viewModel);

                bool regroup = false;

                if (viewModel.CopySelected)
                {
                    foreach (var entryGroup in SelectedGroupIds)
                    {
                        regroup = true;
                        _groupService.CopyGroup(entryGroup, viewModel.SelectedPasteGroup.Value);
                    }

                    foreach (var entryFile in SelectedFileIds)
                    {
                        var groupId = _fileService.GetFile(entryFile).Groups.FirstOrDefault().GroupRecord.Id;
                        _fileService.CopyFile(entryFile, groupId, viewModel.SelectedPasteGroup.Value);
                    }

                    Services.Notifier.Information(T("Entries copy successfuly"));
                }
                else
                {
                    foreach (var entryGroup in SelectedGroupIds)
                    {
                        regroup = true;
                        _groupService.MoveGroup(entryGroup, viewModel.SelectedPasteGroup.Value);
                    }

                    foreach (var entryFile in SelectedFileIds)
                    {
                        var groupId = _fileService.GetFile(entryFile).Groups.FirstOrDefault().GroupRecord.Id;
                        _fileService.MoveFile(entryFile, groupId, viewModel.SelectedPasteGroup.Value);
                    }

                    Services.Notifier.Information(T("Entries move successfuly"));
                }

                //if (regroup) _groupService.RegroupTree();
                
                routeValues.Add("regroup", true);
                
                UpdateCookieValues();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Entries paste failed.");
                Services.Notifier.Error(T("Entries paste failed: {0}", ex));
            }

            return RedirectToAction("Index", routeValues);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submitPage")]
        public ActionResult IndexPageChange()
        {

            string redirectUrl = Request.Form["requestedPage"].ToString();

            var viewModel = new FileGroupIndexViewModel { GroupRecords = new List<GroupEntry>(), FileRecords = new List<FileEntry>(), 
                BulkAction = new FileGroupsAdminIndexBulkAction(), BreadRecord = new List<GroupRecord>(), Gid = 0, DeselectAll = false,
                SelectAll = false};
            

            if (!TryUpdateModel(viewModel))
            {
                return Redirect(Request.Form["requestedPage"].ToString());
            }
            try
            {
                GetSelectedCookieValues(viewModel);

                redirectUrl = FilesGroupsHelpers.UrlAddParam(redirectUrl, "gid", viewModel.Gid.ToString());
                if (viewModel.SelectAll && !viewModel.SelectAllChanged)
                    redirectUrl = FilesGroupsHelpers.UrlAddParam(redirectUrl, "sel", "true");
                else
                    redirectUrl = FilesGroupsHelpers.UrlRemoveParam(redirectUrl, "sel");

                UpdateCookieValues();

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Change page failed.");
                Services.Notifier.Error(T("Change page failed: {0}", ex));
            }

            return Redirect(redirectUrl);
        }

        /// <summary>
        /// Get selcted ids from cookie.
        /// </summary>
        private void GetSelectedFromCookie()
        {
            if (HttpContext.Request.Cookies.Get(CookieNameFile) != null)
            {
                var tmpCookie = HttpContext.Request.Cookies.Get(CookieNameFile);
                SelectedFileIds = StringToListInt(tmpCookie.Value, '_');
            }

            if (HttpContext.Request.Cookies.Get(CookieNameGroup) != null)
            {
                var tmpCookie = HttpContext.Request.Cookies.Get(CookieNameGroup);
                SelectedGroupIds = StringToListInt(tmpCookie.Value, '_');
            }
        }

        /// <summary>
        /// Get selected ids from cookie and add new selected values, remove old values.
        /// </summary>
        /// <param name="viewModel"></param>
        private void GetSelectedCookieValues(FileGroupIndexViewModel viewModel)
        {
            GetSelectedFromCookie();

            if (viewModel.DeselectAll)
            {
                SelectedFileIds = SelectedGroupIds = new List<int>();
            }

            if (viewModel.SelectAll)
            {
                SelectedGroupIds = _groupService.GetGroups().Where(x => x.Parent != null && x.Parent.Id == viewModel.Gid).Select(x => x.Id).ToList();
                SelectedFileIds = _fileService.GetFiles().Where(x => x.Groups.Any(y => y.FileRecord.Id == x.Id && y.GroupRecord.Id == viewModel.Gid)).Select(x => x.Id).ToList();
            }

            //else
            //{
                IEnumerable<GroupEntry> checkedEntriesGroup = viewModel.GroupRecords.Where(t => t.IsChecked);
                IEnumerable<int> deselectEntriesGroup = viewModel.GroupRecords.Where(t => !t.IsChecked).Select(x => x.Group.Id);
                IEnumerable<FileEntry> checkedEntriesFiles = viewModel.FileRecords.Where(t => t.IsChecked);
                IEnumerable<int> deselectEntriesFiles = viewModel.FileRecords.Where(t => !t.IsChecked).Select(x => x.File.Id);

                SelectedFileIds = SelectedFileIds.Concat(checkedEntriesFiles.Select(x => x.File.Id).ToList()).Distinct().ToList();
                SelectedFileIds = SelectedFileIds.Where(x => !deselectEntriesFiles.Contains(x)).ToList();

                SelectedGroupIds = SelectedGroupIds.Concat(checkedEntriesGroup.Select(x => x.Group.Id).ToList()).Distinct().ToList();
                SelectedGroupIds = SelectedGroupIds.Where(x => !deselectEntriesGroup.Contains(x)).ToList();
            //}
        }


        /// <summary>
        /// Set new selected ids to cookie
        /// </summary>
        private void UpdateCookieValues()
        {
            var selectedIdToCookie = new HttpCookie(CookieNameFile);

            selectedIdToCookie.Value = ListIntToString(SelectedFileIds, "_");

            if (HttpContext.Request.Cookies.Get(CookieNameFile) == null)
            {
                HttpContext.Response.Cookies.Add(selectedIdToCookie);
            }
            else
            {
                HttpContext.Response.Cookies.Set(selectedIdToCookie);
            }

            var selectedIdToCookieGroup = new HttpCookie(CookieNameGroup);
            selectedIdToCookieGroup.Value = ListIntToString(SelectedGroupIds, "_");


            if (HttpContext.Request.Cookies.Get(CookieNameGroup) != null)
            {
                HttpContext.Response.Cookies.Set(selectedIdToCookieGroup);
            }
            else
            {
                HttpContext.Response.Cookies.Add(selectedIdToCookieGroup);
            }
        }      

        private string ListIntToString(List<int> listOfInt, string separator)
        {
            if (string.IsNullOrEmpty(separator)) separator = "_";
            var cookieString = string.Join(separator, listOfInt.ToArray());
            var plainBytes = Encoding.UTF8.GetBytes(cookieString);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, 
                DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(encryptedBytes);
        }


        private List<int> StringToListInt(string items, char separator)
        {
            List<int> ret = new List<int>();
            try
            {
                var encryptedBytes = Convert.FromBase64String(items);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, 
                    null, DataProtectionScope.LocalMachine);
                var plaintext = Encoding.UTF8.GetString(decryptedBytes);
                ret = plaintext.Split(separator).ToList().
                    ConvertAll<int>(x => Convert.ToInt32(x));
            }
            catch
            {
                ret = new List<int>();
            }
            return ret;
        }

        private string PathToCurentRootFolder(GroupRecord selectedgroup)
        {
            string ret = "";
            if (selectedgroup == null) return "[";
            var curent = selectedgroup;
            while (curent.Parent != null)
            {
                ret = "/" + curent.Name + ret;
                curent = curent.Parent;
            }
            return "/Media Folders" + ret;
        }


        [Browsable(false)]
        private  List<int> selectedFileIds;
        public  List<int> SelectedFileIds 
        {
            get {
                if (selectedFileIds == null)
                    selectedFileIds = new List<int>();
                return selectedFileIds; 
            }
            set { selectedFileIds = value; }
        }

        [Browsable(false)]
        private List<int> selectedGroupIds;
        public List<int> SelectedGroupIds
        {
            get
            {
                if (selectedGroupIds == null)
                    selectedGroupIds = new List<int>();
                return selectedGroupIds;
            }
            set { selectedGroupIds = value; }
        }

        #endregion

        #region Create
        
        public ActionResult Create(int? gid)
        {
            try
            {
                if (!gid.HasValue)
                    return RedirectToAction("Index");

                var parent = _groupService.GetGroup((int)gid);
                var breadcrumbsQ = new List<GroupRecord>();
                while (parent.Parent != null)
                {
                    breadcrumbsQ.Add(parent);
                    parent = parent.Parent;
                }

                var breadcrumbs = breadcrumbsQ.OrderBy(x => x.Level).ToList();

                var roles = _roleService.GetRoles().ToList();
                var systemRoles = roles.Select(x => new RoleEntry { Role = new RoleRecord() { Name = x.Name, Id = x.Id }, IsChecked = false }).ToList();

                var model = new FileGroupCreateViewModel() { BreadRecord = breadcrumbs, Active = true, SystemRoles = systemRoles };
                return View(model);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Loading create group failed.");
                Services.Notifier.Error(T("Loading create group failed: {0}.", ex));
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Create")]
        [FormValueRequired("submit.Create")]
        public ActionResult CreatePost(int? gid)
        {
            var routeValues = new RouteValueDictionary();
            var viewModel = new FileGroupCreateViewModel();

            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't create media folder")))
                return new HttpUnauthorizedResult();

            try
            {
                IEnumerable<GroupRecord> groupsQ = _groupService.GetGroups();
                if (!gid.HasValue)
                    gid = groupsQ.OrderBy(x => x.Level).Select(y => y.Id).FirstOrDefault();
                var parent = groupsQ.Where(x => x.Id == gid).FirstOrDefault();

                if (parent == null)
                {
                    Logger.Error("Create folder failed. Parent group doesn't exist.");
                    Services.Notifier.Error(T("Create folder failed. Parent group doesn't exist."));
                    return RedirectToAction("Index", routeValues);
                }


                routeValues.Add("gid", gid);

                if (!TryUpdateModel(viewModel))
                {
                    return RedirectToAction("Index", routeValues);
                }
                if (string.IsNullOrEmpty(viewModel.Name.Trim()))
                {
                    Services.Notifier.Error(T("A folder name can't be empty."));
                    return RedirectToAction("Create", routeValues);
                }

                if (!FilesGroupsHelpers.CheckValidityName(viewModel.Name))
                {
                    Services.Notifier.Error(T("A folder name can't contain any of the following characters: \\ / : * ? \" < > |."));
                    return RedirectToAction("Create", routeValues);
                }

                var group = _groupService.CreateGroup(viewModel.Active, viewModel.Description, viewModel.Name, parent);
                if (group == null)
                {
                    Logger.Error("Create folder failed.");
                    Services.Notifier.Error(T("Create folder failed."));
                }
                else
                {
                    List<int> roles = viewModel.SystemRoles.Where(x => x.IsChecked).Select(x => x.Role.Id).ToList();
                    var updateRoles = _permissionService.UpdatePermissions((int)PermisionRecordType.GroupRecordType, gid.Value, roles);
                    if (!updateRoles)
                    {
                        Logger.Error("Group create access permissions failed.");
                        Services.Notifier.Error(T("File create access permissions failed."));
                    }
                    routeValues.Add("regroup", true);
                    Services.Notifier.Information(T("Folder created successfuly"));
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Create folder failed.");
                Services.Notifier.Error(T("Create folder failed: {0}.", ex));
            }
            
            
            return RedirectToAction("Index", routeValues);
        }

        #endregion

        #region EditGroup

        [ActionName("EditGroup")]
        public ActionResult EditGroup(int? gid)
        {
            try
            {
                if (!gid.HasValue)
                    return RedirectToAction("Index");
                var group = _groupService.GetGroup((int)gid);


                var parent = group.Parent;
                var breadcrumbsQ = new List<GroupRecord>();
                while (parent.Parent != null)
                {
                    breadcrumbsQ.Add(parent);
                    parent = parent.Parent;
                }
                var breadcrumbs = breadcrumbsQ.OrderBy(x => x.Level).ToList();
                breadcrumbs.Add(group);
                
                UserPart user = null;
                if (group.CreatorId.HasValue)
                    user = Services.ContentManager.Get<UserPart>((int)group.CreatorId);
                
                var roles = _roleService.GetRoles().ToList();
                var actRoles = _permissionService.GetRecordPermission(group.Id, (int)PermisionRecordType.GroupRecordType).Select(x => x.RoleId).ToList();
                var systemRoles = roles.Select(x => new RoleEntry { Role = new RoleRecord(){ Name = x.Name, Id = x.Id}, IsChecked = actRoles.Contains(x.Id) }).ToList();

                var model = new FileGroupEditViewModel() { 
                    Group = group, 
                    BreadRecord = breadcrumbs,
                    Creator = user,
                    SystemRoles = systemRoles
                };

                return View(model);
            } 
            catch (Exception ex)
            {
                Services.Notifier.Error(T("Load group edit failed: {0}.", ex));
                Logger.Error(ex, "Load group edit failed.");
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("EditGroup")]
        [FormValueRequired("submit.Save")]
        public ActionResult EditGroupPostSave(int? gid)
        {
            var routeValues = new RouteValueDictionary();
            var viewModel = new FileGroupEditViewModel();

            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't manage media folder")))
                return new HttpUnauthorizedResult();

            if (gid.HasValue) routeValues.Add("gid", gid);
            else RedirectToAction("Index", routeValues);

            try
            {
                if (!TryUpdateModel(viewModel))
                {
                    return RedirectToAction("Index", routeValues);
                }

                if (string.IsNullOrEmpty(viewModel.Group.Name))
                {
                    Services.Notifier.Error(T("A folder name can't be empty."));
                    return RedirectToAction("EditGroup", routeValues);
                }

                if (_fileService.GetFiles().Any(x => x.Groups.Any(y => y.GroupRecord.Id == viewModel.Group.Id && x.Name == viewModel.Group.Name)))
                {
                    Services.Notifier.Error(T("There is already a file with the same name as the folder name you specified."));
                    return RedirectToAction("EditGroup", routeValues);
                }

                if (!FilesGroupsHelpers.CheckValidityName(viewModel.Group.Name))
                {
                    Services.Notifier.Error(T("A folder name can't contain any of the following characters: \\ / : * ? \" < > |."));
                    return RedirectToAction("EditGroup",routeValues);
                }

                List<int> roles = viewModel.SystemRoles.Where(x => x.IsChecked).Select(x => x.Role.Id).ToList();
                var updateRoles = _permissionService.UpdatePermissions((int)PermisionRecordType.GroupRecordType, gid.Value, roles);
                if (!updateRoles)
                {
                    Logger.Error("Group update access permissions failed.");
                    Services.Notifier.Error(T("File update access permissions failed."));
                }

                if (!(_groupService.UpdateGroup(gid.Value, viewModel.Group)))
                {
                    Logger.Error("Folder update failed.");
                    Services.Notifier.Error(T("Folder update failed"));
                    return RedirectToAction("EditGroup", routeValues);
                }
                else Services.Notifier.Information(T("Folder update successfuly"));
            }
            catch(Exception ex) 
            {
                Logger.Error(ex, "Folder update failed.");
                Services.Notifier.Error(T("Folder update failed: {0}.", ex));
            }

            return RedirectToAction("Index", routeValues);
        }

        [HttpPost, ActionName("EditGroup")]
        [FormValueRequired("submit.Delete")]
        public ActionResult EditGroupPostDelete(int? gid)
        {

            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't delete media folder")))
                return new HttpUnauthorizedResult();

            if (!gid.HasValue) RedirectToAction("Index");

            try
            {
                if (!(_groupService.DeleteGroup((int)gid)))
                {
                    Logger.Error("Folder delete failed.");
                    Services.Notifier.Error(T("Folder delete failed"));
                }
                else Services.Notifier.Information(T("Folder delete successfuly"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Folder delete failed.");
                Services.Notifier.Error(T("Folder delete failed: {0}.", ex));
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region Add
        
        public ActionResult Add(int? gid)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't upload media file")))
                return new HttpUnauthorizedResult();

            var routeValues = new RouteValueDictionary();

            try
            {
                if (!gid.HasValue)
                    gid = _groupService.GetGroups().Where(x => x.Level == 0).FirstOrDefault().Id;

                if (!gid.HasValue)
                    gid = _groupService.GetGroups().FirstOrDefault().Id;

                routeValues.Add("gid", gid.Value);
                var parent = _groupService.GetGroup((int)gid);
                var breadcrumbsQ = new List<GroupRecord>();
                while (parent.Parent != null)
                {
                    breadcrumbsQ.Add(parent);
                    parent = parent.Parent;
                }

                var separators = new List<string>() { " " };
                
                var uploadWhiteList = new string[0];

                var uplWHLst = _settingsService.GetFileUploadFilesWhiteList();
                if (uplWHLst != null && !string.IsNullOrEmpty(uplWHLst.UploadAllowedFileTypeWhitelist)) 
                {
                    uploadWhiteList = uplWHLst.UploadAllowedFileTypeWhitelist.Split(separators.ToArray(),StringSplitOptions.RemoveEmptyEntries);
                }

                if (uploadWhiteList == null)
                    uploadWhiteList = new String[0];
                
                var breadcrumbs = breadcrumbsQ.OrderBy(x => x.Level).ToList();
                var maxRequestLenght = (System.Web.Configuration.WebConfigurationManager.GetSection("system.web/httpRuntime") as System.Web.Configuration.HttpRuntimeSection).MaxRequestLength * 1024;
                var model = new FileItemAddViewModel { BreadRecord = breadcrumbs, Gid = (int)gid, MaxRequestLength = maxRequestLenght, UploadAllowedFileTypeWhitelist = string.Join("|", uploadWhiteList) };
                return View(model);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Add file load failed.");
                Services.Notifier.Error(T("Add file load failed: {0}.", ex));
            }
            return RedirectToAction("Index", routeValues);
        
        }

        [HttpPost, ActionName("Add")]
        public ActionResult AddPost(int? gid, HttpPostedFileBase file)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't upload media file")))
                return new HttpUnauthorizedResult();

            var viewModel = new FileItemAddViewModel();
            try
            {
                UpdateModel(viewModel);

                if (String.IsNullOrWhiteSpace(Request.Files[0].FileName))
                {
                    ModelState.AddModelError("File", T("Select a file to upload").ToString());
                }

                if (!ModelState.IsValid)
                    return View(viewModel);

                string path = null;
                GroupRecord parent = new GroupRecord();
                try
                {
                    parent = _groupService.GetGroup(viewModel.Gid);
                    path = _groupService.GetGroupPath(parent);

                }
                catch { }

                foreach (string fileName in Request.Files)
                {
                    _fileService.UploadFile(path, Request.Files[fileName], viewModel.ExtractZip, parent);
                }

                Services.Notifier.Information(T("Media file(s) uploaded"));
                return RedirectToAction("Index", new { gid = viewModel.Gid });
            }
            catch (Exception exception)
            {
                Services.Notifier.Error(T("Uploading media file failed: {0}.", exception));
                Logger.Error(exception, "Uploading media file failed.");

                return View(viewModel);
            }
        }


        [HttpPost, ActionName("FileUploadPost")]
        public void FileUploadPost(HttpContext context)
        {
            FileUpload();
        }

        public void FileUpload()
        {
            try
            {
                var context = HttpContext;

                if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't upload media file")))
                {
                    context.Response.StatusCode = 401;
                    context.Response.Write("<html><head></head><body><h1>401 -  Access denied</h1><p>Couldn't upload media file.</p></body></html>");
                }

                if (context.Request.HttpMethod == "GET")
                {
                    WriteFileListJson(context, new Dictionary<string, FileData>());
                    return;
                }
                if (context.Request.HttpMethod != "POST")
                {
                    context.Response.StatusCode = 403;
                    context.Response.Write("<html><head></head><body><h1>403 - Forbidden</h1><p>Uploaded files must be POSTed.</p></body></html>");
                    return;
                }

                var viewModel = new FileItemAddViewModel();
                var routeValues = new RouteValueDictionary();
                UpdateModel(viewModel);

                routeValues.Add("gid", viewModel.Gid);
                string path = null;
                GroupRecord parent = new GroupRecord();
                try
                {
                    parent = _groupService.GetGroup(viewModel.Gid);
                    path = _groupService.GetGroupPath(parent);

                }
                catch { }

                if (context.Request.Files.Count == 0)
                {
                    throw new Exception("File missing from form post");
                }
                if (context.Request.Files.Count > 1)
                {
                    Services.Notifier.Error(T("Currently only supports single file at a time."));
                    RedirectToAction("Add", routeValues);
                    return;
                }
                var uploadedFiles = new Dictionary<string, FileData>();
                bool uploadedZip = false;
                foreach (var key in context.Request.Files.AllKeys)
                {
                    var file = HttpContext.Request.Files[key];
                    var fileName = file.FileName;

                    if (viewModel.ExtractZip && !uploadedZip)
                        uploadedZip = FilesGroupsHelpers.IsZipFile(Path.GetExtension(fileName));

                    var filePath = _fileService.UploadFile(path, file, viewModel.ExtractZip, parent);
                    var saveFile = _fileService.GetFiles().Where(x => x.Name == fileName && x.Groups.Any(y => y.GroupRecord.Id == parent.Id)).FirstOrDefault();
                    var fileData =  new FileData
                    {
                        Name = fileName,
                        Size = file.ContentLength,
                        SavePath = filePath,
                        FileId = saveFile != null ? saveFile.Id : 0
                    };
                   
                    uploadedFiles.Add(fileName, fileData);
                }
                WriteFileListJson(context, uploadedFiles);
                
                if (uploadedZip && viewModel.ExtractZip)
                    _groupService.Actualize();
            }
            catch (Exception ex)
            {
                Services.Notifier.Error(T("Uploading media file failed: {0}.", ex));
                Logger.Error(ex, "Uploading media file failed.");
                RedirectToAction("Index");
            }
        }


        /// <summary>
        /// Writes the file list as JSON to the httpcontect, setting content type and encoding.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="uploadedFiles">The uploaded files to write out.</param>
        private static void WriteFileListJson(HttpContextBase context, Dictionary<string, FileData> uploadedFiles)
        {
            context.Response.ContentType = context.Request.AcceptTypes != null && context.Request.AcceptTypes.AsQueryable().Contains("application/json") ? "application/json" : "text/plain";
            context.Response.ContentEncoding = Encoding.UTF8;
           
            var results = new List<object>();
            foreach (var file in uploadedFiles)
            {
                results.Add(new
                {
                    name = file.Value.Name,
                    size = file.Value.Size
                    //fileid = file.Value.FileId,
                });
            }
            
            JavaScriptSerializer js = new JavaScriptSerializer();
            context.Response.Write(js.Serialize(results));
        }


        /// <summary>
        /// Container for all the data we need to know about an uploaded file,
        /// including data for redisplay in the uploader, and where the temp file
        /// is saved.
        /// </summary>
        private class FileData
        {
            public string Name { get; set; }
            public long Size { get; set; }
            public string SavePath { get; set; }
            public int FileId { get; set; }
        }

        #endregion

        #region EditFile
        
        public ActionResult EditFile(int? fid, int? gid)
        {
            try
            {
                if (!fid.HasValue)
                    return RedirectToAction("Index");
                var file = _fileService.GetFile((int)fid);

                if (!gid.HasValue)
                    gid = _groupService.GetGroups().OrderBy(x => x.Level).Select(y => y.Id).FirstOrDefault();

                var parent = _groupService.GetGroup((int)gid);
                List<GroupRecord> breadcrumbs = new List<GroupRecord>();
                if (parent != null)
                {
                    var breadcrumbsQ = new List<GroupRecord>();
                    while (parent.Parent != null)
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

                path = Path.Combine(path, file.Name);

                path = _fileService.FilePublicPath(path);

                UserPart user = null;
                if (file.CreatorId.HasValue)
                    user = Services.ContentManager.Get<UserPart>((int)file.CreatorId);

                var roles = _roleService.GetRoles().ToList();
                var actRoles = _permissionService.GetRecordPermission(file.Id, (int)PermisionRecordType.FileRecordType).Select(x => x.RoleId).ToList();
                var systemRoles = roles.Select(x => new RoleEntry { Role = new RoleRecord() { Name = x.Name, Id = x.Id }, IsChecked = actRoles.Contains(x.Id) }).ToList();

                var model = new FileEditViewModel() { File = file, BreadRecord = breadcrumbs, FilePath = path, Creator = user, FileType = _settingsService.GetFileType(file.Ext), SystemRoles = systemRoles };

                return View(model);
            }
            catch (Exception ex)
            {
                Services.Notifier.Error(T("Load file edit failed: {0}.", ex));
                Logger.Error(ex, "Load file edit failed.");
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("EditFile")]
        [FormValueRequired("submit.Save")]
        public ActionResult EditFilePostSave(int? fid, int? gid)
        {
            var routeValues = new RouteValueDictionary();
            var viewModel = new FileEditViewModel();

            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't modify media file")))
                return new HttpUnauthorizedResult();

            if (gid.HasValue) routeValues.Add("gid", gid);
            if (!fid.HasValue) RedirectToAction("Index", routeValues);


            try
            {
                if (!TryUpdateModel(viewModel))
                {
                    return RedirectToAction("Index", routeValues);
                }

                if (string.IsNullOrWhiteSpace(viewModel.File.Name))
                {
                    Services.Notifier.Error(T("A file name can't be empty."));
                    routeValues.Add("fid", fid);
                    return RedirectToAction("EditFile", routeValues);
                }

                if (Path.GetExtension(viewModel.File.Name).Length < 1)
                {
                    Services.Notifier.Error(T("New file \"{0}\" name is not allowed. Please provide a file extension.", viewModel.File.Name));
                    routeValues.Add("fid", fid);
                    return RedirectToAction("EditFile", routeValues);
                }


                if (!FilesGroupsHelpers.CheckValidityName(viewModel.File.Name))
                {
                    Services.Notifier.Error(T("A file name can't contain any of the following characters: \\ / : * ? \" < > |."));
                    routeValues.Add("fid", fid);
                    return RedirectToAction("EditGroup", routeValues);
                }

                List<int> roles = viewModel.SystemRoles.Where(x => x.IsChecked).Select(x => x.Role.Id).ToList();
                var updateRoles = _permissionService.UpdatePermissions((int)PermisionRecordType.FileRecordType, fid.Value, roles);
                if (!updateRoles)
                {
                    Logger.Error("File update access permissions failed.");
                    Services.Notifier.Error(T("File update access permissions failed."));
                }

                if (!(_fileService.UpdateFile((int)fid, viewModel.File, (int)gid)))
                {
                    Logger.Error("File update failed.");
                    Services.Notifier.Error(T("File update failed."));
                }
                else Services.Notifier.Information(T("File update successfuly"));

                   
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "File update failed.");
                Services.Notifier.Error(T("File update failed: {0}.", ex));
            }
            //routeValues.Add("fid", fid.Value);
            //return RedirectToAction("EditFile", routeValues);
            return RedirectToAction("Index", routeValues);
        }

        [HttpPost, ActionName("EditFile")]
        [FormValueRequired("submit.Delete")]
        public ActionResult EditFilePostDelete(int? fid, int? gid)
        {
            var routeValues = new RouteValueDictionary();
            if (gid.HasValue) routeValues.Add("gid", gid);

            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't delete media file")))
                return new HttpUnauthorizedResult();

            if (!fid.HasValue || !gid.HasValue) return RedirectToAction("Index", routeValues);

            try
            {
                if (!(_fileService.DeleteFile((int)fid, (int)gid)))
                {
                    Logger.Error("Folder delete failed.");
                    Services.Notifier.Error(T("File delete failed"));
                }
                else Services.Notifier.Information(T("File delete successfuly"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Folder delete failed.");
                Services.Notifier.Error(T("Folder delete failed: {0}.",ex));
            }

            return RedirectToAction("Index",routeValues);
        }

        #endregion

        #region DeleteFromIndex
        
        public ActionResult DeleteFile(int? fid, int? gid)
        {

            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't delete media file")))
                return new HttpUnauthorizedResult();

            var routeValues = new RouteValueDictionary();
            if (gid.HasValue) routeValues.Add("gid", gid);
            if (!fid.HasValue) return RedirectToAction("Index", routeValues);

            try
            {
                if (!(_fileService.DeleteFile((int)fid, (int)gid)))
                {
                    Logger.Error("Folder delete failed.");
                    Services.Notifier.Error(T("File delete failed"));
                }
                else Services.Notifier.Information(T("File delete successfuly"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Folder delete failed.");
                Services.Notifier.Error(T("Folder delete failed: {0}.", ex));
            }

            return RedirectToAction("Index", routeValues);

        }

        public ActionResult DeleteGroup(int? gid, int? rid)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't delete media folder")))
                return new HttpUnauthorizedResult();

            var routeValues = new RouteValueDictionary();
            if (rid.HasValue) routeValues.Add("gid", rid);

            if (!gid.HasValue) return RedirectToAction("Index", routeValues);

            try
            {
                if (!(_groupService.DeleteGroup((int)gid)))
                {
                    Logger.Error("Folder delete failed.");
                    Services.Notifier.Error(T("Folder delete failed"));
                }
                else Services.Notifier.Information(T("Folder delete successfuly"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Folder delete failed.");
                Services.Notifier.Error(T("Folder delete failed: {0}.",ex));
            }
          
            return RedirectToAction("Index", routeValues);
        }

        #endregion

        #region Public, unpublic from index
        
        public ActionResult PublicGroup(int? gid, int? rid) 
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't modify media folder")))
                return new HttpUnauthorizedResult();

            var routeValues = new RouteValueDictionary();
            if (rid.HasValue) routeValues.Add("gid", rid);

            if (!gid.HasValue) return RedirectToAction("Index", routeValues);

            try
            {
                if (!(_groupService.ChangeActiveGroup((int)gid, true)))
                {
                    Logger.Error("Group public failed.");
                    Services.Notifier.Error(T("Group public failed"));
                }
                else Services.Notifier.Information(T("Group public successfuly"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Group public failed.");
                Services.Notifier.Error(T("Group public failed: {0}",ex));
            }

            return RedirectToAction("Index", routeValues);
        }

        public ActionResult UnPublicGroup(int? gid, int? rid)
        {

            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't modify media folder")))
                return new HttpUnauthorizedResult();

            var routeValues = new RouteValueDictionary();
            if (rid.HasValue) routeValues.Add("gid", rid);

            if (!gid.HasValue) return RedirectToAction("Index", routeValues);

            try
            {
                if (!(_groupService.ChangeActiveGroup((int)gid, false)))
                {
                    Logger.Error("Group unpublic failed.");
                    Services.Notifier.Error(T("Group unpublic failed"));
                }
                else Services.Notifier.Information(T("Group unpublic successfuly"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Group unpublic failed.");
                Services.Notifier.Error(T("Group unpublic failed: {0}.", ex));
            }

            return RedirectToAction("Index", routeValues);
        }

        public ActionResult PublicFile(int? fid, int? rid) 
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't modify media file")))
                return new HttpUnauthorizedResult();

            var routeValues = new RouteValueDictionary();
            if (rid.HasValue) routeValues.Add("gid", rid);

            if (!fid.HasValue) return RedirectToAction("Index", routeValues);

            try
            {
                if (!(_fileService.ChangeActiveFile((int)fid, true)))
                {
                    Logger.Error("Folder public failed.");
                    Services.Notifier.Error(T("File public failed"));
                }
                else Services.Notifier.Information(T("File public successfuly"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Folder public failed.");
                Services.Notifier.Error(T("Folder public failed: {0}.", ex));
            }

            return RedirectToAction("Index", routeValues);
        }

        public ActionResult UnPublicFile(int? fid, int? rid)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageMedia, T("Couldn't modify media file")))
                return new HttpUnauthorizedResult();

            var routeValues = new RouteValueDictionary();
            if (rid.HasValue) routeValues.Add("gid", rid);

            if (!fid.HasValue) return RedirectToAction("Index", routeValues);

            try
            {
                if (!(_fileService.ChangeActiveFile((int)fid, false)))
                {
                    Logger.Error("Folder unpublic failed.");
                    Services.Notifier.Error(T("File unpublic failed"));
                }
                else Services.Notifier.Information(T("File unpublic successfuly"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Folder unpublic failed.");
                Services.Notifier.Error(T("Folder unpublic failed: {0}", ex));
            }

            return RedirectToAction("Index", routeValues);
        }

        #endregion

        #region Settings

        public ActionResult Settings()
        {
            try
            {
                var model = new SettingsViewModel() { PictureExtensions = "", DocumentsExtensions = "", VideoExtensions = "", UploadAllowedFileTypeWhitelist = "" };
                var pict = _settingsService.GetFileSetting((int)FileManagerSettingsTypes.pictures);
                if (pict != null)
                    model.PictureExtensions = pict.FileExtensions;

                var doc = _settingsService.GetFileSetting((int)FileManagerSettingsTypes.documents);
                if (doc != null)
                    model.DocumentsExtensions = doc.FileExtensions;

                var vid = _settingsService.GetFileSetting((int)FileManagerSettingsTypes.video);
                if (vid != null)
                    model.VideoExtensions = vid.FileExtensions;
                var uplWhite = _settingsService.GetFileUploadFilesWhiteList();
                if (uplWhite != null)
                    model.UploadAllowedFileTypeWhitelist = uplWhite.UploadAllowedFileTypeWhitelist;

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Load file settings failed.");
                Services.Notifier.Error(T("Load file settings failed: {0}.", ex));
            }

            return View();
        }

        [HttpPost, ActionName("Settings")]
        [FormValueRequired("submit.Save")]
        public ActionResult SettingsSave()
        {
            var viewModel = new SettingsViewModel();
            try
            {
                if (!TryUpdateModel(viewModel))
                {
                    Services.Notifier.Error(T("Load file settings save failed."));
                    return View(); ;
                }

                _settingsService.UpdateFileSettings((int)FileManagerSettingsTypes.pictures, viewModel.PictureExtensions);
                _settingsService.UpdateFileSettings((int)FileManagerSettingsTypes.documents, viewModel.DocumentsExtensions);
                _settingsService.UpdateFileSettings((int)FileManagerSettingsTypes.video, viewModel.VideoExtensions);
                _settingsService.UpdateFileUploadWhiteList(viewModel.UploadAllowedFileTypeWhitelist);

                Services.Notifier.Information(T("Update files settings succefull."));
            }
            catch(Exception ex)
            {
                Logger.Error(ex, "Load file settings save failed.");
                Services.Notifier.Error(T("Load file settings save failed: {0}.", ex));
            }
            return View();
        }

        [HttpPost, ActionName("Settings")]
        [FormValueRequired("submit.Refresh")]
        public ActionResult SettingsRefresh()
        {
            try
            {
                _groupService.Actualize();
                Services.Notifier.Information(T("Actualize file and folders succefully."));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Actualize file and folders failed.");
                Services.Notifier.Error(T("Actualize file and folders failed: {0}.", ex));
            }
            return RedirectToAction("Settings");
        }


        #endregion

        #region Media Picker

        [Themed(false)]
        public ActionResult MediaPicker(int? gid)
        {
            IEnumerable<GroupRecord> groupsQ = _groupService.GetGroups();
            GroupRecord parent = null;

            if (gid.HasValue)
                parent = groupsQ.Where(x => x.Id == gid).FirstOrDefault();

            if (!gid.HasValue || parent == null)
                gid = groupsQ.OrderBy(x => x.Level).Select(y => y.Id).FirstOrDefault();

            parent = groupsQ.Where(x => x.Id == gid).FirstOrDefault();


            var breadcrumbsQ = new List<GroupRecord>();
            while (parent.Parent != null)
            {
                breadcrumbsQ.Add(parent);
                parent = parent.Parent;
            }

            var breadcrumbs = breadcrumbsQ.OrderBy(x => x.Level).ToList();
            var groupEntries = groupsQ.Where(x => x.Parent != null && x.Parent.Id == gid).Select(FilesGroupsHelpers.CreateGroupEntry).OrderBy(x => x.Group.Name).ToList();

            string path = "";
            foreach (var item in breadcrumbs)
            {
                path = Path.Combine(path, item.Name);
            }

            IEnumerable<FileRecord> filesQ = _fileService.GetFiles().Where(x => x.Groups.Any(y => y.FileRecord.Id == x.Id && y.GroupRecord.Id == gid));
            List<FilePickerEntry> fileEntries = new List<FilePickerEntry>();

            foreach (var item in filesQ)
            {
                fileEntries.Add(new FilePickerEntry()
                {
                     File = item,
                     PublicPath = _fileService.FilePublicPath(Path.Combine(path, item.Name)),
                     FileType = _settingsService.GetFileType(item.Ext)
                });
            }

            fileEntries = fileEntries.OrderBy(x => x.File.Name).ToList();

            var model = new MediaPickerViewModel
            {
                FileRecords = fileEntries,
                GroupRecords = groupEntries,
                BreadRecord = breadcrumbs,
                Gid = (int)gid,
            };

            ViewData["Service"] = _settingsService;
            return View(model);
        }


        [HttpPost]
        public JsonResult CreateFolder(int? gid, string folderName)
        {
            try
            {
                if (!Services.Authorizer.Authorize(Permissions.ManageMedia))
                {
                    return Json(new { Success = false, Message = T("Couldn't create media folder").ToString() });
                }

                if (string.IsNullOrEmpty(folderName))
                    return Json(new { Success = false, Message = T("Create folder failed. Folder name can't by empty.").ToString() });

                if (!FilesGroupsHelpers.CheckValidityName(folderName))
                {
                    return Json(new { Success = false, Message = T("A folder name can't contain any of the following characters: \\ / : * ? \" < > |.").ToString() });
                }

                IEnumerable<GroupRecord> groupsQ = _groupService.GetGroups();
                if (!gid.HasValue)
                    gid = groupsQ.OrderBy(x => x.Level).Select(y => y.Id).FirstOrDefault();
                var parent = groupsQ.Where(x => x.Id == gid).FirstOrDefault();

                if (parent == null)
                {
                    return Json(new { Success = false, Message = T("Create folder failed. Parent group doesn't exist.").ToString() });
                }

                var group = _groupService.CreateGroup(true, null, folderName, parent);
                if (group == null)
                {
                    Logger.Error("Create folder failed.");
                    Services.Notifier.Error(T("Create folder failed."));
                }

                return Json(new { Success = true, Message = "" });
            }
            catch (Exception exception)
            {
                return Json(new { Success = false, Message = T("Creating Folder failed: {0}", exception.Message).ToString() });
            }
        }

        [HttpPost]
        public ContentResult AddFromClient()
        {
            var viewModel = new MediaPickerViewModel();
            try
            {
                if (!Services.Authorizer.Authorize(Permissions.ManageMedia))
                    return Content(string.Format("<script type=\"text/javascript\">var result = {{ error: \"{0}\" }};</script>", T("ERROR: You don't have permission to upload media files")));

                UpdateModel(viewModel);

                if (Request.Files.Count < 1 || Request.Files[0].ContentLength == 0)
                    return Content(string.Format("<script type=\"text/javascript\">var result = {{ error: \"{0}\" }};</script>", T("HEY: You didn't give me a file to upload")));

                if (String.IsNullOrWhiteSpace(Request.Files[0].FileName))
                {
                    return Content(string.Format("<script type=\"text/javascript\">var result = {{ error: \"{0}\" }};</script>", T("ERROR: Uploading media file failed: file is empty")));
                }
                if (viewModel.UploadUrl)
                {
                    var pageGroup = _groupService.GetGroups().Where(x => x.Name == "Page" && x.Level == 1).FirstOrDefault();
                    if (pageGroup == null)
                    {
                        var parentPageGroup = _groupService.GetGroups().Where(x => x.Level == 0).FirstOrDefault();
                        pageGroup = _groupService.CreateGroup(false, null, "Page", parentPageGroup);
                    }
                    viewModel.Gid = pageGroup.Id;
                }


                string path = null;
                GroupRecord parent = new GroupRecord();
                try
                {
                    parent = _groupService.GetGroup(viewModel.Gid);
                    path = _groupService.GetGroupPath(parent);

                    if (parent == null || path == null)
                        return Content(string.Format("<script type=\"text/javascript\">var result = {{ error: \"{0}\" }};</script>", T("ERROR: Uploading media file failed: file is empty")));
                }
                catch (Exception ex)  
                {
                    return Content(string.Format("<script type=\"text/javascript\">var result = {{ error: \"{0}\" }};</script>", T("ERROR: Uploading media file failed: {0}", ex.Message)));
                }


                var file = Request.Files[0];
                var publicUrl = _fileService.UploadFile(path, file, false, parent);
                var type = _settingsService.GetFileType(Path.GetExtension(publicUrl));
                var iconUrl = publicUrl;
                if (type != FileManagerSettingsTypes.pictures)
                {
                    iconUrl = HttpContext.Request.ApplicationPath + FilesGroupsHelpers.GetExtensionImagePath(Path.GetExtension(publicUrl)).Substring(1);
                    //publicUrl = FilesGroupsHelpers.GetAsoluteUrlPath(publicUrl);
                }

                return Content(string.Format("<script type=\"text/javascript\">var result = {{ url: \"{0}\", iconUrl: \"{1}\", fileType: \"{2}\" }};</script>", publicUrl, iconUrl, type.ToString()));
            }
            catch (Exception exception)
            {
                return Content(string.Format("<script type=\"text/javascript\">var result = {{ error: \"{0}\" }};</script>", T("ERROR: Uploading media file failed: {0}", exception.Message)));
            }
        }

        //[HttpPost]
        public JsonResult GetFileHtml(int? fid, string src, string alt, string cssclass, string style, string align, int? width, int? height, string displaystyle)
        {
            try
            {
                if (fid == null && string.IsNullOrEmpty(src))
                {
                    return Json(new { Success = false, Message = T("Inserting file failed: src is empty.").ToString() });
                    //return JavaScript(T("Inserting file failed: src is empty.").ToString());
                }
                FileRecord file = new FileRecord();
                FileManagerSettingsTypes type;
                DisplayType displayType = DisplayType.inline;
                switch (displaystyle)
                {
                    case "inline": displayType = DisplayType.inline; break;
                    case "element": displayType = DisplayType.elements; break;
                    default: displayType = DisplayType.inline; break;
                }
                
                
                if (fid.HasValue)
                    file = _fileService.GetFile(fid.Value);
                else
                {
                    var name = src.Substring(src.LastIndexOf("/")+1);
                    file.Name = name;
                    file.Ext = name.Substring(name.LastIndexOf(".")+1);
                    file.Size = 0;
                    file.Description = "";
                }

                string ret = "";

                type = _settingsService.GetFileType(file.Ext);

                if (type == FileManagerSettingsTypes.pictures && displayType != DisplayType.inline)
                {
                    ret = FilesGroupsHelpers.GetImageTag(src, file, true, width.Value, height.Value, style, alt, cssclass);
                }
                else if (type == FileManagerSettingsTypes.documents && displayType != DisplayType.inline)
                {
                    ret = FilesGroupsHelpers.GetPagetag(src, width.Value, height.Value, style, cssclass);                    
                }
                else if (type == FileManagerSettingsTypes.video && displayType != DisplayType.inline)
                {
                    ret = FilesGroupsHelpers.GetVideoTag(width.Value, height.Value, cssclass, style,true,src);
                }
                else
                {
                   var control = new Control();
                   var extImg = FilesGroupsHelpers.GetExtensionImagePath(file.Ext);
                   var scripts = string.Format("<link href='{0}' rel='stylesheet' type='text/css'>", control.ResolveUrl("~/Modules/FileManager/Styles/FileDownload.css"));
                   ret ="<div class='file-body-div'><div class='file-body-body-up'>" +
                        "<img src='" + control.ResolveUrl(extImg)  + "' alt='" + file.Name + "'/><div class='file-body-info'>" + 
                        "<div class='file-body-info-title'><a href='"+ src + "'>" + file.Name + "</a>" +
                        "</div><div><label>Size: <span>"+ file.Size.ToFriendlySizeString() + "</span></label>" +
                        "</div></div><div class='file-body-download'><a href='" + src + "'>" +
                        "<img src='" + control.ResolveUrl("~/Modules/FileManager/Styles/Images/download-button.png") + 
                        "' alt='Download " + file.Name + "' title='Download " + file.Name + "' />"+
                        "</a></div></div><div class='file-body-description'>" + file.Description + "</div>";
                   ret = scripts + "\n" +  ret;
                }


                return Json(new { Success = true, Message = "<div>" + ret  + "</div><br/>"});
                //return JavaScript(ret);
                
            }
            catch (Exception exception)
            {
                return Json(new { Success = false, Message = T("Inserting file failed: {0}", exception.Message).ToString() });
                //return JavaScript(T("Inserting file failed: {0}", exception.Message).ToString());
            }
        }

        #endregion
    }
}