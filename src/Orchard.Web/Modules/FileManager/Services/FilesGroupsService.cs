using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using Orchard.Data;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.ContentManagement;
using Orchard.Security;
using FileManager.Models;
using FileManager.Helpers;
using Orchard.UI.Notify;
using Orchard;
using Orchard.Media.Services;
using Orchard.FileSystems.Media;
using Orchard.Media.Models;
using Orchard.Caching;
using Orchard.Roles.Services;


namespace FileManager.Services
{
    [UsedImplicitly]
    public class FilesGroupsService : IGroupsService, IFilesService, IFileManagerService, ISettingsService, IPermissionService
    {
        private const string SignalName = "FileManager.Services.FilesGroupsService";
        private readonly IRepository<GroupRecord> _groupRepository;
        private readonly IRepository<FilesGroupsRecord> _filesGroupRepository;
        private readonly IRepository<FileRecord> _fileRepository;
        private readonly IRepository<FileManagerRecord> _moduleRepository;
        private readonly IRepository<FileManagerSettingsRecord> _settingsRepository;
        private readonly IRepository<PermissionRecord> _permissionRepository;
        private readonly IRepository<MediaSettingsPartRecord> _mediaSettingsRepository;
        private readonly ISignals _signals;


        private readonly INotifier _notifier;
        private readonly IOrchardServices _orchardServices;
        private readonly IStorageProvider _storageProvider;
        private readonly IMediaService _mediaService;
        private readonly IRoleService _roleService;
        public ILogger Logger { get; set; }
        public Localizer T { get; set; }


        public FilesGroupsService(IRepository<GroupRecord> groupRepository,
                          IRepository<FilesGroupsRecord> filesGroupRepository,
                          IRepository<FileRecord> fileRepository,
                          IRepository<FileManagerRecord> moduleRepository,
                          IRepository<FileManagerSettingsRecord> settingsRepository,
                          IRepository<PermissionRecord> permissionRepository,
                          IRepository<MediaSettingsPartRecord> mediaSettingsRepository,
                          ISignals signals,
                          INotifier notifier,
                          IOrchardServices orchardServices,
                          IStorageProvider storageProvider,
                          IRoleService roleService,
                          IMediaService mediaService)
        {
            _groupRepository = groupRepository;
            _filesGroupRepository = filesGroupRepository;
            _fileRepository = fileRepository;
            _moduleRepository = moduleRepository;
            _settingsRepository = settingsRepository;
            _permissionRepository = permissionRepository;
            _mediaSettingsRepository = mediaSettingsRepository;
            _notifier = notifier;
            _orchardServices = orchardServices;
            _storageProvider = storageProvider;
            _mediaService = mediaService;
            _roleService = roleService;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
            _signals = signals;
        }

        const string CopySufixParamName = "-Copy";

        #region IGroupService Members

        public void Actualize()
        {
            Actualize(null);
            TriggerSignal();
        }

        public void Actualize(GroupRecord root)
        {
            var mediaFolders = _mediaService.GetMediaFolders(null).OrderBy(x => x.Name);
            if (root == null)
                root = GetGroups().Where(x => x.Level == 0).ToList().FirstOrDefault();
            if (root == null)
                root = CreateRootGroup();
            
            
            int level = root.Level + 1;


            List<GroupRecord> levelGroups = _groupRepository.Table.Where(x => x.Level == level).ToList();
            foreach (var mediaFolder in mediaFolders)
            {
                GroupRecord currentFolder = null;
                if (!GetGroups().Any(x => x.Name == mediaFolder.Name && x.Level == level))
                {
                    currentFolder = CreateGroup(true, null, mediaFolder.Name, root, false);
                }
                else
                {
                    currentFolder = GetGroups().Where(x => x.Name == mediaFolder.Name && x.Level == level).FirstOrDefault();
                    if (currentFolder != null) levelGroups.Remove(currentFolder);
                }
                if (currentFolder == null) continue;

                Actualize(mediaFolder, level + 1, currentFolder.Name, currentFolder);
            }

            foreach (var item in levelGroups)
            {
                DeleteGroup(item.Id, false);
            }


            IEnumerable<MediaFile> mediaFiles = _mediaService.GetMediaFiles(null).OrderBy(x => x.Name);
            List<FileRecord> levelFiles = GetFiles().Where(x => x.Groups.Any(y => y.GroupRecord.Id == root.Id)).ToList();

            foreach (var item in mediaFiles)
            {
                FileRecord currentFile = null;
                if (!levelFiles.Any(x => x.Name == item.Name))
                {
                    currentFile = CreateFileRecord(item.Name, true, null, item.Size, root);
                }
                else
                {
                    currentFile = levelFiles.Where(x => x.Name == item.Name).FirstOrDefault();
                    if (currentFile != null) levelFiles.Remove(currentFile);
                }
            }

            foreach (var item in levelFiles)
            {
                DeleteFile(item.Id, root.Id, false);
            }
            TriggerSignal();

            var groupIds = GetGroups().Select(x => x.Id).ToList();
            var fileIds = GetFiles().Select(x => x.Id).ToList();

            var wrGroups = GetGroups().Where(x => x.Parent != null && !groupIds.Contains(x.Parent.Id)).ToList();
            foreach (var item in wrGroups)
            {
                DeleteGroup(item.Id, false);
            }
            TriggerSignal();
            groupIds = GetGroups().Select(x => x.Id).ToList();

            var wrFilesJoinGroup = _filesGroupRepository.Table.Where(x => !groupIds.Contains(x.GroupRecord.Id) || !fileIds.Contains(x.FileRecord.Id)).ToList();

            foreach (var item in wrFilesJoinGroup)
            {
                _filesGroupRepository.Delete(item);
            }
            TriggerSignal();

            //var filesInGroup = _filesGroupRepository.Table.ToList().Select(x => x.FileRecord.Id).ToList();

            //var wrFiles = GetFiles().Where(x => !filesInGroup.Contains(x.Id)).ToList();

            //foreach (var item in wrFiles)
            //{
            //    DeleteRecordPermissions(item.Id, (int)PermisionRecordType.FileRecordType);
            //    _fileRepository.Delete(item);
            //}
            TriggerSignal();

            var actRoles = _roleService.GetRoles().Select(x => x.Id).ToList();
            var wrRolesRec = GetPermissions().Where(x => !actRoles.Contains(x.RoleId)).ToList();

            foreach (var item in wrRolesRec)
            {
                _permissionRepository.Delete(item);
            }
            TriggerSignal();

            RegroupTree();
        }

        private void Actualize(MediaFolder parentMediaFolder, int level, string mediaPath, GroupRecord parent)
        {
            if (parent == null) return;

            IEnumerable<MediaFile> mediaFiles = _mediaService.GetMediaFiles(mediaPath).OrderBy(x => x.Name);
            List<FileRecord> levelFiles = GetFiles().Where(x => x.Groups.Any(y => y.GroupRecord.Id == parent.Id)).ToList();

            foreach (var item in mediaFiles)
            {
                FileRecord currentFile = null;
                if (!levelFiles.Any(x => x.Name == item.Name))
                {
                    currentFile = CreateFileRecord(item.Name, true, null, item.Size, parent);
                }
                else
                {
                    currentFile = levelFiles.Where(x => x.Name == item.Name).FirstOrDefault();
                    if (currentFile != null) levelFiles.Remove(currentFile);
                }
            }

            foreach (var item in levelFiles)
            {
                DeleteFile(item.Id, parent.Id, false);
            }


            var mediaFolders = _mediaService.GetMediaFolders(mediaPath).OrderBy(x => x.Name);
            List<GroupRecord> levelGroups = GetGroups().Where(x => x.Level == level && x.Parent.Id == parent.Id).ToList();
            foreach (var mediaFolder in mediaFolders)
            {
                GroupRecord currentFolder = null;
                if (!_groupRepository.Table.Any(x => x.Name == mediaFolder.Name && x.Level == level))
                {
                    currentFolder = CreateGroup(true, null, mediaFolder.Name, parent, false);
                }
                else
                {
                    currentFolder = GetGroups().Where(x => x.Name == mediaFolder.Name && x.Parent.Id == parent.Id).FirstOrDefault();
                    if (currentFolder != null) levelGroups.Remove(currentFolder);
                }

                if (currentFolder == null) continue;

                var path = Path.Combine(mediaPath, currentFolder.Name);
                Actualize(mediaFolder, level + 1, path, currentFolder);
            }

            foreach (var item in levelGroups)
            {
                DeleteGroup(item.Id, false);
            }


        }

        public IEnumerable<GroupRecord> GetGroups()
        {
            return _groupRepository.Table.ToList();
        }

        public GroupRecord GetGroup(int groupId)
        {
            return _groupRepository.Get(x => x.Id == groupId);
        }

        public GroupRecord GetGroupByName(string groupName)
        {
            return _groupRepository.Get(x => x.Name == groupName);
        }

        public GroupRecord CreateRootGroup()
        {
            if (_groupRepository.Table.Any(x => x.Level == 0)) 
                return _groupRepository.Table.Where(x => x.Level == 0).FirstOrDefault();

            var result = new GroupRecord
            {
                Name = "",
                Active = true,
                Alias = "",
                CreateDate = DateTime.Now,
                Level = 0,
                Description = null,
                Parent = null,
                Wlft = 0,
                Wrgt = 1,
                ContentItemRecord = new Orchard.ContentManagement.Records.ContentItemRecord(),
                CreatorId = _orchardServices.WorkContext.CurrentUser.Id,
            };

            _groupRepository.Create(result);
            TriggerSignal();
            return result;
        }


        /// <summary>
        /// Create group, in database and folder in filesystem
        /// </summary>
        /// <param name="active">Define group si active.</param>
        /// <param name="desc">Group description.</param>
        /// <param name="name">Group name.</param>
        /// <param name="parentGroup">Parent of new group</param>
        /// <returns></returns>
        public GroupRecord CreateGroup(bool active, string desc, string name, GroupRecord parentGroup)
        {
            return CreateGroup(active, desc, name, parentGroup, true);
        }



        /// <summary>
        /// Create group, in database and folder in filesystem
        /// </summary>
        /// <param name="active">Define group si active.</param>
        /// <param name="desc">Group description.</param>
        /// <param name="name">Group name.</param>
        /// <param name="parentGroup">Parent of new group</param>
        /// <returns></returns>
        public GroupRecord CreateGroup(bool active, string desc, string name, GroupRecord parentGroup, bool createFolder)
        {
            GroupRecord result = null;
            try
            {
                if (parentGroup == null)
                {
                    Logger.Warning("Faild to save new folder.");
                    return result;
                }

                result = _groupRepository.Get(x => x.Name == name && x.Parent.Id == parentGroup.Id);
                if (result == null)
                {
                    name = FilesGroupsHelpers.TtrimName(name); ;

                    result = new GroupRecord { 
                        Name = name, 
                        Active = active, 
                        Alias = name.ToLower().Trim(), 
                        CreateDate = DateTime.Now, 
                        Level = parentGroup.Level + 1, 
                        Description = desc, 
                        Parent = parentGroup, 
                        Wlft = parentGroup.Wrgt, 
                        Wrgt = parentGroup.Wrgt + 1,  
                        ContentItemRecord = new Orchard.ContentManagement.Records.ContentItemRecord(),
                        CreatorId = _orchardServices.WorkContext.CurrentUser.Id,
                    };

                    var path = GetGroupPath(parentGroup);
                    _groupRepository.Create(result);
                    TriggerSignal();
                    if (createFolder)
                        _mediaService.CreateFolder(path, name);
                }
            }
            catch (Exception ex)
            {
                result = null;
                Logger.Warning(ex,"Faild to save new folder.");
            }

            return result;
        }

        /// <summary>
        /// Update wlft and wrgt of group tree
        /// </summary>
        public void RegroupTree()
        {
            int wlft = 0;
            var groups = _groupRepository.Table.Where(x => x.Parent == null).ToList();

            foreach (var item in groups)
            {
                wlft = RegroupTree(item, wlft);
                wlft++;
            }
            TriggerSignal();
        }

        private int RegroupTree(GroupRecord group, int wlft)
        {
            group.Wlft = wlft;
            
            var childs = _groupRepository.Table.Where(x => x.Parent.Id == group.Id).OrderBy(x => x.Wlft);
            foreach (var item in childs)
            {
                wlft = RegroupTree(item, ++wlft);
            }
            wlft++;
            group.Wrgt = wlft;
            return wlft;
        }

        public string GetGroupPath(GroupRecord parentGroup)
        {
            string path = null;
            var parent = parentGroup;
            while (parent != null)
            {
                if (parent.Parent == null) break;
                if (!string.IsNullOrEmpty(path))
                    path = Path.Combine(parent.Name, path); 
                else
                    path = parent.Name;
                parent = parent.Parent;
            }
            return path;
        }

        public bool UpdateGroup(int groupId, GroupRecord group)
        {
            try
            {
                if (group == null)
                    return false;

                var groupRecord = GetGroup(groupId);

                if (groupRecord != null)
                {
                    group.Name = FilesGroupsHelpers.TtrimName(group.Name);

                    string description = "";
                    if (string.IsNullOrWhiteSpace(group.Description))
                        description = null;
                    else
                        description = group.Description.Trim().ToString();

                    var path = GetGroupPath(groupRecord);
                    if (groupRecord.Name != group.Name)
                        _mediaService.RenameFolder(path, group.Name);
                    
                    groupRecord.Name = group.Name;
                    groupRecord.Active = group.Active;
                    groupRecord.Description = description;
                    groupRecord.UpdateDate = DateTime.Now;
                    TriggerSignal();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Update group record failed");
            }
            return false;
        }


        public bool DeleteGroup(int groupId)
        {
            return DeleteGroup(groupId, true);
        }

        public bool DeleteGroup(int groupId, bool deleteFolder)
        {
            string path = null;
            try
            {
                var group = GetGroup(groupId);
                if (group == null)
                    return false;
                
                var groupsonGroups = _groupRepository.Table.Where(x => x.Wlft > group.Wlft && x.Wrgt < group.Wrgt);

                foreach (var item in groupsonGroups)
                {
                    var filesJoinGroup = _filesGroupRepository.Table.Where(x => x.GroupRecord.Id == item.Id);
                    foreach (var filejoinGroup in filesJoinGroup)
                    {
                        DeleteFile(filejoinGroup.FileRecord.Id, item.Id, deleteFolder);
                        _filesGroupRepository.Delete(filejoinGroup);
                    }


                    path = GetGroupPath(item);
                    if (deleteFolder)
                        _mediaService.DeleteFolder(path);
                    DeleteRecordPermissions(item.Id, (int)PermisionRecordType.GroupRecordType);

                    _groupRepository.Delete(item);
                }

                var fileGroups = _filesGroupRepository.Table.Where(x => x.GroupRecord.Id == group.Id);
                foreach (var fileGroupItem in fileGroups)
                {
                    DeleteFile(fileGroupItem.FileRecord.Id, group.Id, deleteFolder);
                    _filesGroupRepository.Delete(fileGroupItem);
                }

                path = GetGroupPath(group);
                if (deleteFolder)
                    _mediaService.DeleteFolder(path);

                DeleteRecordPermissions(group.Id, (int)PermisionRecordType.GroupRecordType);
                _groupRepository.Delete(group);
                TriggerSignal();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Delete group record failed");
                return false;
            }
        }

        public List<SelectListItem> GetTreeListItem()
        {
            return GetTreeListItem(null);
        }

        public List<SelectListItem> GetTreeListItem(int? selectedId)
        {
            var groups = _groupRepository.Table.Where(x => x.Parent == null);
            List<SelectListItem> ret = new List<SelectListItem>();

            foreach (var item in groups)
            {
                ret.Add(new SelectListItem { Text = item.Name, Value = item.Id.ToString(), Selected = (selectedId.HasValue && selectedId == item.Id ? true : false) });
                ret = GetTreeListItem(item, ret, "--", selectedId);
            }
            return ret;
        }

        private List<SelectListItem> GetTreeListItem(GroupRecord group, List<SelectListItem> ddList, string offset, int? selectedId)
        {


            var childs = _groupRepository.Table.Where(x => x.Parent.Id == group.Id).OrderBy(x => x.Wlft);
            foreach (var item in childs)
            {
                ddList.Add(new SelectListItem { Text = offset + item.Name, Value = item.Id.ToString(), Selected = (selectedId.HasValue && selectedId == item.Id ? true : false) });
                ddList = GetTreeListItem(item, ddList, offset + "--", selectedId);
            }

            return ddList;
        }


        public bool ChangeActiveGroup(int groupId, bool active)
        {
            try
            {
                var groupRecord = GetGroup(groupId);

                if (groupRecord != null)
                {
                    groupRecord.Active = active;
                    groupRecord.UpdateDate = DateTime.Now;
                    TriggerSignal();
                    return true;
                }
            }
            catch(Exception ex)
            {
                Logger.Warning(ex, "Change public for group record failed");
            }
            return false;
        }

        public GroupRecord CopyGroup(int groupId, int targetGroup)
        {
            return CopyGroup(GetGroup(groupId), targetGroup);
        }

        public GroupRecord CopyGroup(GroupRecord group, int targetGroup)
        {
            if (group == null) return null;

            string path = GetGroupPath(group);
            string getGroupPath = GetGroupPath(GetGroup(targetGroup)) ?? "";
            string targetPath = Path.Combine(getGroupPath, group.Name);
            string name = group.Name;

            if (path == targetPath)
            {
                targetPath = targetPath + CopySufixParamName;
                name = name + CopySufixParamName;
            }

            targetPath = HttpContext.Current.Server.MapPath(FilePublicPath(targetPath));
            int counter = 1;
            var tmpTargetPath = targetPath;
            var tmpName = name;
            while (Directory.Exists(targetPath))
            {
                name = tmpName + string.Format("({0})", counter);
                targetPath = tmpTargetPath + string.Format("({0})", counter);
                counter++;
            }

            var parentGroup = CreateGroup(group.Active, group.Description, name, GetGroup(targetGroup),true);

            if (parentGroup != null)
            {
                var groupPermissions = GetRecordPermission(group.Id, (int)PermisionRecordType.GroupRecordType).Select(x => x.RoleId).ToList();
                if (groupPermissions != null && groupPermissions.Count() > 0)
                {
                    UpdatePermissions((int)PermisionRecordType.GroupRecordType, parentGroup.Id, groupPermissions);
                }
            }

            TriggerSignal();

            var files = GetFiles().Where(x => x.Groups.Any(y => y.GroupRecord.Id == group.Id)).ToList();
            foreach (var file in files) 
            {
                CopyFile(file, group, parentGroup);
                //CreateFileRecord(file.Name, file.Active,file.Description, file.Size, parentGroup);
            }

            TriggerSignal();

            var groups = GetGroups().Where(x => x.Parent != null && x.Parent == group).ToList();
            foreach (var item in groups)
            {
                CopyItemsOfGroup(item, parentGroup);
            }

            TriggerSignal();
            RegroupTree();
            return parentGroup;
        }

        protected void CopyItemsOfGroup(GroupRecord group, GroupRecord parentGroup)
        {
            if (group == null || parentGroup == null) return;

            string path = GetGroupPath(group);
            string targetPath = Path.Combine(GetGroupPath(parentGroup), group.Name);
            string name = group.Name;

            if (path == targetPath)
            {
                targetPath = targetPath + CopySufixParamName;
                name = name + CopySufixParamName;
            }

            targetPath = HttpContext.Current.Server.MapPath(FilePublicPath(targetPath));
            int counter = 1;
            var tmpTargetPath = targetPath;
            var tmpName = name;
            while (Directory.Exists(targetPath))
            {
                name = tmpName + string.Format("({0})", counter);
                targetPath = tmpTargetPath + string.Format("({0})", counter);
                counter++;
            }

            var newParentGroup = CreateGroup(group.Active, group.Description, name, parentGroup,true);

            if (newParentGroup != null)
            {
                var groupPermissions = GetRecordPermission(group.Id, (int)PermisionRecordType.GroupRecordType).Select(x => x.RoleId).ToList();
                if (groupPermissions != null && groupPermissions.Count() > 0)
                {
                    UpdatePermissions((int)PermisionRecordType.GroupRecordType, newParentGroup.Id, groupPermissions);
                }
            }

            TriggerSignal();

            var files = GetFiles().Where(x => x.Groups.Any(y => y.GroupRecord.Id == group.Id)).ToList();
            foreach (var file in files)
            {
                CopyFile(file, group, newParentGroup);
                //CreateFileRecord(file.Name, file.Active, file.Description, file.Size, newParentGroup);
            }

            TriggerSignal();

            var groups = GetGroups().Where(x => x.Parent != null && x.Parent == group).ToList();
            foreach (var item in groups)
            {
                CopyItemsOfGroup(item, newParentGroup);
            }
            
        }

        public GroupRecord MoveGroup(int groupId, int targetGroup)
        {
            return MoveGroup(GetGroup(groupId), targetGroup);
        }

        public GroupRecord MoveGroup(GroupRecord group, int targetGroup)
        {
            if (group == null) return null;

            string path = GetGroupPath(group);
            string getGroupPath = GetGroupPath(GetGroup(targetGroup)) ?? "";
            string targetPath = Path.Combine(getGroupPath, group.Name);
            string name = group.Name;

            if (path == targetPath)
            {
                return group;
            }

            targetPath = HttpContext.Current.Server.MapPath(FilePublicPath(targetPath));
            path = HttpContext.Current.Server.MapPath(FilePublicPath(path));
            
            if (Directory.Exists(targetPath))
            {
                return group;
            }

            Directory.Move(path, targetPath);

            group.Parent = GetGroup(targetGroup);
            group.Level = GetGroup(targetGroup).Level + 1;


            TriggerSignal();
            RegroupTree();

            return group;
        }

        public long GetGroupSize(int groupId)
        {
            return GetGroupSize(GetGroup(groupId));
        }

        public long GetGroupSize(GroupRecord group)
        {
            if (group == null) return 0;
            else
            {
                try
                {
                    var path = GetGroupPath(group.Parent);
                    var size = _storageProvider.ListFolders(path).Where(x => x.GetName() == group.Name).Select(x => x.GetSize()).FirstOrDefault();
                    return size;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Get group size failed.");
                }
                return 0;
            }
        }


        #endregion

        #region IFilesService Members
        
        public IEnumerable<FileRecord> GetFiles()
        {
            return _fileRepository.Table.ToList();
        }

        public FileRecord GetFile(int fileId)
        {
            return _fileRepository.Get(x => x.Id == fileId);
        }

        public FileRecord GetFileByName(string fileName)
        {
            return _fileRepository.Get(x => x.Name == fileName);
        }

        public bool DeleteFile(int fileId, int groupId)
        {
            return DeleteFile(fileId, groupId, true);
        }

        public bool DeleteFile(int fileId, int groupId, bool deleteFile)
        {
            try
            {
                var fileRecord = GetFile(fileId);
                var groupRecord = GetGroup(groupId);
                var fileGroupsRecord = _filesGroupRepository.Table.Where(x => x.FileRecord.Id == fileId && x.GroupRecord.Id == groupId);

                if (fileRecord != null && groupRecord != null)
                {
                    var path = GetGroupPath(groupRecord);
                    if (deleteFile)
                        _mediaService.DeleteFile(path,fileRecord.Name);
                    foreach (var item in fileGroupsRecord)
                    {
                        _filesGroupRepository.Delete(item);
                    }

                    DeleteRecordPermissions(fileId, (int)PermisionRecordType.FileRecordType);

                    _fileRepository.Delete(fileRecord);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Delete file record failed.");
            }
            return false;
        }

        public string UploadFile(string folderPath, HttpPostedFileBase postedFile, bool extractZip, GroupRecord parentGroup)
        {
            string path = null;
            try
            {
                path = _mediaService.UploadMediaFile(folderPath, postedFile, extractZip);
                if (!extractZip)
                {
                    var record = CreateFileRecord(postedFile.FileName, true, null, postedFile.ContentLength, parentGroup);
                    if (record == null)
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Upload file record failed.");
                return null;
            }

            return path;
        }

        private FileRecord CreateFileRecord(string name, bool active, string description, long size, GroupRecord parentGroup)
        {
            FileRecord result;
            try
            {
                result = _fileRepository.Get(x => x.Name == name && x.Groups.Any(y => y.GroupRecord.Id == parentGroup.Id));
                if (result == null)
                {
                    name = FilesGroupsHelpers.TtrimName(name);
                    var ext = Path.GetExtension(name).Length > 0 ?  Path.GetExtension(name).ToLower().Substring(1) : "";
                    result = new FileRecord
                    {
                        Name = name,
                        Active = active,
                        Alias = name.Trim().ToLower(),
                        CreateDate = DateTime.Now,
                        Description = description,
                        Size = size,
                        Ext = ext,
                        ContentItemRecord = new Orchard.ContentManagement.Records.ContentItemRecord(),
                        CreatorId = _orchardServices.WorkContext.CurrentUser.Id,
                    };
                    _fileRepository.Create(result);

                    var fileGroup = new FilesGroupsRecord
                    {
                        FileRecord = result,
                        GroupRecord = parentGroup,
                        ContentItemRecord = new Orchard.ContentManagement.Records.ContentItemRecord()
                    };
                    _filesGroupRepository.Create(fileGroup);
                    TriggerSignal();
                }
            }
            catch (Exception ex)
            {
                result = null;
                Logger.Warning(ex, "Faild to upload new file.");
            }

            return result;
        }

        public bool UpdateFile(int fileId, FileRecord file, int groupId)
        {
            try
            {
                if (file == null)
                    return false;

                var fileRecord = GetFile(fileId);
                var groupRecord = GetGroup(groupId);

                if (fileRecord != null && groupRecord != null)
                {
                    file.Name = FilesGroupsHelpers.TtrimName(file.Name);

                    if (string.IsNullOrWhiteSpace(file.Description))
                        file.Description = null;
                    else
                        file.Description = file.Description.Trim().ToString();

                    var path = GetGroupPath(groupRecord);
                    if (fileRecord.Name != file.Name)
                        _mediaService.RenameFile(path, fileRecord.Name, file.Name);

                    fileRecord.Name = file.Name;
                    fileRecord.Ext = Path.GetExtension(file.Name).Length > 0 ? Path.GetExtension(file.Name).ToLower().Substring(1) :  "";
                    fileRecord.Active = file.Active;
                    fileRecord.Description = file.Description;
                    fileRecord.UpdateDate = DateTime.Now;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update file record failed");
            }
            return false;
        }

        public string FilePublicPath(string path)
        {
            return _mediaService.GetPublicUrl(path);
        }

        public bool ChangeActiveFile(int fileId, bool active)
        {
            try
            {
                var fileRecord = GetFile(fileId);

                if (fileRecord != null)
                {
                    fileRecord.Active = active;
                    fileRecord.UpdateDate = DateTime.Now;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Change public for file record failed");
            }
            return false;
        }

        public FileRecord CopyFile(int fileId, int groupId, int targetGroup)
        {
            return CopyFile(GetFile(fileId), groupId, targetGroup);
        }

        public FileRecord CopyFile(FileRecord file, int groupId, int targetGroup)
        {
            return CopyFile(file, GetGroup(groupId), GetGroup(targetGroup));
        }

        public FileRecord CopyFile(FileRecord file, GroupRecord group, GroupRecord targetGroup)
        {
            if (file == null) return null;

            string path = GetGroupPath(group);
            string targetPath = GetGroupPath(targetGroup);
            string name = file.Name;
            if (path == targetPath)
            {
                int indexOfDot = name.LastIndexOf(".");
                if (indexOfDot < 0) indexOfDot = 0;
                name = name.Substring(0, indexOfDot) + CopySufixParamName + name.Substring(indexOfDot);
            }

            targetPath = HttpContext.Current.Server.MapPath(FilePublicPath(Path.Combine(targetPath ?? "", name)));
            int counter = 1;
            string tmpName = name;
            while (File.Exists(targetPath))
            {
                name = tmpName;
                int indexOfDot = name.LastIndexOf(".");
                if (indexOfDot < 0) indexOfDot = 0;
                name = name.Substring(0, indexOfDot) + string.Format("({0})", counter) + name.Substring(indexOfDot);
                counter++;
                targetPath = HttpContext.Current.Server.MapPath(FilePublicPath(Path.Combine(GetGroupPath(targetGroup) ?? "", name)));
            }

            path = HttpContext.Current.Server.MapPath(FilePublicPath(Path.Combine(path ?? "", file.Name)));
            
            File.Copy(path, targetPath, true);

            var ret = CreateFileRecord(name, file.Active, file.Description, file.Size, targetGroup);

            if (ret != null)
            {
                var filePermissions = GetRecordPermission(file.Id, (int)PermisionRecordType.FileRecordType).Select(x => x.RoleId).ToList();
                if (filePermissions != null && filePermissions.Count() > 0)
                {
                    UpdatePermissions((int)PermisionRecordType.FileRecordType, ret.Id, filePermissions);
                }
            }

            return ret;
        }


        public FileRecord MoveFile(int fileId, int groupId, int targetGroup)
        {
            return MoveFile(GetFile(fileId), groupId, targetGroup);
        }

        public FileRecord MoveFile(FileRecord file, int groupId, int targetGroup)
        {
            if (file == null) return null;

            string path = GetGroupPath(GetGroup(groupId)) ?? "";
            string targetPath = GetGroupPath(GetGroup(targetGroup)) ?? "";
            string name = file.Name;
            if (path == targetPath)
            {
                return file;
            }

            targetPath = HttpContext.Current.Server.MapPath(FilePublicPath(Path.Combine(targetPath, name)));
            
            string tmpName = name;
            if (File.Exists(targetPath))
            {
                return file;
            }

            path = HttpContext.Current.Server.MapPath(FilePublicPath(Path.Combine(path, file.Name)));

            File.Move(path, targetPath);

            var updateGroup = file.Groups.Where(x => x.GroupRecord.Id == groupId).FirstOrDefault();

            updateGroup.GroupRecord = GetGroup(targetGroup);
            TriggerSignal();

            return file;
        }

        #endregion

        #region IModuleService Members
        public IEnumerable<FileManagerRecord> GetModules()
        {
            return _moduleRepository.Table.ToList();
        }

        public FileManagerRecord GetModule(int moduleId)
        {
            return _moduleRepository.Get(moduleId);
        }

        public bool UpdateModule(int moduleId, int groupId, int ShowType, bool HideGroups, bool HideFilterPanel)
        {
            try
            {
                var group = GetGroup(groupId);
                return UpdateModule(moduleId, group, ShowType, HideGroups, HideFilterPanel);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update module settings failed.");
            }
            return false;
        }

        public bool UpdateModule(int moduleId, GroupRecord group, int ShowType, bool HideGroups, bool HideFilterPanel)
        {
            try
            {
                var newModule = new FileManagerRecord();

                newModule.HideGroups = HideGroups;
                newModule.HideFilterPanel = HideFilterPanel;
                newModule.ShowType = ShowType;
                newModule.ParentGroup = group;

                return UpdateModule(moduleId, newModule);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update module settings failed.");
            }
            return false;
        }


        public bool UpdateModule(int moduleId, FileManagerRecord newModule)
        {
            try
            {
                var moduleRecord = GetModule(moduleId);

                if (newModule != null && newModule.ParentGroup != null && moduleRecord != null)
                {
                    moduleRecord.ParentGroup = newModule.ParentGroup;
                    moduleRecord.ShowType = newModule.ShowType;
                    moduleRecord.HideGroups = newModule.HideGroups;
                    moduleRecord.HideFilterPanel = newModule.HideFilterPanel;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update module settings failed.");
            }
            return false;
        }

        public bool DeleteModule(int moduleId)
        {
            try
            {
                var module = _moduleRepository.Get(moduleId);
                _moduleRepository.Delete(module);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Delete module settings failed.");
            }
            return false;
        }
    #endregion

        #region ISettingsService Members
        
        public IEnumerable<FileManagerSettingsRecord> GetFileSettings()
        {
            return _settingsRepository.Table.ToList();
        }

        public FileManagerSettingsRecord GetFileSetting(int systemType)
        {
            return _settingsRepository.Table.Where(x => x.SystemType == systemType).FirstOrDefault();

        }

        public bool UpdateFileSettings(int systemType, string extensions)
        {
            try
            {
                var settings = GetFileSetting(systemType);
                if (settings != null)
                    settings.FileExtensions = extensions;
                else
                {
                    var settP = new FileManagerSettingsRecord()
                    {
                        FileExtensions = extensions,
                        SystemType = systemType,
                        ContentItemRecord = new Orchard.ContentManagement.Records.ContentItemRecord()
                    };
                    _settingsRepository.Create(settP);
                }
                return true;
            }
            catch (Exception ex) { Logger.Error(ex, "Faild to update settings."); }
            return false;
        }

        public void CreateDefaultSettings()
        {
            try 
            {
                
                var settings = GetFileSetting((int)FileManagerSettingsTypes.pictures);
                if (settings == null) 
                {
                    var settP = new FileManagerSettingsRecord()
                    {
                        FileExtensions = FilesGroupsHelpers.DefaultPicturesExParamName,
                        SystemType = (int)FileManagerSettingsTypes.pictures,
                        ContentItemRecord = new Orchard.ContentManagement.Records.ContentItemRecord()
                    };
                    _settingsRepository.Create(settP);
                }
                    

                settings = GetFileSetting((int)FileManagerSettingsTypes.documents);
                if (settings == null)
                {
                    var settD = new FileManagerSettingsRecord() {
                        FileExtensions = FilesGroupsHelpers.DefaultDocumentsExParamName,
                        SystemType = (int)FileManagerSettingsTypes.documents,
                        ContentItemRecord = new Orchard.ContentManagement.Records.ContentItemRecord()
                    };
                    _settingsRepository.Create(settD);
                }
                    

                settings = GetFileSetting((int)FileManagerSettingsTypes.video);
                if (settings == null)
                {
                    var settV = new FileManagerSettingsRecord()
                    {
                        FileExtensions = FilesGroupsHelpers.DefaultVideoExParamName,
                        SystemType = (int)FileManagerSettingsTypes.video,
                        ContentItemRecord = new Orchard.ContentManagement.Records.ContentItemRecord()
                    };
                    _settingsRepository.Create(settV);
                }

                TriggerSignal();
            }
            catch (Exception ex) {
                Logger.Error(ex, "Load file settings failed.");
            }
        }

        public FileManagerSettingsTypes GetFileType(string fileExt)
        {
            var types = GetFileSettings(); 
            foreach (var item in types)
            {
                var extensions = item.FileExtensions.Split(' ');
                if (extensions.Contains(fileExt))
                    return (FileManagerSettingsTypes)item.SystemType;
            }
            return FileManagerSettingsTypes.none;
        }

        public MediaSettingsPartRecord GetFileUploadFilesWhiteList()
        {
            return _mediaSettingsRepository.Table.FirstOrDefault();
        }

        public void UpdateFileUploadWhiteList(string whiteList)
        {
            var uplWhiteList = _mediaSettingsRepository.Table.FirstOrDefault();
            if (uplWhiteList != null)
            {
                uplWhiteList.UploadAllowedFileTypeWhitelist = whiteList;
            }
            TriggerSignal();
        }

        #endregion
        
        #region IPermissionService Members
        
        public IEnumerable<PermissionRecord> GetPermissions()
        {
            return _permissionRepository.Table.ToList();
        }

        public IEnumerable<PermissionRecord> GetRecordPermission(int recId, int systemType)
        {
            return _permissionRepository.Table.Where(x => x.RecordId == recId && x.SystemType == systemType).ToList();
        }

        public bool UpdatePermissions(int systemType, int recId, List<int> roleIds)
        {
            bool ret = true;
            try
            {
                var corrDel = DeleteRecordPermissions(systemType, recId, roleIds);
                if (corrDel)
                {
                    var curRoles = GetRecordPermission(recId, systemType).Select(x => x.RoleId).ToList();
                    var newRoles = roleIds.Where(x => !curRoles.Contains(x)).ToList();

                    foreach (var role in newRoles)
                    {
                        var roleRec = CreatePermissionRecord(systemType, recId, role);
                        if (roleRec == null)
                            ret = false;
                    }

                    TriggerSignal();
                }
                else
                {
                    ret = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Faild to update permissions.");
                ret = false; 
            }
            return ret;
        }

        public bool DeleteRecordPermissions(int systemTpe, int recId)
        {
            try
            {
                var permissions = GetRecordPermission(recId, systemTpe);
                if (permissions != null && permissions.Count() > 0)
                {
                    foreach (var permission in permissions)
                    {
                        _permissionRepository.Delete(permission);
                    }
                    TriggerSignal();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Faild to delete permissions.");
            }
            return false;
        }

        public bool DeleteRecordPermissions(int systemTpe, int recId, List<int> roleIds)
        {
            try
            {
                var permissions = GetRecordPermission(recId, systemTpe).Where(x => !roleIds.Contains(x.RoleId)).ToList();

                if (permissions != null && permissions.Count() > 0)
                {
                    foreach (var permission in permissions)
                    {
                        _permissionRepository.Delete(permission);
                    }
                    TriggerSignal();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Faild to delete permissions.");
            }
            return false;
        }

        public PermissionRecord CreatePermissionRecord(int systemType, int recId, int roleId)
        {
            PermissionRecord result;
            try
            {
                result = _permissionRepository.Get(x => x.RoleId == roleId && x.RecordId == recId && x.SystemType == systemType);
                if (result == null)
                {
                    result = new PermissionRecord
                    {
                        RecordId = recId, RoleId = roleId, SystemType = systemType,
                        ContentItemRecord = new Orchard.ContentManagement.Records.ContentItemRecord(),
                    };
                    _permissionRepository.Create(result);
                    TriggerSignal();
                }
            }
            catch (Exception ex)
            {
                result = null;
                Logger.Error(ex, "Faild to create new permissions.");
            }

            return result;
        }

        public List<int> GetPermissionsForGroup(int groupId)
        {
            List<int> ret = new List<int>();
            try
            {
                var group = GetGroup(groupId);
                if (group != null)
                {
                    while (group.Parent != null)
                    {
                        var gropuPerm = GetRecordPermission(group.Id, (int)PermisionRecordType.GroupRecordType).Select(x => x.RoleId).ToList();
                        if (gropuPerm != null && gropuPerm.Count > 0)
                            ret = ret.Concat(gropuPerm).Distinct().ToList();
                        group = group.Parent;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = new List<int>();
                Logger.Error(ex, "Faild to load group permissions.");
            }

            return ret;
        }

        #endregion

        private void TriggerSignal()
        {
            _signals.Trigger(SignalName);
        }
    }
}
