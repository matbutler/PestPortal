﻿using System.Linq;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;

namespace Orchard.Glimpse.Tabs.Cache {
    public class CacheTab : TabBase, ITabSetup, IKey, ILayoutControl {
        public override object GetData(ITabContext context) {
            var messages = context.GetMessages<CacheMessage>().ToList();

            if (!messages.Any()) {
                return null;
            }

            return messages;
        }

        public override string Name => "Cache Service";

        public void Setup(ITabSetupContext context) {
            context.PersistMessages<CacheMessage>();
        }

        public string Key => "glimpse_orchard_cache";

        public bool KeysHeadings => false;
    }
}