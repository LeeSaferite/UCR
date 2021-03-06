﻿using System;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using HidWizards.UCR.Core.Utilities.AxisHelpers;

namespace HidWizards.UCR.Plugins.Remapper
{
    [Plugin("Axis Merger")]
    [PluginInput(DeviceBindingCategory.Range, "Axis high")]
    [PluginInput(DeviceBindingCategory.Range, "Axis low")]
    [PluginOutput(DeviceBindingCategory.Range, "Axis")]
    public class AxisMerger : Plugin
    {
        [PluginGui("Dead zone", ColumnOrder = 1)]
        public int DeadZone { get; set; }

        [PluginGui("Mode", ColumnOrder = 0)]
        public AxisMergerMode Mode { get; set; }

        [PluginGui("Invert high", RowOrder = 1)]
        public bool InvertHigh { get; set; }

        [PluginGui("Invert low", RowOrder = 2)]
        public bool InvertLow { get; set; }

        private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();

        public enum AxisMergerMode
        {
            Average,
            Greatest,
            Sum
        }

        public AxisMerger()
        {
            DeadZone = 0;
        }

        public override void InitializeCacheValues()
        {
            Initialize();
        }

        public override void Update(params short[] values)
        {
            var valueHigh = values[0];
            var valueLow = values[1];
            short valueOutput;

            if (InvertHigh) valueHigh = Functions.Invert(valueHigh);
            if (InvertLow) valueLow = Functions.Invert(valueLow);

            switch (Mode)
            {
                case AxisMergerMode.Average:
                    valueOutput = (short) ((valueHigh + valueLow) / 2);
                    break;
                case AxisMergerMode.Greatest:
                    valueOutput = (Functions.SafeAbs(valueHigh) > Functions.SafeAbs(valueLow)) ? valueHigh : valueLow;
                    break;
                case AxisMergerMode.Sum:
                    valueOutput = (short) (valueHigh + valueLow);
                    break;
                default:
                    valueOutput = 0;
                    break;
            }

            if (DeadZone != 0)
            {
                valueOutput = _deadZoneHelper.ApplyRangeDeadZone(valueOutput);
            }
            WriteOutput(0, valueOutput);
        }
        
        private void Initialize()
        {
            _deadZoneHelper.Percentage = DeadZone;
        }
    }
}
