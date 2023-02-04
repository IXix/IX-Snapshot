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
        public CPropertyStateVM(CPropertyBase property, CTreeViewItemVM parent, CSnapshotMachineVM ownerVM, int view)
            : base(property, parent, ownerVM, view)
        {
            Properties.Add(property);
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
    }
}