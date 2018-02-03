using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using FileManager.Models;

namespace FileManager.Services
{
    public interface IPermissionService : IDependency
    {
        IEnumerable<PermissionRecord> GetPermissions();
        IEnumerable<PermissionRecord> GetRecordPermission(int recId, int systemType);
        bool UpdatePermissions(int systemType, int recId, List<int> roleIds);
        bool DeleteRecordPermissions(int systemTpe, int recId);
        bool DeleteRecordPermissions(int systemTpe, int recId, List<int> roleIds);
        PermissionRecord CreatePermissionRecord(int systemType, int recId, int roleId);
        List<int> GetPermissionsForGroup(int groupId);
    }
}


