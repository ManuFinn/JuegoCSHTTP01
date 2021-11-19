using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ServidorJuegoCS.Game {

    public class Raiser : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void RaiseProperty([CallerMemberName]string propertyname = null) {
            PropertyChanged?.Invoke(this, new(propertyname));
        }

    }

}