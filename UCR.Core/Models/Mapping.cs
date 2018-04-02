﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml.XPath;
using HidWizards.UCR.Core.Models.Binding;

namespace HidWizards.UCR.Core.Models
{
    public class Mapping
    {
        // Persistence
        public String Title { get; set; }
        public Guid Guid { get; set; }
        public List<DeviceBinding> DeviceBindings { get; set; }
        public List<Plugin> Plugins { get; set; }

        // Runtime
        internal Profile Profile { get; set; }
        private List<long> InputCache { get; set; }

        [XmlIgnore]
        public string FullTitle
        {
            get
            {
                var mapping = GetOverridenMapping();
                return mapping != null ? $"{Title} (Overrides {GetOverridenMapping().Profile.Title})" : Title;
            }
        }

        public Mapping()
        {
            Guid = Guid.NewGuid();
            DeviceBindings = new List<DeviceBinding>();
            Plugins = new List<Plugin>();
        }

        public Mapping(Profile profile, string title) : this()
        {
            Profile = profile;
            Title = title;
        }

        internal bool IsBound()
        {
            if (DeviceBindings.Count == 0) return false;
            var result = true;
            foreach (var deviceBinding in DeviceBindings)
            {
                result &= deviceBinding.IsBound;
            }
            return result;
        }

        internal void PrepareMapping()
        {
            InputCache = new List<long>();
            foreach (var deviceBinding in DeviceBindings)
            {
                deviceBinding.Callback = Update;
                InputCache.Add(0L);
            }
        }

        internal Mapping GetOverridenMapping()
        {
            var list = new List<Mapping>();
            var parentProfile = Profile.ParentProfile;
            if (parentProfile != null) list.AddRange(parentProfile.Mappings);

            while (list.Count > 0)
            {
                var mapping = list[0];
                list.RemoveAt(0);
                if (string.Compare(Title, mapping.Title, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    return mapping;
                }
                parentProfile = parentProfile?.ParentProfile;
                if (parentProfile != null) list.AddRange(parentProfile.Mappings);
            }

            return null;
        }

        // TODO Add Guid to distinguish devicebindings
        public void Update(long value)
        {
            InputCache[0] = value;
            foreach (var plugin in Plugins)
            {
                if (plugin.State == Guid.Empty || Profile.GetRuntimeState(plugin.State))
                {
                    plugin.Update(InputCache.ToArray());
                }
            }
        }

        #region Plugin

        internal List<Plugin> GetPluginList()
        {
            var plugins = Profile.Context.GetPlugins();
            plugins.Sort();
            if (Plugins.Count > 0)
            {
                plugins = plugins.FindAll(p => p.HasSameInputCategories(Plugins[0]));
            }
            return plugins;
        }

        public bool AddPlugin(Plugin plugin)
        {
            if (Plugins.Count == 0)
            {
                foreach (var _ in plugin.InputCategories)
                {
                    DeviceBindings.Add(new DeviceBinding(Update, Profile, DeviceIoType.Input));
                }
            }
            
            plugin.SetProfile(Profile);
            Plugins.Add(plugin);
            Profile.Context.ContextChanged();
            return true;
        }

        public bool RemovePlugin(Plugin plugin)
        {
            if (!Plugins.Remove(plugin)) return false;

            if (Plugins.Count == 0)
            {
                DeviceBindings = new List<DeviceBinding>();
            }
            Profile.Context.ContextChanged();

            return true;
        }

        #endregion


        internal void PostLoad(Context context, Profile profile = null)
        {
            Profile = profile;
            foreach (var deviceBinding in DeviceBindings)
            {
                deviceBinding.Profile = profile;
            }

            foreach (var plugin in Plugins)
            {
                plugin.PostLoad(context, profile);
            }
        }

        internal void InitializeMappings(int amount)
        {
            DeviceBindings = new List<DeviceBinding>();
            for (var i = 0; i < amount; i++)
            {
                DeviceBindings.Add(new DeviceBinding(Update, Profile, DeviceIoType.Input));
            }
        }
    }
}