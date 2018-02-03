using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;

namespace River.SecureFileStorage.Models
{
    public class SecureFileStorageSettingsPart : ContentPart<SecureFileStorageSettingsPartRecord>
    {

        public string RootStorageDir { get { return Record.RootStorageDir; } set { Record.RootStorageDir = value + ( ( value != null && value.EndsWith("\\") ) ? "" : "\\" ); } }

    }
}