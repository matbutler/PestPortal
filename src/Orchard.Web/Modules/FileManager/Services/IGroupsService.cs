using System.Collections.Generic;
using Orchard.ContentManagement;
using FileManager.Models;
using Orchard;
using System.Web.Mvc;


namespace FileManager.Services
{
    public interface IGroupsService : IDependency
    {
        /// <summary>
        /// Create root group with empty name, level 0 and null parent
        /// </summary>
        /// <returns></returns>
        GroupRecord CreateRootGroup();

        /// <summary>
        /// Get all table of groups record
        /// </summary>
        /// <returns></returns>
        IEnumerable<GroupRecord> GetGroups();

        /// <summary>
        /// Get group record
        /// </summary>
        /// <param name="groupId">Id of GroupRecord</param>
        /// <returns></returns>
        GroupRecord GetGroup(int groupId);

        /// <summary>
        /// Create group record
        /// </summary>
        /// <param name="active">publish file</param>
        /// <param name="desc">description</param>
        /// <param name="name">file name</param>
        /// <param name="parentGroup">group where is file located</param>
        /// <returns></returns>
        GroupRecord CreateGroup(bool active, string desc, string name, GroupRecord parentGroup);

        /// <summary>
        /// Create group record
        /// </summary>
        /// <param name="active">publish file</param>
        /// <param name="desc">description</param>
        /// <param name="name">file name</param>
        /// <param name="parentGroup">group where is file located</param>
        /// <param name="createFolder">create folder physicaly on disc</param>
        /// <returns></returns>
        GroupRecord CreateGroup(bool active, string desc, string name, GroupRecord parentGroup, bool createFolder);
        
        /// <summary>
        /// Update group
        /// </summary>
        /// <param name="groupId">id of updated group</param>
        /// <param name="group">new values for updated group</param>
        /// <returns>defines whether group was updated successfully</returns>
        bool UpdateGroup(int groupId, GroupRecord group);
        
        /// <summary>
        /// Delete group
        /// </summary>
        /// <param name="groupId">id of deleted group</param>
        /// <returns>defines whether group was deleted successfully</returns>
        bool DeleteGroup(int groupId);
        
        /// <summary>
        /// Delete group
        /// </summary>
        /// <param name="groupId">id of deleted group</param>
        /// <param name="deleteFolder">delete folder on disk</param>
        /// <returns>defines whether group was deleted successfully</returns>
        bool DeleteGroup(int groupId, bool deleteFolder);

        /// <summary>
        /// Regroup tree, updated wlft and wrgt in table
        /// </summary>
        void RegroupTree();

        /// <summary>
        /// Actualize groups and files. Read all folders in default mediafolders, and update database
        /// </summary>
        void Actualize();

        /// <summary>
        /// Actualize groups and files. Read all folders in default mediafolders, and update database
        /// </summary>
        /// <param name="root">root groop</param>
        void Actualize(GroupRecord root);

        /// <summary>
        /// get group path from root ex: media/data/group
        /// </summary>
        /// <param name="parentGroup"></param>
        /// <returns></returns>
        string GetGroupPath(GroupRecord parentGroup);
        
        /// <summary>
        /// Get list items of groups for dropdownlist
        /// </summary>
        /// <returns></returns>
        List<SelectListItem> GetTreeListItem();

        /// <summary>
        /// /// Get list items of groups for dropdownlist
        /// </summary>
        /// <param name="selectedId">selected group</param>
        /// <returns></returns>
        List<SelectListItem> GetTreeListItem(int? selectedId);

        /// <summary>
        /// change publised flag
        /// </summary>
        /// <param name="groupId">group</param>
        /// <param name="active"></param>
        /// <returns></returns>
        bool ChangeActiveGroup(int groupId, bool active);

        /// <summary>
        /// copy group to group, if group in target group exist group name will be group-Copy / group-Copy(1)..
        /// </summary>
        /// <param name="groupId">group id I want to copy</param>
        /// <param name="targetGroup">copy to this group</param>
        /// <returns></returns>
        GroupRecord CopyGroup(int groupId, int targetGroup);

        /// <summary>
        /// copy group to group, if group in target group exist group name will be group-Copy / group-Copy(1)..
        /// </summary>
        /// <param name="group">group id I want to copy</param>
        /// <param name="targetGroup">copy to this group</param>
        /// <returns></returns>
        GroupRecord CopyGroup(GroupRecord group, int targetGroup);

        /// <summary>
        /// move group to another group. if group in target group exist do nothing
        /// </summary>
        /// <param name="groupId">group I want to move</param>
        /// <param name="targetGroup">move to this group</param>
        /// <returns></returns>
        GroupRecord MoveGroup(int groupId, int targetGroup);

        /// <summary>
        /// move group to another group. if group in target group exist do nothing
        /// </summary>
        /// <param name="group">group I want to move</param>
        /// <param name="targetGroup">move to this group</param>
        /// <returns></returns>
        GroupRecord MoveGroup(GroupRecord group, int targetGroup);

        /// <summary>
        /// Get size of group in bytes
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        long GetGroupSize(int groupId);

        /// <summary>
        /// Get size of group in bytes
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        long GetGroupSize(GroupRecord group);
    }
}