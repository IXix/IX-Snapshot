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
    public class CPropertyStateVM : CTreeViewItemVM
    {
        readonly IPropertyState _property;

        public CPropertyStateVM(IPropertyState property, CTreeViewItemVM parent, CSnapshotMachineVM ownerVM)
            : base(parent, false, ownerVM)
        {
            _property = property;
            IsChecked = _property.Selected;
            _property.SelChanged += OnSelChanged;
            _property.SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            OnPropertyChanged("DisplayName");
        }

        public override bool GotValue
        {
            get { return _ownerVM.CurrentSlot.ContainsProperty(_property); }
        }

        public override bool GotValueA
        {
            get { return _ownerVM.SlotA.ContainsProperty(_property); }
        }

        public override bool GotValueB
        {
            get { return _ownerVM.SlotB.ContainsProperty(_property); }
        }

        protected override void OnCheckChanged()
        {
            _property.Selected = (bool)IsChecked;
            _property.OnSelChanged(new StateChangedEventArgs() { Property = _property, Checked = _property.Selected });
        }

        public string Name => _property.Name;

        public string DisplayName => _property.DisplayName;
    }
}