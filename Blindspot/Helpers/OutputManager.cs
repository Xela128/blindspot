﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ScreenReaderAPIWrapper;

namespace Blindspot.Helpers
{
    public interface IOutputManager
    {
        void OutputMessage(string message, bool interrupt = false, NavigationDirection navigationDirection = NavigationDirection.None);
        void OutputMessageToScreenReader(string message, bool interrupt = false);
        void OutputMessageGraphically(string message, bool interrupt = false, NavigationDirection navigationDirection = NavigationDirection.None);
    }
    
    /// <summary>
    /// A class that handles either screen reader or visual output
    /// </summary>
    public class OutputManager : IOutputManager
    {
        public IScreenReader ScreenReader { get; set; }
        public TaskbarNotifier Notifyer { get; set; }
        
        public OutputManager()
        {
            ScreenReader = new ScreenReader();
            ScreenReader.SapiEnabled = true;
            var screenWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            Notifyer = new TaskbarNotifier()
            {
                ContentClickable = false,
                TitleClickable = false,
                EnableSelectionRectangle = false,
                NormalContentColor = Color.White,
                HoverContentColor = Color.White,
                NormalTitleColor = Color.White,
                HoverTitleColor = Color.White,
                BackColor = Color.DarkBlue,
                TitleRectangle=new Rectangle(20, 5, screenWidth / 4 - 40, 25),
                ContentRectangle=new Rectangle(8, 25, screenWidth / 4 - 16, 60),
                KeepVisibleOnMousOver= true,
                ChangeContentResetsTimer = true,
                ChangeTitleResetsTimer = true
            };
        }

        public OutputManager(IScreenReader screenreader)
        {
            ScreenReader = screenreader;
        }

        private static OutputManager _instance;
        public static OutputManager Instance
        {
            get
            {
                return _instance ?? (_instance = new OutputManager());
            }
        }

        private bool UseScreenReader { get { return UserSettings.Instance.ScreenReaderOutput; } }
        private bool UseGraphicalOutput { get { return UserSettings.Instance.GraphicalOutput; } }
                
        public void OutputMessage(string message, bool interrupt = false, NavigationDirection navigationDirection = NavigationDirection.None)
        {
            if (UseScreenReader)
                OutputMessageToScreenReader(message, interrupt);
            if (UseGraphicalOutput)
                OutputMessageGraphically(message, interrupt, navigationDirection);
        }   

        public void OutputMessageToScreenReader(string message, bool interrupt = false)
        {
            ScreenReader.SayString(message, interrupt);
        }

        public void OutputMessageGraphically(string message, bool interrupt = false, NavigationDirection navigationDirection = NavigationDirection.None)
        {
            int appearingTime = 100, showingTime = 5000, disappearingTime = 200;
            switch (Notifyer.TaskbarState)
            {
                case TaskbarNotifier.TaskbarStates.appearing:
                case TaskbarNotifier.TaskbarStates.disappearing:
                    Notifyer.Hide();
                    Notifyer.Show(null, message, appearingTime, showingTime, disappearingTime);
                    break;
                case TaskbarNotifier.TaskbarStates.visible:
                    Notifyer.ContentText = message;
                    break;
                default:
                    Notifyer.Show(null, message, appearingTime, showingTime, disappearingTime);
                    break;
            }
        }
    }

    public enum NavigationDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
}