/*	
The MIT License (MIT)
Copyright (c) 2015 Microsoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. 
 */

using System;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace SimplePlaces
{
    /// <summary>
    /// class to display the popup(pushpin) on the map.
    /// </summary>
    public partial class PushPin : UserControl
    {
        /// <summary>
        /// holds description of the place details.
        /// </summary>
        private string _description;
        /// <summary>
        /// geopoint to address of the place.
        /// </summary>
        private Geopoint _geopoint;
        /// <summary>
        /// holds address of the place.
        /// </summary>
        private string _address;

        /// <summary>
        /// constructor
        /// </summary>
        public PushPin(string description, Geopoint geopoint, string address)
        {
            InitializeComponent();
            _description = description;
            _geopoint = geopoint;
            _address = address;
            Loaded += PushPin_Loaded;
        }

        /// <summary>
        /// page load event. sets the pushpin text.
        /// </summary>
        /// <param name="sender">event details</param>
        /// <param name="e">event sender</param>
        async void PushPin_Loaded( object sender, RoutedEventArgs e )
        {
            Lbltext.Text = "'"+_address+"'"+" place";
            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync( _geopoint );
            // If successful then display the address.
            if( result.Status == MapLocationFinderStatus.Success )
            {
                if( result.Locations.Count > 0 )
                {
                    if( string.IsNullOrEmpty( result.Locations[ 0 ].Address.Street ) )
                    {
                        // If the address of a geographic loaction is empty or null, get only the town and region
                        var region = result.Locations[ 0 ].Address.Region != "" ? ", " + result.Locations[ 0 ].Address.Region : null;
                        _description += "Address: " + result.Locations[ 0 ].Address.Town + region;
                    }
                    else
                    {
                        // Get the complete address of a geographic location
                        _description += "Address: " + result.Locations[ 0 ].Address.StreetNumber + " " + result.Locations[ 0 ].Address.Street;
                    }
                }
            }
        }

        /// <summary>
        /// Tapped event on the pushpin to display details.
        /// </summary>
        /// <param name="sender">event details</param>
        /// <param name="e">event sender</param>
        private async void PushPinTapped( object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e )
        {
            MessageDialog dialog = new MessageDialog( _description, "Place Information" );
            await dialog.ShowAsync();
        }
    }
}