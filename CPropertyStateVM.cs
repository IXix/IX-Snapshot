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

        public CPropertyStateVM(IPropertyState property, CTreeViewItemVM parent, CMachineStateVM stateVM)
            : base(parent, false, stateVM)
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
            get { return _stateVM.Reference.ContainsProperty(_property); }
        }

        protected override void OnCheckChanged()
        {
            _property.Selected = (bool)IsChecked;
            _property.OnSelChanged(new StateChangedEventArgs() { Property = _property, Selected = _property.Selected });
        }

        public string Name => _property.Name;

        public string DisplayName => _property.DisplayName;
    }
}