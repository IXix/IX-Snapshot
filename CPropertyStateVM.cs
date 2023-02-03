using BuzzGUI.Common;
using BuzzGUI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Snapshot
{
    public class CPropertyStateVM : CMachinePropertyItemVM
    {
        readonly IPropertyState _property;

        public CPropertyStateVM(IPropertyState property, CTreeViewItemVM parent, CSnapshotMachineVM ownerVM, int view)
            : base(parent, false, ownerVM, view)
        {
            _property = property;
            _properties.Add(_property);

            IsChecked = _property.Checked;
            IsCheckedM = _property.Checked_M;

            _property.CheckChanged += CheckChanged;
            _property.StateChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            OnPropertyChanged("DisplayName");
        }
        public override bool GotValue
        {
            get
            {
                try
                {
                    return ReferenceSlot().ContainsProperty(_property);
                }
                catch
                {
                    return false;
                }
            }
        }

        public override string DisplayValue
        {
            get
            {
                return ReferenceSlot().GetPropertyDisplayValue(_property);
            }
        }

        protected override void OnCheckChanged()
        {
            _property.Checked = (bool)IsChecked;
            _property.Checked_M = (bool)IsCheckedM;
            _property.OnStateChanged();
        }

        public override string Name => _property.Name;

        public override string DisplayName => _property.DisplayName;

        public override int? SmoothingCount => _property.SmoothingCount;

        public override int? SmoothingUnits => _property.SmoothingUnits;
    }
}