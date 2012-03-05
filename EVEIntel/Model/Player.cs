using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace EVEIntel.Model
{
    public class Player : IData, INotifyPropertyChanged
    {
        public Int64 CharacterID { get; set; }
        public string Name { get; set; }
        public string Corp { get; set; }
        public string Alliance { get; set; }
        public Ship Ship { get; set; }
        public List<Ship> KnownShips { get; set; }
        public Seen LastSeen { get; set; }
        public List<Seen> Seen { get; set; }
        public string Faction{   get; set;}

        private BitmapImage portrait;
        public BitmapImage Portrait
        {
            get { return portrait; }
            set 
            {
                portrait = value;
                OnPropertyChanged("Portrait");
            }
        }

        public Player()
        {
            LastSeen = new Seen
                           {
                               Count = 0,
                               System = String.Empty,
                               Time = DateTime.MinValue
                           };
        }

        public bool Validate()
        {
            return !String.IsNullOrEmpty(Name) &&
                   !String.IsNullOrEmpty(Corp) &&
                   Ship != null &&
                   Ship.Validate();
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Player))
                return false;
            Player p2 = obj as Player;

            return Name == p2.Name &&
                   Corp == p2.Corp &&
                   Alliance == p2.Alliance;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
