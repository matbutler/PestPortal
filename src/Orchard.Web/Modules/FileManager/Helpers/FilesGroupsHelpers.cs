using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Web;
using System.Web.UI;
using System.Web.Mvc;
using FileManager.Models;
using FileManager.Services;
using FileManager.ViewModels;
using Orchard.Utility.Extensions;
using Orchard;


namespace FileManager.Helpers
{
    public class FileShowTags 
    {
        public string EmbedPath { get; set; }
        public string ShowHtml { get; set; }
        public string InlineHtml { get; set; }
    }

    public static class FilesGroupsHelpers
    {

        public const string DefaultPicturesExParamName = "jpg jpeg png gif tiff tif bmp";
        public const string DefaultDocumentsExParamName = "txt doc docx xls xlsx pdf ppt pptx pps ppsx";
        public const string DefaultVideoExParamName = "avi mp4 flv wmv";
        public const string DisplaySortTypeUrlParam = "sortType";
        public const string DisplaySortUrlParam = "ascSort";
        public const string DisplaySearchTextUrlParam = "searchText";

        public const int maxImgWidth = 650;
        public const int maxImgHeight = 650;
        public const int maxPageWidth = 650;
        public const int maxPageHeight = 650;
        public const int defaultVideoWidth = 640;
        public const int defaultVideoHeight = 480;


        /// <summary>
        /// Determines if a file is a Zip Archive based on its extension.
        /// </summary>
        /// <param name="extension">The extension of the file to analyze.</param>
        /// <returns>True if the file is a Zip archive; false otherwise.</returns>
        public static bool IsZipFile(string extension)
        {
            return string.Equals(extension.TrimStart('.'), "zip", StringComparison.OrdinalIgnoreCase);
        }


        public static string TtrimName(string name)
        {
            name = name.TrimStart();
            name = name.TrimEnd();
            return name;
        }

        public static bool FileExist(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return File.Exists(HttpContext.Current.Server.MapPath(path));
        }

        public static bool IsImageBigger(string path)
        {
            var img = Image.FromFile(HttpContext.Current.Server.MapPath(path));
            return (img.Width >= maxImgWidth && img.Height >= maxImgHeight);
        }

        public static string GetImageTag(string path, FileRecord file)
        {
            return GetImageTag(path, file, false, maxImgWidth, null, null, null, null);
        }
        public static string GetImageTag(string path, FileRecord file, bool includeJs, int? width, int? height, string style, string alt, string cssClass)
        {
            string imgTag = "";
            
            var innerImgTag = string.Format("<img alt='{0}' src='{1}' width='{2}' height='{3}' style='{4}' class='{5}' />",alt ?? file.Name, path, width, height, style ?? "", cssClass ?? "");
            imgTag += string.Format("<a title='{0}' href='{1}' >{2}</a>", string.IsNullOrEmpty(file.Description) ? file.Name : file.Description, path, innerImgTag);

            imgTag = string.Format("<div class='img-gallery-lightbox'>{0}</div>", imgTag);
            if (includeJs)
            {
                var scripts = string.Format("<link href='{0}' rel='stylesheet' type='text/css'>", (new Control()).ResolveUrl("~/Modules/FileManager/Styles/jquery.lightbox.css"));
                scripts += string.Format("<script src='{0}' type='text/javascript'></script>", (new Control()).ResolveUrl("~/Modules/FileManager/scripts/jquery.lightbox.js"));
                string buttons = string.Format("imageLoading: '{0}', imageBtnPrev: '{1}', imageBtnNext: '{2}', imageBtnClose: '{3}', 	 imageBlank: '{4}'",
                    (new Control()).ResolveUrl("~/Modules/FileManager/Styles/Images/lightbox-ico-loading.gif"),
                    (new Control()).ResolveUrl("~/Modules/FileManager/Styles/Images/lightbox-btn-prev.gif"),
                    (new Control()).ResolveUrl("~/Modules/FileManager/Styles/Images/lightbox-btn-next.gif"),
                    (new Control()).ResolveUrl("~/Modules/FileManager/Styles/Images/lightbox-btn-close.gif"),
                    (new Control()).ResolveUrl("~/Modules/FileManager/Styles/Images/lightbox-blank.gif"));

                var scriptsJs = string.Format("<script type='text/javascript'> //<![CDATA[ $(function () {0}  $('.img-gallery-lightbox a').lightBox({0}{1}{2});  {2}); //]]></script>", "{", buttons, "}");
                imgTag = scripts + scriptsJs + imgTag;

            }

            return imgTag;
        }

        public static string GetPagetag(string path)
        {
            return GetPagetag(path, maxPageWidth, maxPageHeight, null, null);
        }

        public static string GetPagetag(string path, int width, int height, string style, string cssClass)
        {
            return string.Format("<iframe src='http://docs.google.com/gview?url={0}&embedded=true' class='' style='width:{1}px; height:{2}px;{3}' frameborder='0'></iframe>", GetAsoluteUrlPath(path), width, height, style ?? "", cssClass ?? "");
        }


        public static string GetExtensionImagePath(FileRecord file)
        {
            return GetExtensionImagePath(file.Ext);
        }

        public static string GetExtensionImagePath(string ext)
        {
            string path = string.Format("~/Modules/FileManager/Styles/Images/icons/{0}.png", ext);
            if (!File.Exists(HttpContext.Current.Server.MapPath(path)))
            {
                path = string.Format("~/Modules/FileManager/Styles/Images/icons/unknown.png");
            }

            return path;
        }



        public static FileShowTags GetFileElements(string path, FileRecord file, FileManagerSettingsTypes fileType)
        {
            FileShowTags ret = new FileShowTags();
            if (fileType == FileManagerSettingsTypes.pictures) 
            {
                ret.EmbedPath = GetImageTag(path, file);
                ret.ShowHtml = GetImageTag(path, file);
            }
            else if (fileType == FileManagerSettingsTypes.documents) 
            {
                ret.EmbedPath = GetPagetag(path);
                ret.ShowHtml = GetPagetag(path);
            }
            else if (fileType == FileManagerSettingsTypes.video) 
            {
                ret.ShowHtml = GetVideoTag(GetAsoluteUrlPath(path));
                ret.EmbedPath = string.Format("<a href='{0}'>{1}</a>", path, file.Name);
            }
            else 
            {
                ret.EmbedPath = string.Format("<a href='{0}'>{1}</a>", path, file.Name);
            }

            return ret;
        }

        public static string GetVideoTag(string path)
        {
            return GetVideoTag(defaultVideoWidth, defaultVideoHeight, "", "", false, path);
        }

        public static string GetVideoTag(int width, int height, string cssClass, string style, bool includeScript, string path)
        {
            var control = new Control();
            var videoId = "player" + new Random().Next(100).ToString();
            
            string script = "";
            string videoDiv = "";
            string videoScript = "";

            if (string.Equals(Path.GetExtension(path), ".wmv", StringComparison.OrdinalIgnoreCase))
            {
                if (includeScript)
                    script = string.Format("<script type='text/javascript' src='{0}'></script><script type='text/javascript' src='{1}'></script>", control.ResolveUrl("~/Modules/FileManager/Scripts/silverlight.js"), control.ResolveUrl("~/Modules/FileManager/Scripts/wmvplayer.js"));

                videoDiv = string.Format("<div id='{0}' class='{1}' style='{2}' ></div>", videoId, cssClass, style);
                videoScript = string.Format("<script type='text/javascript'> var cnt = document.getElementById('{0}'); cnt.innerHTML = ''; var src = '{1}';" +
                                        "var cfg = {5} height: '{2}',   width: '{3}', file: '{4}', bufferlength: '20', autostart: 'false' {6}; var ply = new jeroenwijering.Player(cnt, src, cfg); </script>",
                                        videoId, control.ResolveUrl("~/Modules/FileManager/Scripts/wmvplayer.xaml"), height, width, GetAsoluteUrlPath(path), "{", "}");
            }
            else
            {
                if (includeScript)
                    script = string.Format("<script type='text/javascript' src='{0}'></script>", control.ResolveUrl("~/Modules/FileManager/Scripts/flowplayer.js"));

                videoDiv = string.Format("<div style='display:block;width:{0}px;height:{1}px;{4}'id='{2}' class='{3}'></div>", width, height, videoId, cssClass, style);
                videoScript = string.Format("<script>flowplayer('{0}', '{1}', {2} clip: '{4}' {3});</script>",
                    videoId, control.ResolveUrl("~/Modules/FileManager/Scripts/flowplayer.swf"), "{", "}", GetAsoluteUrlPath(path));
            }

            return script + videoDiv + videoScript;
        }

        public static string GetAsoluteUrlPath(string virtualPath)
        {
            if (virtualPath.StartsWith("http") || virtualPath.StartsWith("ftp:")) return virtualPath;
            return HttpContext.Current.Request.ToRootUrlString() + virtualPath;
        }

        public static bool CheckValidityName(string name)
        {
            if (string.IsNullOrEmpty(name.Trim())) return false;

            return !(name.Contains("\\") || name.Contains("/") || name.Contains(":") || name.Contains("*") ||
                name.Contains("?") || name.Contains("\"") || name.Contains("<") || name.Contains(">") ||
                name.Contains("|"));
        }
        
        public static FileEntry CreateFileEntry(FileRecord fileRecord)
        {
            return new FileEntry
            {
                File = fileRecord,
                IsChecked = false,
                parentGid = fileRecord.Groups.FirstOrDefault().GroupRecord.Id 
            };
        }

        public static GroupEntry CreateGroupEntry(GroupRecord groupRecord)
        {
            return new GroupEntry
            {
                Group = groupRecord,
                IsChecked = false,
                parentGid = groupRecord.Parent != null ? groupRecord.Parent.Id : 0
            };
        }

        public static string GetDisplayGroupPath(int groupId)
        {
            var currPath = HttpContext.Current.Request.ToUrlString();

            return UrlAddParam(currPath, "sgid", groupId.ToString());

            //if (currPath.IndexOf('?') > 0)
            //{
            //    if (currPath.IndexOf("sgid") > 0)
            //    {
            //        var oldId = currPath.Substring(currPath.IndexOf("sgid"));
            //        if (oldId.IndexOf("&") > 0)
            //            oldId = oldId.Substring(0, oldId.IndexOf("&") + 1);


            //        var newId = "sgid=" + groupId.ToString();
            //        currPath = currPath.Replace(oldId, newId);
            //    }
            //    else
            //        currPath += "&sgid=" + groupId.ToString();
            //}
            //else
            //    currPath += "?sgid=" + groupId.ToString();

            //return currPath;
        }

        public static List<SelectListItem> GetShowTypesList()
        {
            return GetShowTypesList(null);
        }


        public static List<SelectListItem> GetShowTypesList(int? selectedId)
        {
            var listTypes = Enum.GetValues(typeof(DisplayType)).Cast<DisplayType>().ToList()
                                .Where(x => x != DisplayType.none && x != DisplayType.edit);
            List<SelectListItem> ret = new List<SelectListItem>();

            if (!selectedId.HasValue) selectedId = (int)DisplayType.inline;
            
            foreach (DisplayType item in listTypes)
            {
                ret.Add(new SelectListItem { Text = item.ToString(), Value = ((int)item).ToString(), 
                    Selected = (selectedId.HasValue && selectedId == (int)item ? true : false)});
            }
            return ret;
        }

        /// <summary>
        /// Add some param to url adress. If param exist update param value
        /// </summary>
        /// <param name="url"></param>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <returns>updated url</returns>
        public static string UrlAddParam(string url, string param, string value)
        {

            if (url.IndexOf('?') > 0)
            {
                if (url.IndexOf(param) > 0)
                {
                    var oldId = url.Substring(url.IndexOf(param));
                    if (oldId.IndexOf("&") > 0)
                        oldId = oldId.Substring(0, oldId.IndexOf("&"));


                    var newId = param + "=" + value;
                    url = url.Replace(oldId, newId);
                }
                else
                    url += "&" + param + "=" + value;
            }
            else
                url += "?" + param + "=" + value;

            return url;
        }

        /// <summary>
        /// Remove some param from url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="param">param to remove</param>
        /// <returns></returns>
        public static string UrlRemoveParam(string url, string param)
        {
            if (url.IndexOf('?') > 0)
            {
                if (url.IndexOf(param) > 0)
                {
                    var oldId = url.Substring(url.IndexOf(param));
                    if (oldId.IndexOf("&") > 0)
                        oldId = oldId.Substring(0, oldId.IndexOf("&"));
                    if (url.IndexOf(param) > 0 && url[url.IndexOf(param) - 1] == '&')
                        oldId = "&" + oldId;

                    url = url.Replace(oldId, "");
                }
            }
            return url;
        }

        public static string GetDisplaySortingUrl(bool asc)
        {
            var currPath = HttpContext.Current.Request.ToUrlString();
            currPath = UrlAddParam(currPath, DisplaySortUrlParam, asc.ToString().ToLower());
            return currPath;
        }

        public static string GetDisplaySortingTypeUrl(string param, string replaceString)
        {
            var currPath = HttpContext.Current.Request.ToUrlString();
            currPath = UrlAddParam(currPath, param, replaceString);
            return currPath;
        }


        public static string UrlRemoveParamFromCurrUrl(string param)
        {
            var currPath = HttpContext.Current.Request.ToUrlString();
            return UrlRemoveParam(currPath, param);
        }
    }

    public static class LongExtensions
    {
        private static readonly List<string> units = new List<string>(5) { "B", "KB", "MB", "GB", "TB" }; // Not going further. Anything beyond MB is probably overkill anyway.

        public static string ToFriendlySizeString(this long bytes)
        {
            var somethingMoreFriendly = TryForTheNextUnit(bytes, units[0]);
            var roundingPlaces = units[0] == somethingMoreFriendly.Item2 ? 0 : units.IndexOf(somethingMoreFriendly.Item2) - 1;
            return string.Format("{0} {1}", Math.Round(somethingMoreFriendly.Item1, roundingPlaces), somethingMoreFriendly.Item2);
        }

        private static Tuple<double, string> TryForTheNextUnit(double size, string unit)
        {
            var indexOfUnit = units.IndexOf(unit);

            if (size > 1024 && indexOfUnit < units.Count - 1)
            {
                size = size / 1024;
                unit = units[indexOfUnit + 1];
                return TryForTheNextUnit(size, unit);
            }

            return new Tuple<double, string>(size, unit);
        }
    }

}