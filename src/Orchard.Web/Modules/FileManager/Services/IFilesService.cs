using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Orchard.ContentManagement;
using FileManager.Models;
using Orchard;

namespace FileManager.Services
{
    public interface IFilesService : IDependency
    {
        /// <summary>
        /// get all records of files table
        /// </summary>
        /// <returns></returns>
        IEnumerable<FileRecord> GetFiles();

        /// <summary>
        /// get file by id
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        FileRecord GetFile(int fileId);
        
        /// <summary>
        /// Upload file, and create file record
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="postedFile"></param>
        /// <param name="extractZip"></param>
        /// <param name="parentGroup"></param>
        /// <returns></returns>
        string UploadFile(string folderPath, HttpPostedFileBase postedFile, bool extractZip, GroupRecord parentGroup);

        /// <summary>
        /// update filerecord values
        /// </summary>
        /// <param name="fileId">updated file</param>
        /// <param name="file">new values</param>
        /// <param name="groupId">group where is situated</param>
        /// <returns></returns>
        bool UpdateFile(int fileId, FileRecord file, int groupId);
        
        /// <summary>
        /// delete file record
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="groupId">group where is file situated</param>
        /// <returns></returns>
        bool DeleteFile(int fileId, int groupId);

        /// <summary>
        /// delete filre record
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="groupId">group where is file situated</param>
        /// <param name="deleteFile">delete file on disc</param>
        /// <returns></returns>
        bool DeleteFile(int fileId, int groupId, bool deleteFile);
        
        /// <summary>
        /// public path of file, path from the root media folder
        /// </summary>
        /// <param name="path">path to root media folder</param>
        /// <returns></returns>
        string FilePublicPath(string path);

        /// <summary>
        /// change publish flag
        /// </summary>
        /// <param name="fileId">file</param>
        /// <param name="active"></param>
        /// <returns></returns>
        bool ChangeActiveFile(int fileId, bool active);

        /// <summary>
        /// Copy file to selected group, if file in selected group exist, file name will be name-Copy / name-Copy(1)...
        /// </summary>
        /// <param name="fileId">file to copy</param>
        /// <param name="groupId">file from this group</param>
        /// <param name="targetGroup">to this group</param>
        /// <returns></returns>
        FileRecord CopyFile(int fileId, int groupId, int targetGroup);

        /// <summary>
        /// Copy file to selected group, if file in selected group exist, file name will be name-Copy / name-Copy(1)...
        /// </summary>
        /// <param name="file">file to copy</param>
        /// <param name="groupId">file from this group</param>
        /// <param name="targetGroup">to this group</param>
        /// <returns></returns>
        FileRecord CopyFile(FileRecord file, int groupId, int targetGroup);

        /// <summary>
        /// move file to selected group, if file in selected group exist, do nothing
        /// </summary>
        /// <param name="fileId">file to copy</param>
        /// <param name="groupId">file from this group</param>
        /// <param name="targetGroup">to this group</param>
        /// <returns></returns>
        FileRecord MoveFile(int fileId, int groupId, int targetGroup);

        /// <summary>
        /// move file to selected group, if file in selected group exist, do nothing
        /// </summary>
        /// <param name="file">file to copy</param>
        /// <param name="groupId">file from this group</param>
        /// <param name="targetGroup">to this group</param>
        /// <returns></returns>
        FileRecord MoveFile(FileRecord file, int groupId, int targetGroup);
    }
}