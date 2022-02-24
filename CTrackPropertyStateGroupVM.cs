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
    // Groups
    public class CTrackPropertyStateGroupVM : CTreeViewItemVM
    {
        readonly CTrackPropertyStateGroup _group;

        public CTrackPropertyStateGroupVM(CTrackPropertyStateGroup group, CTreeViewItemVM parent)
            : base(parent, true)
        {
            _group = group;
            IsChecked = false;
            LoadChildren();
        }

        public string Name
        {
            get { return _group.Name; }
        }

        public override bool GotValue => _group.GotValue;

        protected override void LoadChildren()
        {
            foreach (var pg in _group.Children)
            {
                Children.Add(new CPropertyStateGroupVM(pg, this));
            }
        }
    }
}