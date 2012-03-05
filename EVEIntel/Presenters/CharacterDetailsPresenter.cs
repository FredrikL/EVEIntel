using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using EVEIntel.Model;
using EVEIntel.Views;
using System.Windows.Media.Imaging;

namespace EVEIntel.Presenters
{
    public class CharacterDetailsPresenter : PresenterBase<CharacterDetailsView>
    {
        private readonly ApplicationPresenter _presenter;
        private readonly Player _player;

        public CharacterDetailsPresenter(ApplicationPresenter presenter, CharacterDetailsView view, Player player):
            base(view, "Player.Name")
        {
            View.DataContext = this;
            _presenter = presenter;
            _player = player;

            if(player.Portrait == null)
            {
                //TODO: Make this async pls
                player.Portrait = Repository.EVEApi.Character.GetCharacterPortrait(player);       
            }
        }

        public Player Player
        {
            get { return _player; }
        }

        public void Close()
        {
            _presenter.CloseTab(this);
        }
    }
}
