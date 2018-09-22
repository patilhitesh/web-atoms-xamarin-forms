using Rg.Plugins.Popup.Pages;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace WebAtoms
{
    public class BusyPopup : PopupPage
    {
        public BusyPopup()
        {
            var grid = new Grid();

            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto  });
            grid.RowDefinitions.Add(new RowDefinition());

            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var label = new Label();
            label.BackgroundColor = Color.White;
            label.TextColor = Color.Green;

            label.Text = "Loading...";

            Grid.SetRow(label, 1);
            Grid.SetColumn(label, 1);

            grid.Children.Add(label);
            this.Content = grid;
        }

    }
}
