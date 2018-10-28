
SimplePlaces
==========
SimplePlaces is a sample application which demonstrates the usage of the Place 
Monitor API in Windows Phone 8.1. This application shows the known places on the map and by tapping the places push pin, more information about the selected place. The user is able to see the places visited from the last seven
days as well as places history by using the application bar buttons.

1. Instructions
--------------------------------------------------------------------------------

Learn about the Lumia SensorCore SDK from the Lumia Developer's Library. 
The example requires the Lumia SensorCore SDK's NuGet package but will retrieve it
automatically (if missing) on the first build.

To build the application you need to have Windows 8.1 and Windows Phone SDK 8.1
installed.

Using the Windows Phone 8.1 SDK:

1. Open the SLN file: File > Open Project, select the file `SimplePlaces.sln`
2. Remove the "AnyCPU" configuration (not supported by the Lumia SensorCore SDK)
or simply select ARM
3. Select the target 'Device'.
4. Press F5 to build the project and run it on the device.

Please see the official documentation for
deploying and testing applications on Windows Phone devices:
http://msdn.microsoft.com/en-us/library/gg588378%28v=vs.92%29.aspx


2. Implementation
--------------------------------------------------------------------------------

The functionality is in the MainPage.xaml.cs file, which handles the usage of
Places Monitor API. The API is called through the CallSenseApiAsync() helper function to
handle the errors when the required features are disabled in the system settings.

Initialize() function contains the compatibility check for devices that have different
sensorCore SDK service.

UpdateScreenAsync() is the method which queries the SensorCore SDK Place Monitor for the
known places during the current day or the last seven days, by calling: GetPlaceHistoryAsync(). 
Push pins are created to indicate the visited places. Place information is added to the push pin and it will
be displayed on message dialog when tapped.

ShowHistory() function gets history of places visited from last seven days by calling GetPlaceHistoryAsync()
and displays the result in a message dialog, by tapping the application bar button.    
    
3. Version history
--------------------------------------------------------------------------------
* Version 1.1.0.0: The first release.

4. Downloads
---------

| Project | Release | Download |
| ------- | --------| -------- |
| SimplePlaces | v1.1.0.0 | [simpleplaces-1.1.0.0.zip](https://github.com/Microsoft/SimplePlaces/archive/v1.1.0.0.zip) |

5. See also
--------------------------------------------------------------------------------

The projects listed below are exemplifying the usage of the SensorCore APIs

* SimpleSteps - https://github.com/Microsoft/SimpleSteps
* SimpleActivity - https://github.com/Microsoft/SimpleActivity
* SimpleTracks - https://github.com/Microsoft/SimpleTracks
