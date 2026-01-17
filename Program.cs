using WwDevicesDotNet;
using WwDevicesDotNet.WinWing.FcuAndEfis;
using WwDevicesDotNet.WinWing.Pap3;

Console.WriteLine("WinWing Device Test Program");
Console.WriteLine("===========================\n");

var connectedCdus = CduFactory.FindLocalDevices();
var connectedFrontpanels = FrontpanelFactory.FindLocalDevices();

if (connectedCdus.Count == 0 && connectedFrontpanels.Count == 0)
{
    Console.WriteLine("No WinWing devices found. Please connect a device and try again.");
    return;
}

if (connectedCdus.Count > 0)
{
    Console.WriteLine($"Found {connectedCdus.Count} CDU device(s):\n");
    for (int i = 0; i < connectedCdus.Count; i++)
    {
        var device = connectedCdus[i];
        Console.WriteLine($"[CDU-{i + 1}] {device.Description}");
        Console.WriteLine($"    Type: {device.DeviceType}");
        Console.WriteLine($"    User: {device.DeviceUser}");
        Console.WriteLine($"    USB: 0x{device.UsbVendorId:X4}:0x{device.UsbProductId:X4}\n");
    }
}

if (connectedFrontpanels.Count > 0)
{
    Console.WriteLine($"Found {connectedFrontpanels.Count} FrontPanel device(s):\n");
    for (int i = 0; i < connectedFrontpanels.Count; i++)
    {
        var device = connectedFrontpanels[i];
        Console.WriteLine($"[FP-{i + 1}] {device.Description}");
        Console.WriteLine($"    Type: {device.DeviceType}");
        Console.WriteLine($"    USB: 0x{device.UsbVendorId:X4}:0x{device.UsbProductId:X4}\n");
    }
}

Console.WriteLine("\nPress any key to start testing all connected devices...");
Console.ReadKey();
Console.WriteLine("\n");

var activeCdus = new List<ICdu>();
var activeFrontpanels = new List<IFrontpanel>();

try
{
    foreach (var deviceId in connectedCdus)
    {
        Console.WriteLine($"Connecting to {deviceId.Description}...");
        var cdu = CduFactory.ConnectLocal(deviceId);
        
        if (cdu != null)
        {
            activeCdus.Add(cdu);
            Console.WriteLine($"Successfully connected to {deviceId.Description}");
            
            cdu.KeyDown += (sender, e) =>
            {
                Console.WriteLine($"[{deviceId.Description}] Key Down: {e.Key}");
            };
            
            cdu.KeyUp += (sender, e) =>
            {
                Console.WriteLine($"[{deviceId.Description}] Key Up: {e.Key}");
            };
            
            cdu.Disconnected += (sender, e) =>
            {
                Console.WriteLine($"[{deviceId.Description}] Device disconnected!");
            };
            
            DisplayTestScreen(cdu, deviceId);
        }
        else
        {
            Console.WriteLine($"Failed to connect to {deviceId.Description}");
        }
    }

    foreach (var deviceId in connectedFrontpanels)
    {
        Console.WriteLine($"Connecting to {deviceId.Description}...");
        var frontpanel = FrontpanelFactory.ConnectLocal(deviceId);
        
        if (frontpanel != null)
        {
            activeFrontpanels.Add(frontpanel);
            Console.WriteLine($"Successfully connected to {deviceId.Description}");
            
            frontpanel.ControlActivated += (sender, e) =>
            {
                Console.WriteLine($"[{deviceId.Description}] Control Activated: {e.ControlId}");
            };
            
            frontpanel.ControlDeactivated += (sender, e) =>
            {
                Console.WriteLine($"[{deviceId.Description}] Control Deactivated: {e.ControlId}");
            };
            
            frontpanel.Disconnected += (sender, e) =>
            {
                Console.WriteLine($"[{deviceId.Description}] Device disconnected!");
            };
            
            InitializeFrontpanel(frontpanel, deviceId);
        }
        else
        {
            Console.WriteLine($"Failed to connect to {deviceId.Description}");
        }
    }
    
    if (activeCdus.Count > 0 || activeFrontpanels.Count > 0)
    {
        Console.WriteLine($"\n{activeCdus.Count} CDU(s) and {activeFrontpanels.Count} FrontPanel(s) are now active.");
        DisplayMenu();
        
        bool running = true;
        while (running)
        {
            var key = Console.ReadKey(true);
            
            switch (key.KeyChar)
            {
                case '1':
                    Console.WriteLine("\nDisplaying test pattern...");
                    foreach (var cdu in activeCdus)
                    {
                        DisplayTestPattern(cdu);
                    }
                    break;
                    
                case '2':
                    Console.WriteLine("\nTesting CDU LEDs...");
                    TestLeds(activeCdus);
                    break;
                    
                case '3':
                    Console.WriteLine("\nTesting CDU brightness controls...");
                    TestBrightness(activeCdus);
                    break;
                    
                case '4':
                    Console.WriteLine("\nAmbient light sensor values:");
                    foreach (var cdu in activeCdus)
                    {
                        if (cdu.HasAmbientLightSensor)
                        {
                            Console.WriteLine($"  {cdu.DeviceId.Description}:");
                            Console.WriteLine($"    Left: {cdu.LeftAmbientLightNative}");
                            Console.WriteLine($"    Right: {cdu.RightAmbientLightNative}");
                            Console.WriteLine($"    Percent: {cdu.AmbientLightPercent}%");
                        }
                        else
                        {
                            Console.WriteLine($"  {cdu.DeviceId.Description}: No ambient light sensor");
                        }
                    }
                    break;
                    
                case '5':
                    Console.WriteLine("\nClearing all CDU displays...");
                    foreach (var cdu in activeCdus)
                    {
                        cdu.Output.Clear();
                        cdu.RefreshDisplay();
                    }
                    break;
                    
                case '6':
                    Console.WriteLine("\nTesting FrontPanel displays...");
                    TestFrontpanelDisplays(activeFrontpanels);
                    break;
                    
                case '7':
                    Console.WriteLine("\nTesting FrontPanel LEDs...");
                    TestFrontpanelLeds(activeFrontpanels);
                    break;
                    
                case '8':
                    Console.WriteLine("\nTesting FrontPanel brightness...");
                    TestFrontpanelBrightness(activeFrontpanels);
                    break;
                    
                case 'm':
                case 'M':
                    DisplayMenu();
                    break;
                    
                case 'q':
                case 'Q':
                    Console.WriteLine("\nExiting...");
                    running = false;
                    break;
            }
        }
    }
}
finally
{
    foreach (var cdu in activeCdus)
    {
        cdu?.Dispose();
    }
    
    foreach (var frontpanel in activeFrontpanels)
    {
        frontpanel?.Dispose();
    }
}

Console.WriteLine("Test program completed.");

static void DisplayMenu()
{
    Console.WriteLine("\nTest Menu:");
    Console.WriteLine("  CDU Tests:");
    Console.WriteLine("    1 - Display test pattern on CDUs");
    Console.WriteLine("    2 - Test CDU LED lights");
    Console.WriteLine("    3 - Test CDU brightness controls");
    Console.WriteLine("    4 - Display ambient light sensor values");
    Console.WriteLine("    5 - Clear all CDU displays");
    Console.WriteLine("  FrontPanel Tests:");
    Console.WriteLine("    6 - Test FrontPanel displays");
    Console.WriteLine("    7 - Test FrontPanel LEDs");
    Console.WriteLine("    8 - Test FrontPanel brightness");
    Console.WriteLine("  M - Show this menu");
    Console.WriteLine("  Q - Quit\n");
}

static void DisplayTestScreen(ICdu cdu, DeviceIdentifier deviceId)
{
    cdu.Output
        .Clear()
        .White()
        .TopLine()
        .Centred($"{deviceId.Device}")
        .NewLine()
        .Green()
        .WriteLine($"Device: {deviceId.DeviceUser}")
        .Yellow()
        .WriteLine($"Type: {deviceId.DeviceType}")
        .NewLine()
        .Cyan()
        .WriteLine("Ready for testing")
        .NewLine()
        .White()
        .WriteLine($"Supported Keys: {cdu.SupportedKeys.Count}")
        .WriteLine($"Supported LEDs: {cdu.SupportedLeds.Count}");
    
    cdu.RefreshDisplay();
    
    cdu.DisplayBrightnessPercent = 80;
    cdu.BacklightBrightnessPercent = 50;
    cdu.LedBrightnessPercent = 50;
}

static void DisplayTestPattern(ICdu cdu)
{
    cdu.Output.Clear();
    
    int currentLine = 0;
    int maxLines = cdu.Screen.Rows.Length;
    
    if (currentLine < maxLines)
    {
        cdu.Output.Line(currentLine++).White().WriteLine("Normal Colors:");
    }
    
    var colors = new[] { Colour.White, Colour.Green, Colour.Cyan, Colour.Yellow, Colour.Amber };
    for (int i = 0; i < colors.Length && currentLine < maxLines; i++)
    {
        cdu.Output
            .Line(currentLine++)
            .Colour(colors[i])
            .WriteLine($"  {colors[i]}");
    }
    
    if (currentLine < maxLines)
    {
        cdu.Output.Line(currentLine++).NewLine();
    }
    
    if (currentLine < maxLines)
    {
        cdu.Output.Line(currentLine++).White().WriteLine("Inverted Colors:");
    }
    
    var invertedColors = new[] 
    { 
        (fg: Colour.Black, bg: Colour.White),
        (fg: Colour.Black, bg: Colour.Green),
        (fg: Colour.Black, bg: Colour.Cyan),
        (fg: Colour.White, bg: Colour.Red),
        (fg: Colour.Black, bg: Colour.Yellow)
    };
    
    for (int i = 0; i < invertedColors.Length && currentLine < maxLines; i++)
    {
        cdu.Output
            .Line(currentLine++)
            .Colour(invertedColors[i].fg)
            .BGColor(invertedColors[i].bg)
            .Write($"  {invertedColors[i].bg} BG")
            .BGBlack();
    }
    
    cdu.RefreshDisplay();
}

static void TestLeds(List<ICdu> cdus)
{
    foreach (var cdu in cdus)
    {
        foreach (var led in cdu.SupportedLeds)
        {
            cdu.Leds.SetLed(led, true);
        }
        cdu.RefreshLeds();
    }
    
    Console.WriteLine("All LEDs ON. Press any key to turn off...");
    Console.ReadKey(true);
    
    foreach (var cdu in cdus)
    {
        foreach (var led in cdu.SupportedLeds)
        {
            cdu.Leds.SetLed(led, false);
        }
        cdu.RefreshLeds();
    }
    
    Console.WriteLine("LEDs turned off.");
}

static void TestBrightness(List<ICdu> cdus)
{
    Console.WriteLine("Testing brightness levels (0% -> 100%)...");
    
    for (int brightness = 0; brightness <= 100; brightness += 20)
    {
        Console.WriteLine($"Setting brightness to {brightness}%");
        foreach (var cdu in cdus)
        {
            cdu.DisplayBrightnessPercent = brightness;
            cdu.BacklightBrightnessPercent = brightness;
            cdu.LedBrightnessPercent = brightness;
        }
        Thread.Sleep(500);
    }
    
    foreach (var cdu in cdus)
    {
        cdu.DisplayBrightnessPercent = 80;
        cdu.BacklightBrightnessPercent = 50;
        cdu.LedBrightnessPercent = 50;
    }
    
    Console.WriteLine("Brightness test completed. Reset to default levels.");
}

static void InitializeFrontpanel(IFrontpanel frontpanel, DeviceIdentifier deviceId)
{
    Console.WriteLine($"Initializing {deviceId.Description}...");
    
    // Set initial brightness
    frontpanel.SetBrightness(128, 128, 128);
    
    // Display test pattern based on device type
    if (deviceId.Device == Device.WinWingFcu || 
        deviceId.Device == Device.WinWingFcuLeftEfis || 
        deviceId.Device == Device.WinWingFcuRightEfis || 
        deviceId.Device == Device.WinWingFcuBothEfis)
    {
        var state = new FcuEfisState
        {
            Speed = 250,
            Heading = 180,
            Altitude = 10000,
            VerticalSpeed = 1500,
            SpeedIsMach = false,
            HeadingIsTrack = false,
            VsIsFpa = false,
            SpeedManaged = true,
            HeadingManaged = false,
            LatIndicator = true,
            LeftBaroPressure = 1013,
            LeftBaroQnh = true,
            RightBaroPressure = 1013,
            RightBaroQnh = true
        };
        frontpanel.UpdateDisplay(state);
        
        var leds = new FcuEfisLeds
        {
            Ap1 = true,
            Loc = true,
            LeftFd = true,
            Exped = true,
            ExpedYellowBrightness = 128
        };
        frontpanel.UpdateLeds(leds);
    }
    else if (deviceId.Device == Device.WinWingPap3)
    {
        var state = new Pap3State
        {
            Speed = 250,
            Heading = 180,
            Altitude = 10000,
            VerticalSpeed = 1500,
            PltCourseValue = 45,
            CplCourseValue = 225
        };
        frontpanel.UpdateDisplay(state);
        
        var leds = new Pap3Leds
        {
            CmdA = true,
            Lnav = true,
            Vnav = true,
            FdL = true
        };
        frontpanel.UpdateLeds(leds);
    }
    else if (deviceId.Device == Device.WinWingPdc3n)
    {
        // PDC3 initialization - basic state display
        var state = new FcuEfisState
        {
            Speed = 180,
            Heading = 90,
            Altitude = 5000
        };
        frontpanel.UpdateDisplay(state);
    }
    
    Console.WriteLine($"{deviceId.Description} initialized.");
}

static void TestFrontpanelDisplays(List<IFrontpanel> frontpanels)
{
    if (frontpanels.Count == 0)
    {
        Console.WriteLine("No frontpanel devices connected.");
        return;
    }
    
    Console.WriteLine("Cycling through display values and indicators...");
    
    for (int cycle = 0; cycle < 5; cycle++)
    {
        foreach (var frontpanel in frontpanels)
        {
            if (frontpanel.DeviceId.Device == Device.WinWingFcu || 
                frontpanel.DeviceId.Device == Device.WinWingFcuLeftEfis || 
                frontpanel.DeviceId.Device == Device.WinWingFcuRightEfis || 
                frontpanel.DeviceId.Device == Device.WinWingFcuBothEfis)
            {
                var state = new FcuEfisState();
                
                switch (cycle)
                {
                    case 0: // Normal mode - knots, HDG, V/S, hPa QNH
                        Console.WriteLine("  Cycle 1: SPD (knots), HDG, V/S, hPa QNH");
                        state.Speed = 250;
                        state.Heading = 180;
                        state.Altitude = 10000;
                        state.VerticalSpeed = 1500;
                        state.SpeedIsMach = false;
                        state.HeadingIsTrack = false;
                        state.VsIsFpa = false;
                        state.LeftBaroPressure = 1013;
                        state.RightBaroPressure = 1013;
                        state.LeftBaroQnh = true;
                        state.RightBaroQnh = true;
                        break;
                        
                    case 1: // Mach, TRK mode, FPA, inHg QNH
                        Console.WriteLine("  Cycle 2: MACH, TRK, FPA, inHg QNH");
                        state.Speed = 82; // Mach 0.82
                        state.Heading = 270;
                        state.Altitude = 35000;
                        state.VerticalSpeed = -2000;
                        state.SpeedIsMach = true;
                        state.HeadingIsTrack = true;
                        state.VsIsFpa = true;
                        state.LeftBaroPressure = 2992; // 29.92 inHg (stored as 2992)
                        state.RightBaroPressure = 2992;
                        state.LeftBaroQnh = true;
                        state.RightBaroQnh = true;
                        break;
                        
                    case 2: // Managed modes, QFE
                        Console.WriteLine("  Cycle 3: Managed modes (dots), QFE");
                        state.Speed = 180;
                        state.Heading = 90;
                        state.Altitude = 5000;
                        state.VerticalSpeed = 0;
                        state.SpeedManaged = true;
                        state.HeadingManaged = true;
                        state.AltitudeManaged = true;
                        state.LatIndicator = true;
                        state.LeftBaroPressure = 1000;
                        state.RightBaroPressure = 1000;
                        state.LeftBaroQfe = true;
                        state.RightBaroQfe = true;
                        break;
                        
                    case 3: // Level change indicators
                        Console.WriteLine("  Cycle 4: Level change brackets");
                        state.Speed = 300;
                        state.Heading = 45;
                        state.Altitude = 15000;
                        state.VerticalSpeed = 2500;
                        state.LvlIndicator = true;
                        state.LvlLeftBracket = true;
                        state.LvlRightBracket = true;
                        state.AltitudeIsFlightLevel = true;
                        state.LeftBaroPressure = 1025;
                        state.RightBaroPressure = 1025;
                        state.LeftBaroQnh = true;
                        state.RightBaroQnh = true;
                        break;
                        
                    case 4: // All indicators
                        Console.WriteLine("  Cycle 5: Multiple indicators combined");
                        state.Speed = 75; // Mach 0.75
                        state.Heading = 315;
                        state.Altitude = 25000;
                        state.VerticalSpeed = -1000;
                        state.SpeedIsMach = true;
                        state.HeadingIsTrack = true;
                        state.VsIsFpa = true;
                        state.SpeedManaged = true;
                        state.LatIndicator = true;
                        // NOTE: VsHorzIndicator is NOT set here because it interferes with V/S digit display
                        // Both use the same byte position (0x25) causing extra segments to light up
                        state.LeftBaroPressure = 2950; // 29.50 inHg
                        state.RightBaroPressure = 3005; // 30.05 inHg
                        state.LeftBaroQnh = true;
                        state.RightBaroQnh = true;
                        break;
                }
                
                frontpanel.UpdateDisplay(state);
            }
            else if (frontpanel.DeviceId.Device == Device.WinWingPap3)
            {
                var state = new Pap3State
                {
                    Speed = 200 + (cycle * 50),
                    Heading = (cycle * 72) % 360,
                    Altitude = 5000 + (cycle * 5000),
                    VerticalSpeed = (cycle - 2) * 1000,
                    PltCourseValue = (cycle * 90) % 360,
                    CplCourseValue = ((cycle * 90) + 180) % 360
                };
                frontpanel.UpdateDisplay(state);
            }
        }
        
        Thread.Sleep(2000);
    }
    
    Console.WriteLine("Display test completed.");
}

static void TestFrontpanelLeds(List<IFrontpanel> frontpanels)
{
    if (frontpanels.Count == 0)
    {
        Console.WriteLine("No frontpanel devices connected.");
        return;
    }
    
    Console.WriteLine("Testing LEDs - All ON...");
    
    foreach (var frontpanel in frontpanels)
    {
        if (frontpanel.DeviceId.Device == Device.WinWingFcu || 
            frontpanel.DeviceId.Device == Device.WinWingFcuLeftEfis || 
            frontpanel.DeviceId.Device == Device.WinWingFcuRightEfis || 
            frontpanel.DeviceId.Device == Device.WinWingFcuBothEfis)
        {
            var leds = new FcuEfisLeds
            {
                Loc = true,
                Ap1 = true,
                Ap2 = true,
                AThr = true,
                Exped = true,
                ExpedYellowBrightness = 255,
                Appr = true,
                LeftFd = true,
                RightFd = true,
                LeftLs = true,
                RightLs = true,
                LeftCstr = true,
                LeftWpt = true,
                LeftVorD = true,
                LeftNdb = true,
                LeftArpt = true,
                RightCstr = true,
                RightWpt = true,
                RightVorD = true,
                RightNdb = true,
                RightArpt = true
            };
            frontpanel.UpdateLeds(leds);
        }
        else if (frontpanel.DeviceId.Device == Device.WinWingPap3)
        {
            var leds = new Pap3Leds
            {
                N1 = true,
                Speed = true,
                Vnav = true,
                LvlChg = true,
                HdgSel = true,
                Lnav = true,
                VorLoc = true,
                App = true,
                AltHold = true,
                Vs = true,
                CmdA = true,
                CwsA = true,
                CmdB = true,
                CwsB = true,
                AtArm = true,
                FdL = true,
                FdR = true
            };
            frontpanel.UpdateLeds(leds);
        }
    }
    
    Thread.Sleep(2000);
    
    Console.WriteLine("Testing EXPED yellow brightness (FCU only)...");
    foreach (var frontpanel in frontpanels)
    {
        if (frontpanel.DeviceId.Device == Device.WinWingFcu || 
            frontpanel.DeviceId.Device == Device.WinWingFcuLeftEfis || 
            frontpanel.DeviceId.Device == Device.WinWingFcuRightEfis || 
            frontpanel.DeviceId.Device == Device.WinWingFcuBothEfis)
        {
            byte[] brightnessLevels = { 0, 64, 128, 192, 255 };
            foreach (byte brightness in brightnessLevels)
            {
                Console.WriteLine($"  EXPED yellow brightness: {brightness}");
                var leds = new FcuEfisLeds
                {
                    Exped = true,
                    ExpedYellowBrightness = brightness,
                    Loc = true,
                    Ap1 = true
                };
                frontpanel.UpdateLeds(leds);
                Thread.Sleep(500);
            }
        }
    }
    
    Console.WriteLine("Testing LEDs - All OFF...");
    
    foreach (var frontpanel in frontpanels)
    {
        if (frontpanel.DeviceId.Device == Device.WinWingFcu || 
            frontpanel.DeviceId.Device == Device.WinWingFcuLeftEfis || 
            frontpanel.DeviceId.Device == Device.WinWingFcuRightEfis || 
            frontpanel.DeviceId.Device == Device.WinWingFcuBothEfis)
        {
            frontpanel.UpdateLeds(new FcuEfisLeds());
        }
        else if (frontpanel.DeviceId.Device == Device.WinWingPap3)
        {
            frontpanel.UpdateLeds(new Pap3Leds());
        }
    }
    
    Console.WriteLine("LED test completed.");
}

static void TestFrontpanelBrightness(List<IFrontpanel> frontpanels)
{
    if (frontpanels.Count == 0)
    {
        Console.WriteLine("No frontpanel devices connected.");
        return;
    }
    
    Console.WriteLine("Testing brightness levels (0 -> 255)...");
    
    byte[] brightnessLevels = { 0, 51, 102, 153, 204, 255 };
    foreach (byte brightness in brightnessLevels)
    {
        Console.WriteLine($"Setting brightness to {brightness}");
        foreach (var frontpanel in frontpanels)
        {
            frontpanel.SetBrightness(brightness, brightness, brightness);
        }
        Thread.Sleep(500);
    }
    
    foreach (var frontpanel in frontpanels)
    {
        frontpanel.SetBrightness(128, 128, 128);
    }
    
    Console.WriteLine("Brightness test completed. Reset to default levels.");
}
