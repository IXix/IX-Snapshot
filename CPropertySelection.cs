using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snapshot
{
    public class CPropertySelection
    {
        public CPropertySelection(CMachine owner, bool create)
        {
            m_owner = owner;
            if (create)
            {
                m_selection = new HashSet<CPropertyBase>();
            }
        }

        public CPropertySelection(CMachine owner, HashSet<CPropertyBase> selection) : this(owner, false)
        {
            if(selection != null)
            {
                m_selection = new HashSet<CPropertyBase>(selection);
            }
        }

        private HashSet<CPropertyBase> m_selection;
        private readonly CMachine m_owner;

        public HashSet<CPropertyBase> SelectedProperties => m_selection;

        internal void ReadData(BinaryReader r)
        {
            UInt32 nMachines = r.ReadUInt32();
            if (nMachines > 0)
            {
                m_selection = new HashSet<CPropertyBase>();

                for (UInt32 i = 0; i < nMachines; i++)
                {
                    string macName = r.ReadString();
                    string dllName = r.ReadString();

                    // Should be one and only one state matching both name and dllname. Exception if not.
                    CMachineState s = m_owner.States.Single(x => x.Machine.Name == macName && x.Machine.DLL.Name == dllName);

                    UInt32 nProperties = r.ReadUInt32();
                    for (UInt32 ip = 0; ip < nProperties; ip++)
                    {
                        CPropertyBase p = CPropertyBase.FindPropertyFromSavedInfo(r, s);
                        if (p != null)
                        {
                            _ = m_selection.Add(p);
                        }
                    }
                }
            }
        }


        public void WriteData(BinaryWriter w)
        {
            // Build list of machines and properties
            Dictionary<CMachineState, HashSet<CPropertyBase>> savedata = new Dictionary<CMachineState, HashSet<CPropertyBase>>();
            if (m_selection != null)
            {
                foreach (CPropertyBase p in m_selection)
                {
                    // TEMP TEST
                    if (p is CMachineState || p is CPropertyStateGroup || p is CTrackPropertyStateGroup)
                    {
                        throw new Exception("Group!");
                    }

                    if (savedata.ContainsKey(p.ParentMachine))
                    {
                        savedata[p.ParentMachine].Add(p);
                    }
                    else
                    {
                        savedata[p.ParentMachine] = new HashSet<CPropertyBase>() { p };
                    }
                }
            }

            // Write structured data
            w.Write(savedata.Count);
            foreach (CMachineState s in savedata.Keys)
            {
                w.Write(s.Machine.Name);
                w.Write(s.Machine.DLL.Name);
                w.Write(savedata[s].Count);
                foreach (CPropertyBase p in savedata[s])
                {
                    p.WritePropertyInfo(w);
                }
            }
        }
    }
}
