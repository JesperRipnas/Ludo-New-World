﻿using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using LudoNewWorld.Classes;
using System.Numerics;
using Windows.Media.Playback;
using Windows.UI.Xaml.Media.Imaging;

namespace LudoNewWorld
{
    /// ***********************************************************************************\\\
    /// In the case the buttons at start isnt visible, try to adjust the window size a bit \\\
    /// ***********************************************************************************\\\

    public sealed partial class MainPage : Page
    {
        public static Rect bounds = ApplicationView.GetForCurrentView().VisibleBounds;
        public static float scaleWidth, scaleHeight;
        public static float DesignWidth = 1920;
        public static float DesignHeight = 1080;
        public static int gameState = 0;
        public static Faction playerFaction;
        public static MediaPlayer mPlayer = new MediaPlayer();
        public static MediaPlayer mPlayerr = new MediaPlayer();
        public static double currentVolume = 0.5;
        public static int volumeLevel = 5;
        public static bool volumeMute = false;
        public static bool nextRoundAvailable = true;
        public static bool showDice = true;
        private static int gameTickCounter = 0;
        public Vector3 scaleVector3Variable = new Vector3(DesignWidth, DesignHeight, 1);
        GameEngine gameEngine = new GameEngine();

        
        public MainPage()
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size { Width = 1280, Height = 720 });
            ApplicationView.PreferredLaunchViewSize = new Size(1920, 1080);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            this.InitializeComponent();
            Window.Current.SizeChanged += Current_SizeChanged;
            Scaler.SetScale();
            Sound.SoundPlay();
            GraphicHandler.LoadResources();            
        }

        /// <summary>
        /// Window size change event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            Scaler.SetScale();

            scaleVector3Variable.X = (float)mainGrid.ActualWidth / DesignWidth;
            scaleVector3Variable.Y = (float)mainGrid.ActualHeight / DesignHeight;

            var xMargin = mainGrid.ActualWidth - DesignWidth;
            var yMargin = mainGrid.ActualHeight - DesignHeight;

            // Scale assets based on the true size of the game window
            MenuField.Scale = scaleVector3Variable;
            FactionField.Scale = scaleVector3Variable;
            Dice.Scale = scaleVector3Variable;
            PlayingMenu.Scale = scaleVector3Variable;

            //Scale margin of assets based on the actual window size
            MenuField.Margin = new Thickness(1, 1, xMargin, yMargin);
            FactionField.Margin = new Thickness(1, 1, xMargin, yMargin);
            Dice.Margin = new Thickness(1, 1, xMargin, yMargin);
            PlayingMenu.Margin = new Thickness(1, 1, xMargin, yMargin);
        }

        /// <summary>
        /// Create event, create objects and variables to be used in Win2D
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void GameCanvas_CreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
        {
            GraphicHandler.CreateResources(sender, args);
        }

        /// <summary>
        /// Draw event, draws to the canvas once per update
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void GameCanvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            GraphicHandler.Draw(sender, args);
        }

        /// <summary>
        /// Update event, happens 60 times a second
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void GameCanvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            gameTickCounter++;
            GameEngine.moveAITick++;
            GameEngine.SwitchPlayerTimer++;

            if (GameEngine.GetGameActive())
            {
                if (gameTickCounter >= 60)
                {
                    TriggerRollButton();
                    gameTickCounter = 0;
                }
                if(nextRoundAvailable)
                {
                    gameEngine.NextRound();
                }
            }
            if (GameEngine.moveAITick > 200) GameEngine.moveAITick = 0;
        }
        private void GameCanvas_Loaded(object sender, RoutedEventArgs e) { }

        /// <summary>
        /// Handles user pointer input, saves the X/Y coordinates in a vector2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Vector2 cords = new Vector2((float)e.GetCurrentPoint(GameCanvas).Position.X, (float)e.GetCurrentPoint(GameCanvas).Position.Y);
            Vector2 scaledVector = Scaler.ClickCords(cords);
            gameEngine.boat = gameEngine.CheckForShipsOnMousePressed(scaledVector);
            gameEngine.tile = gameEngine.CheckForTileOnMousePressed(scaledVector);
            if (GameEngine.GetGameActive())
            {
                if(gameEngine.boat != null)
                {
                }
            }
        }

        /// <summary>
        /// Will enable or disable the Roll button in the game based on <see cref="GameEngine.PlayerTurn"/> has as value
        /// Anything else than 1 will disable the button
        /// </summary>
        public async void TriggerRollButton()
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (showDice)
                {
                    Dice.Visibility = Visibility.Visible;
                }
                else if(!showDice)
                {
                    Dice.Visibility = Visibility.Collapsed;
                }
            }).AsTask();
        }

        private void btnRoll_Click(object sender, RoutedEventArgs e)
        {
            Sound.DiceSound();
            GameEngine.LastDiceRoll = GraphicHandler.ScrambleDice(GameEngine.PlayerTurn);
            GameEngine.diceRolled = true;
            showDice = false;
            Dice.Visibility = Visibility.Collapsed;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            MenuField.Visibility = Visibility.Collapsed;
            FactionField.Visibility = Visibility.Visible;
        }

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            MenuField.Visibility = Visibility.Collapsed;
            QuitConfirm_Popup.IsOpen = true;
        }

        private void btnBritain_Click(object sender, RoutedEventArgs e)
        {
            gameEngine.StartGame(Faction.Britain);
            FactionField.Visibility = Visibility.Collapsed;
            Dice.Visibility = Visibility.Visible;
            gameState = 1;
            Thickness margin = new Thickness(30, 179, 0, 0);
            btnRoll.Margin = margin;
        }

        private void btnDutch_Click(object sender, RoutedEventArgs e)
        {
            gameEngine.StartGame(Faction.Dutch);
            FactionField.Visibility = Visibility.Collapsed;
            Dice.Visibility = Visibility.Visible;
            gameState = 1;
            Thickness margin = new Thickness(1773, 179, 0, 0);
            btnRoll.Margin = margin;
        }

        private void btnFrance_Click(object sender, RoutedEventArgs e)
        {
            gameEngine.StartGame(Faction.France);
            FactionField.Visibility = Visibility.Collapsed;
            Dice.Visibility = Visibility.Visible;
            gameState = 1;
            Thickness margin = new Thickness(30, 851, 0, 0);
            btnRoll.Margin = margin;
        }
 
        private void btnSpain_Click(object sender, RoutedEventArgs e)
        {
            gameEngine.StartGame(Faction.Spain);
            FactionField.Visibility = Visibility.Collapsed;
            Dice.Visibility = Visibility.Visible;
            gameState = 1;
            Thickness margin = new Thickness(1773, 851, 0, 0);
            btnRoll.Margin = margin;
        }
        private void BtnMuteVolume_Click(object sender, RoutedEventArgs e)
        {
            if (!volumeMute && volumeLevel > 0)
            {
                mPlayer.Volume = 0;
                volumeMute = true;
                muteButtonChange.Source = new BitmapImage(new Uri(base.BaseUri, @"/Assets/Images/Menu/volumemute.png"));
            }
            else if (volumeMute && volumeLevel > 0)
            {
                mPlayer.Volume = currentVolume;
                volumeMute = false;
                muteButtonChange.Source = new BitmapImage(new Uri(base.BaseUri, @"/Assets/Images/Menu/volumeunmute.png"));
            }
        }
        private void BtnLowerVolume_Click(object sender, RoutedEventArgs e)
        {
            if (!volumeMute && volumeLevel > 0)
            {
                currentVolume -= 0.1;
                mPlayer.Volume -= 0.1;
                volumeLevel--;
                volumeSlider.Source = new BitmapImage(new Uri(base.BaseUri, @"/Assets/Images/Menu/menu" + volumeLevel + ".png"));
                if(volumeLevel == 0)
                {
                    muteButtonChange.Source = new BitmapImage(new Uri(base.BaseUri, @"/Assets/Images/Menu/volumemute.png"));
                }
            }
        }

        private void BtnRaiseVolume_Click(object sender, RoutedEventArgs e)
        {
            if(!volumeMute && volumeLevel < 10)
            {
                currentVolume += 0.1;
                mPlayer.Volume += 0.1;
                volumeLevel++;
                volumeSlider.Source = new BitmapImage(new Uri(base.BaseUri, @"/Assets/Images/Menu/menu" + volumeLevel + ".png"));
                if (volumeLevel > 0)
                {
                    muteButtonChange.Source = new BitmapImage(new Uri(base.BaseUri, @"/Assets/Images/Menu/volumeunmute.png"));
                }
            }
        }

        private void Help_Quit_Click(object sender, RoutedEventArgs e)
        {
            Popup2.IsOpen = false;
            ExitMenuConfirm_Popup.IsOpen = true;
        }

        private void Instruct_btn_Click(object sender, RoutedEventArgs e)
        {
            Popup2.IsOpen = false;
            MyPopup.IsOpen = false;
            InstructPopup.IsOpen = true;
        }

        private void Credit_btn_Click(object sender, RoutedEventArgs e)
        {
            Popup2.IsOpen = false;
            MyPopup.IsOpen = false;
            CreditPopup.IsOpen = true;
            Sound.CrediSound(CreditPopup.IsOpen);
        }

        private void instruct_return_Click(object sender, RoutedEventArgs e)
        {
            InstructPopup.IsOpen = false;
            MyPopup.IsOpen = true;
            Popup2.IsOpen = true;
        }

        private void credit_return_Click(object sender, RoutedEventArgs e)
        {
            CreditPopup.IsOpen = false;
            MyPopup.IsOpen = true;
            Popup2.IsOpen = true;
            Sound.CrediSound(CreditPopup.IsOpen);
        }

        private void QuitConfirm_yes_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void QuitConfirm_no_Click(object sender, RoutedEventArgs e)
        {
            MenuField.Visibility = Visibility.Visible;
            QuitConfirm_Popup.IsOpen = false;
        }

        private void ExitConfirm_yes_Click(object sender, RoutedEventArgs e)
        {
            gameState = 2;

            if (gameState == 2)
            {
                ParentPopup.IsOpen = false;
                ExitMenuConfirm_Popup.IsOpen = false;
                MenuField.Visibility = Visibility.Visible;
                Dice.Visibility = Visibility.Collapsed;
            }
        }

        private void ExitConfirm_no_Click(object sender, RoutedEventArgs e)
        {
            ExitMenuConfirm_Popup.IsOpen = false;
            Popup2.IsOpen = true;
        }

        private void BtnMenuHelp_Click(object sender, RoutedEventArgs e)
        {
             
            if (gameState == 1)
            {
                ParentPopup.IsOpen = true;

                if (ParentPopup.IsOpen == true)
                {
                    ParentPopup_grid.Visibility = Visibility.Visible;
                    MyPopup.IsOpen = true;
                    Popup2.IsOpen = true;
                }
                else if (ParentPopup.IsOpen == false)
                {
                    ParentPopup_grid.Visibility = Visibility.Collapsed;
                    Popup2.IsOpen = false;
                    MyPopup.IsOpen = false;
                }

            }
        }
       
        public Popup winnerPOP
        {
            get { return winnerPop ; }
        }
        public TextBlock WinnerTextBlock
        {
            get { return winnerText; }
        }
       
    }
}
