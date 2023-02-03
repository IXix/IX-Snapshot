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
    public class CTrackPropertyStateGroupVM : CMachinePropertyItemVM
    {
        readonly CTrackPropertyStateGroup _group;

        public CTrackPropertyStateGroupVM(CTrackPropertyStateGroup group, CTreeViewItemVM parent, CSnapshotMachineVM ownerVM, int view)
            : base(parent, true, ownerVM, view)
        {
            _group = group;

            foreach(CPropertyStateGroup param in _group.Children)
            {
                _properties.Concat(param.Children);
            }

            LoadChildren();
        }

        public override string Name
        {
            get { return _group.Name; }
        }

        public override bool GotValue
        {
            get
            {
                try
                {
                    _ = Children.First(x => (x as CMachinePropertyItemVM).GotValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override int? SmoothingCount => _group.SmoothingCount;

        public override int? SmoothingUnits => _group.SmoothingUnits;

        protected override void LoadChildren()
        {
            foreach (CPropertyStateGroup pg in _group.Children)
            {
                Children.Add(new CPropertyStateGroupVM(pg, this, _ownerVM, _viewRef));
            }
        }
    }
}