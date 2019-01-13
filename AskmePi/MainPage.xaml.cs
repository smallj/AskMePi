using Microsoft.Cognitive.LUIS;
using RaspberryModules.App.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Core;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AskmePi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SpeechRecognizer _contSpeechRecognizer;
        private readonly string _luiskey = "67c6110ee4784feb8491e71894319525";
        private readonly string _appId = "08d65b73-860f-4ee6-bc45-5e65143874bb";
        private readonly RGBLed _rgbLed = new RGBLed();
        private readonly SpeechSynthesizer _synthesizer = new SpeechSynthesizer();
        private readonly MediaPlayer _speechPlayer = new MediaPlayer();

        public MainPage()
        {
            this.InitializeComponent();
            _rgbLed.Init();
        }
        
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _contSpeechRecognizer = new SpeechRecognizer();
            await _contSpeechRecognizer.CompileConstraintsAsync();
            _contSpeechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
            _contSpeechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
            await _contSpeechRecognizer.ContinuousRecognitionSession.StartAsync();
            var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(i => i.Gender == VoiceGender.Female) ?? SpeechSynthesizer.DefaultVoice;
            _synthesizer.Voice = voice;
        }
        
        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            Debug.WriteLine($"Completed > Restart listening");
            await _contSpeechRecognizer.ContinuousRecognitionSession.StartAsync();
        }
        
        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            string speechResult = args.Result.Text;

            //To have the app respond to a keyword only put the call to handle result to this if statement
            //if (speechResult.ToLower().StartsWith("hey"))
            //{
            //    // CODE HERE
            //}

            Debug.WriteLine($"Text: {speechResult}");

            LuisClient client = new LuisClient(_appId, _luiskey);
            LuisResult result = await client.Predict(speechResult);

            Debug.WriteLine($"LUIS Result: {result.Intents.First().Name} {string.Join(",", result.Entities.Select(a => $"{a.Key}:{a.Value.First().Value}"))}");
            HandleLuisResult(result);
        }

        public void HandleLuisResult(LuisResult result)
        {
            if (!result.Intents.Any())
            {
                return;
            }

            switch (result.Intents.First().Name)
            {
                case "ControlLED":

                    if (result.Entities.Any(a => a.Key == "LedState"))
                    {
                        string ledState = result.Entities.First(a => a.Key == "LedState").Value.First().Value;

                        if (ledState == "on")
                        {
                            if (result.Entities.Any(a => a.Key == "LedColor"))
                            {
                                string ledColor = result.Entities.First(a => a.Key == "LedColor").Value.First().Value;

                                SayAsync($"Turning on the {ledColor} light.");

                                switch (ledColor)
                                {
                                    case "red":
                                        _rgbLed.TurnOnLed(LedStatus.Red);
                                        break;

                                    case "green":
                                        _rgbLed.TurnOnLed(LedStatus.Green);
                                        break;

                                    case "blue":
                                        _rgbLed.TurnOnLed(LedStatus.Blue);
                                        break;

                                    case "purple":
                                        _rgbLed.TurnOnLed(LedStatus.Purple);
                                        break;
                                    default:
                                        _rgbLed.TurnOnLed(LedStatus.White);
                                        break;
                                }
                            }
                            else
                                _rgbLed.TurnOnLed(LedStatus.White);
                        }
                        else if (ledState == "off")
                        {
                            SayAsync($"Turning off the light.");
                            _rgbLed.TurnOffLed();
                        }
                    }
                    break;
            }
        }

        public async Task SayAsync(string text)
        {
            using (var stream = await _synthesizer.SynthesizeTextToStreamAsync(text))
            {
                _speechPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
            }
            _speechPlayer.Play();
        }
    }
}

