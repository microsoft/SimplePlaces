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

using Lumia.Sense;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Navigation;

namespace SimplePlaces
{
    /// <summary>
    /// Class to display visited places on the map.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Private members
        /// <summary>
        /// Place monitor instance
        /// </summary>
        private PlaceMonitor _placeMonitor = null;

        /// <summary>
        /// Current date for moving between next and previous days
        /// </summary>
        private DateTime _iCurrentDate = DateTime.Today;

        /// <summary>
        /// check to see launching finished or not
        /// </summary>
        private bool iLaunched = false;

        /// <summary>
        /// visited places history
        /// </summary>
        IList<Place> placesList = null;

        /// <summary>
        /// holds all the pushpins in the map
        /// </summary>
        List<PushPin> pins = null;
        #endregion

        /// <summary>
        /// constructor
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            Window.Current.VisibilityChanged += async ( oo, ee ) =>
            {
                if( !ee.Visible && _placeMonitor !=null)
                {
                    await CallSenseApiAsync( async () =>
                    {
                        await _placeMonitor.DeactivateAsync();
                    } );
                 }
                else if( _placeMonitor != null )
                {
                    await CallSenseApiAsync( async () =>
                    {
                        await _placeMonitor.ActivateAsync();
                    } );
                    UpdateScreenAsync();
                }
            };
        }

        /// <summary>
        /// initializes the sensor services
        /// </summary>
        /// <returns></returns>
        private async Task Initialize()
        {
            //following code assumes that device has new software(SensorCoreSDK1.1 based)
            try
            {
                if( !( await PlaceMonitor.IsSupportedAsync() ) )
                {
                    MessageDialog dlg = new MessageDialog( "Unfortunately this device does not support viewing visited places" );
                    await dlg.ShowAsync();
                    Application.Current.Exit();
                }
                else
                {
                    uint apiSet = await SenseHelper.GetSupportedApiSetAsync();
                    MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                    if( !settings.LocationEnabled )
                    {
                        MessageDialog dlg = new MessageDialog( "In order to collect and view visited places you need to enable location in system settings. Do you want to open settings now? if no, applicatoin will exit", "Information" );
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchLocationSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) =>
                        {
                            Application.Current.Exit();
                        } ) ) );
                        await dlg.ShowAsync();
                    }
                    
                    if( !settings.PlacesVisited ) 
                    {
                        MessageDialog dlg = null;
                        if(settings.Version < 2)
                        {
                            //device which has old motion data settings.
                            //this is equal to motion data settings on/off in old system settings(SDK1.0 based)
                            dlg = new MessageDialog( "In order to collect and view visited places you need to enable Motion data in Motion data settings. Do you want to open settings now? if no, application will exit", "Information" );
                        }
                        else
                        {
                            dlg = new MessageDialog( "In order to collect and view visited places you need to 'enable Places visited' and 'DataQuality to detailed' in Motion data settings. Do you want to open settings now? if no, application will exit", "Information" );
                        }
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchSenseSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) =>
                        { 
                            Application.Current.Exit(); 
                        }) ) );
                        await dlg.ShowAsync();
                    }
                }
            }
            catch( Exception )
            {
                
            }
            
            //in case if the device has old software(earlier than SDK1.1) or system settings changed after sometime, CallSenseApiAsync() method handles the system settings prompts.
            if( _placeMonitor == null )
            {
                if( !await CallSenseApiAsync( async () =>
                {
                    _placeMonitor = await PlaceMonitor.GetDefaultAsync();
                } ) )
                {
                    Application.Current.Exit();
                }
            }

            //setting current loation in the map
            try
            {
                PlacesMapControl.MapServiceToken = "4eSgIBUeMtjFyJP6YxkyPQ";
                Geoposition geoposition = await new Geolocator().GetGeopositionAsync(  maximumAge: TimeSpan.FromMinutes( 5 ), timeout: TimeSpan.FromSeconds( 5 ));
                PlacesMapControl.Center = geoposition.Coordinate.Point;
            }
            catch( Exception )
            {
                // if current position can't get, setting default position to Espoo, Finland.
                PlacesMapControl.Center = new Geopoint(new BasicGeoposition(){Latitude = 60.17, Longitude = 24.83});
            }
            await GetHistory();
            UpdateScreenAsync();
        }

        /// <summary>
        /// Gets the history of places visited on a day.
        /// </summary>
        private async Task GetHistory()
        {
            
            //gets the places visited on that day
            await CallSenseApiAsync( async () => 
                placesList = await _placeMonitor.GetPlaceHistoryAsync( _iCurrentDate, TimeSpan.FromDays( 1 ) ) );
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo( NavigationEventArgs e )
        {
            // Make sure the sensors are instantiated
            if(!iLaunched)
            {
                iLaunched = true;
                await Initialize();
            }
        }

        /// <summary>
        /// Displays previous day visited places
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">Event arguments </param>
        private async void GoToPreviousDay( object sender, RoutedEventArgs e )
        {
            if( ( DateTime.Now.Date - _iCurrentDate ) < TimeSpan.FromDays( 6 ) )
            {
                _iCurrentDate = _iCurrentDate.AddDays( -1 );
                await GetHistory();
                if( placesList==null||placesList.Count<=0 )
                {
                    if( pins != null )
                    {
                        pins.Clear();
                        pins = null;
                    }
                    PlacesMapControl.MapElements.Clear();
                }
               UpdateScreenAsync();
            }
            else
            {
                MessageDialog dialog = new MessageDialog( "This application displays last seven days visited places only.", "Places" );
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Displays next day visited places
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">Event arguments</param>
        private async void GoToNextDay( object sender, RoutedEventArgs e )
        {
            if( _iCurrentDate.Date < DateTime.Now.Date )
            {
                _iCurrentDate = _iCurrentDate.AddDays( 1 );
                await GetHistory();
                // if no places for a day, remove other day places from the map
                if( placesList == null || placesList.Count <= 0 )
                {
                    if(pins != null)
                    {
                        pins.Clear();
                        pins = null;
                    }
                    PlacesMapControl.MapElements.Clear();
                }
               UpdateScreenAsync();
            }
            else 
            {
                MessageDialog dialog = new MessageDialog( "Can't display future places", "Places" );
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Gets history of places visited from last seven days.
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">Event arguments</param>
        private async void ShowHistory( object sender, RoutedEventArgs e )
        {
            IList<Place> placesList = null;
            await CallSenseApiAsync( async () => placesList =
                    await _placeMonitor.GetPlaceHistoryAsync(DateTime.Today.AddDays( -7 ), TimeSpan.FromDays( 8 )));
            if( placesList != null )
            {
                string history = "";
                foreach( Place place in placesList )
                {
                    history += "Latitude = " + place.Position.Latitude + " \nLongitude = " + place.Position.Longitude + " \nPlace = " + place.Kind +
                                               "\nTimestamp = " + place.Timestamp.ToString( "MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture ) + "\nLenghOfStay = " +
                                               place.LengthOfStay.ToString() + "\nTotalLenghOfStay = " + place.TotalLengthOfStay.ToString() + "\nTotalVisitCount = " +
                                               place.TotalVisitCount.ToString() + "\n\n";
                }
                MessageDialog dialog = new MessageDialog( history, "Places History" );
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Updates visualization
        /// </summary>
        private void UpdateScreenAsync()
        {
            if( _placeMonitor != null )
            {
                // create pushpins on visited places
                if( placesList != null && placesList.Count > 0 )
                {
                    PlacesMapControl.Children.Clear();
                    FilterTime.Text=_iCurrentDate.ToString("MMM dd yyyy", CultureInfo.InvariantCulture);
                    if( pins != null )
                    {
                        pins.Clear();
                        pins = null;
                    }
                    pins = new List<PushPin>();
                    int i = 0;
                    foreach( Place place in placesList )
                    {
                        //set place info to pushpin. this info will be displayed on message dialog when tapped on pushpin.
                        string description = "Latitude = " + place.Position.Latitude + " \nLongitude = " + place.Position.Longitude + " \nPlace = " + place.Kind +
                            "\nTimestamp = " + place.Timestamp.ToString( "MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture ) + "\nLenghOfStay = " +
                            place.LengthOfStay.ToString() + "\nTotalLenghOfStay = " + place.TotalLengthOfStay.ToString() + "\nTotalVisitCount = " +
                            place.TotalVisitCount.ToString() + "\n\n";
                        pins.Add( new PushPin( description, new Geopoint( place.Position ), place.Kind.ToString() ));

                        MapControl.SetLocation( pins[ i ], new Geopoint( place.Position ) );
                        MapControl.SetNormalizedAnchorPoint( pins[ i ], new Point( 0.15, 1 ) );
                        PlacesMapControl.Children.Add( pins[ i ] );
                        i=i+1;
                    }
                }
            }
        }

        /// <summary>
        /// Performs asynchronous SensorCore SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action"></param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        private async Task<bool> CallSenseApiAsync( Func<Task> action )
        {
            Exception failure = null;
            try
            {
                await action();
            }
            catch( Exception e )
            {
                failure = e;
            }

            if( failure != null )
            {
                bool status = false;
                MessageDialog dialog = null;
                switch( SenseHelper.GetSenseError( failure.HResult ) )
                {
                    case SenseError.LocationDisabled:
                        dialog = new MessageDialog( "In order to collect and view visited places you need to enable location in system settings. Do you want to open Location settings now? if no, application will exit", "Information" );
                        dialog.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) =>
                        {
                            status = true;
                            await SenseHelper.LaunchLocationSettingsAsync();
                        } ) ) );
                        dialog.Commands.Add( new UICommand( "No" ) );
                        await dialog.ShowAsync();
                        return status;

                    case SenseError.SenseDisabled:
                        dialog = new MessageDialog( "In order to collect and view visited places you need to enable Places visited in Motion data settings. Do you want to open Motion data settings now?  if no, application will exit", "Information" );
                        dialog.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) =>
                        {
                            status = true;
                            await SenseHelper.LaunchSenseSettingsAsync();
                        } ) ) );
                        dialog.Commands.Add( new UICommand( "No" ) );
                        await dialog.ShowAsync();
                        return status;

                    case SenseError.IncompatibleSDK:
                        dialog = new MessageDialog( "This application has become outdated. Please update to the latest version.", "Information" );
                        await dialog.ShowAsync();
                        return false;

                    default:
                        return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}
