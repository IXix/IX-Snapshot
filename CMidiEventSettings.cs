using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Snapshot
{
    public class CMidiEventSettings
    {
        public CMidiEventSettings(CMachine owner)
        {
            m_owner = owner;

            Channel = 16; // Any
            Message = 0; // Undefined;
            Primary = 128; // Any
            Secondary = 128; // Any
        }

        private readonly CMachine m_owner;

        public string Description
        {
            get
            {
                switch (Message)
                {
                    case 0: // Undefined
                        return "";

                    case 1: // Note-on
                        return string.Format("Note: {0}, Ch. {1}",
                            Primary < 128 ? CMachine.NoteNames[Primary] : "Any",
                            Channel < 16 ? (Channel + 1).ToString() : "Any");

                    case 2: // Note-off
                        return string.Format("Note-off: {0}, Ch. {1}",
                            Primary < 128 ? CMachine.NoteNames[Primary] : "Any",
                            Channel < 16 ? (Channel + 1).ToString() : "Any");

                    case 3: // Controller
                        return string.Format("CC: {0}, Val. {1}, Ch. {2}",
                            Primary < 128 ? (Primary + 1).ToString() : "Any",
                            Secondary < 128 ? (Secondary + 1).ToString() : "Any",
                            Channel < 16 ? (Channel + 1).ToString() : "Any");

                    default:
                        return "";
                }
            }
        }

        public bool Learning { get; set; }

        public Byte Channel { get; set; } // 16 = Any

        // Undefined = 0, Note_On = 1, Note_Off = 2, Controller = 3
        public Byte Message { get; set; }

        public Byte Primary { get; set; } // Note or CC number. 128 = undefined

        public Byte Secondary { get; set; } // Velocity or value. 128 = undefined

        public CPropertySelection Selection { get; set; }

        public bool StoreSelection { get; set; }

        public bool BoolOption1 { get; set; }

        public UInt32 Encode()
        {
            return (UInt32)((Message << 24) | (Secondary << 16) | (Primary << 8) | Channel);
        }

        internal void ReadData(BinaryReader r)
        {
            Byte file_version = m_owner.LoadVersion;

            Channel = r.ReadByte();
            Message = r.ReadByte();
            Primary = r.ReadByte();
            Secondary = r.ReadByte();

            if (file_version >= 4)
            {
                StoreSelection = r.ReadBoolean();
                BoolOption1 = r.ReadBoolean();
                if(StoreSelection)
                {
                    Selection = new CPropertySelection(m_owner, false);
                    Selection.ReadData(r);
                }
            }
        }

        internal void WriteData(BinaryWriter w)
        {
            w.Write(Channel);
            w.Write(Message);
            w.Write(Primary);
            w.Write(Secondary);

            // New in file version 4
            w.Write(StoreSelection);
            w.Write(BoolOption1);
            if(Selection != null)
            {
                Selection.WriteData(w);
            }
        }

        // Check if this event clashes with that event
        internal bool CheckConflict(CMidiEventSettings that)
        {
            // If either is undefined
            if (Message == 0 || that.Message == 0) return false;

            if (Message != that.Message) return false;

            // If neither is 'Any'
            if (Channel < 16 && that.Channel < 16)
            {
                if (Channel != that.Channel) return false;
            }

            // If neither is 'Any;
            if (Primary < 128 && that.Primary < 128)
            {
                if (Primary != that.Primary) return false;
            }

            // If neither is 'Any;
            if (Secondary < 128 && that.Secondary < 128)
            {
                if (Secondary != that.Secondary) return false;
            }

            return true;
        }

        internal void Reset()
        {
            Channel = 16; // Any
            Message = 0; // Undefined;
            Primary = 128; // Any
            Secondary = 128; // Any

            Selection = null;

            StoreSelection = false;
            BoolOption1 = false;
        }
    }

}