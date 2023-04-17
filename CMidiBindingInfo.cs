using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snapshot
{
    public class CMidiBindingInfo : INotifyPropertyChanged
    {
        public CMidiBindingInfo()
        {
            m_info = new Dictionary<string, string>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // FIXME:
        // These should return a formatted string representing the MIDI
        // or empty string if no binding exists
        internal Dictionary<string, string> m_info;

        private string GetInfo(string key)
        {
            try
            {
                return m_info[key];
            }
            catch
            {
                return "";
            }
        }

        public string CaptureSelected => GetInfo("Capture");
        public string CaptureMissing => GetInfo("CaptureMissing");
        public string ClearAll => GetInfo("Clear");
        public string ClearSelected => GetInfo("ClearSelected");
        public string Purge => GetInfo("Purge");
        public string RestoreAll => GetInfo("Restore");

        internal void Update(CMidiTargetInfo info)
        {
            m_info[info.command] = info.EventDetails;
        }
    }
}
