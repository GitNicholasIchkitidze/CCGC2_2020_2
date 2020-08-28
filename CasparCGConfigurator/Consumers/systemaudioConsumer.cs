using System;
using System.ComponentModel;

namespace CasparCGConfigurator
{
    public class SystemAudioConsumer : AbstractConsumer, INotifyPropertyChanged
    {
        public SystemAudioConsumer()
        {
        }

        public override string ToString()
        {
            return "System Audio";
        }

        public override event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void NotifyChanged(String info)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(info));
        }
    }
}
