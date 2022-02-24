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

        public CTrackPropertyStateGroupVM(CTrackPropertyStateGroup group, CTreeViewItemVM parent, CMachineStateVM stateVM)
            : base(parent, true, stateVM)
        {
            _group = group;
            IsChecked = false;
            LoadChildren();
        }

        public string Name
        {
            get { return _group.Name; }
        }

        public override bool GotValue
        {
            get
            {
                try
                {
                    var c = Children.First(x => x.GotValue == true);
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
            foreach (var pg in _group.Children)
            {
                Children.Add(new CPropertyStateGroupVM(pg, this, _stateVM));
            }
        }
    }
}