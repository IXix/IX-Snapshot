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
            : base(group, parent, ownerVM, view)
        {
            _group = group;

            foreach(CPropertyStateGroup param in _group.ChildProperties)
            {
                _childProperties = _childProperties.Concat(param.ChildProperties).ToHashSet();
            }

            LoadChildren();
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

        protected override void LoadChildren()
        {
            foreach (CPropertyStateGroup pg in _group.ChildProperties)
            {
                CPropertyStateGroupVM gvm = new CPropertyStateGroupVM(pg, this, _ownerVM, _viewRef);
                Children.Add(gvm);
                gvm.PropertyChanged += OnChildPropertyChanged;
            }
        }
    }
}