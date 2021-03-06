﻿using Blindspot.Helpers;
using ScreenReaderAPIWrapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Blindspot
{
    public partial class FirstTimeWizard : Form
    {
        private int currentStep = 0;
        private List<GroupBox> stepBoxes;
        private XDocument keyboardDescriptions;
        private CultureInfo CurrentUICulture
        {
            get { return System.Threading.Thread.CurrentThread.CurrentUICulture; }
        }

        public FirstTimeWizard()
        {
            InitializeComponent();
            SetupLocalFolder();
            SetupLanguageBox();
            LoadKeyboardDescriptions();
            SetupKeyboardSettingsBox();
            stepBoxes = new List<GroupBox>() { step1GroupBox, step2GroupBox, step3GroupBox };
        }
        
        private void FirstTimeWizard_Load(object sender, EventArgs e)
        {
            keyboardDescriptionBox.Text = GetKeyboardDescription();
        }

        private void SetupLocalFolder()
        {
            // check for existance of local appdata folder
            if (LocalAppDataFoldersExist())
                return; // we're all good here

            var commonAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Blindspot");
            if (!Directory.Exists(commonAppDataPath))
                throw new DirectoryNotFoundException("The Blindspot common program data directory was not found. Please try rerunning the installer.");

            var localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Blindspot");
            DirectoryCopy(commonAppDataPath, localAppDataPath, true);
        }

        private static bool LocalAppDataFoldersExist()
        {
            var localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Blindspot");
            var KeyboardPath = Path.Combine(localAppDataPath, "Keyboard Layouts");
            return (Directory.Exists(localAppDataPath) && Directory.Exists(KeyboardPath));
        }


        private void LoadKeyboardDescriptions()
        {
            try
            {
                var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Blindspot\Keyboard Layouts\Layout Descriptions.xml");
                if (!File.Exists(fullPath))
                    return;
                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    keyboardDescriptions = XDocument.Load(stream);
                }
            }
            catch (Exception)
            {
                keyboardDescriptions = null;
            }
        }

        private void SetupLanguageBox()
        {
            var langList = new List<CultureInfo>();
            langList.Add(new CultureInfo("de"));
            langList.Add(new CultureInfo("en"));
            langList.Add(new CultureInfo("es"));
            langList.Add(new CultureInfo("fr"));
            langList.Add(new CultureInfo("sv"));
            languageBox.DataSource = langList;
            languageBox.DisplayMember = "NativeName";
            languageBox.ValueMember = "LCID";
            int currentUICultureID = CurrentUICulture.LCID;
            languageBox.SelectedValue = currentUICultureID;
            
            // in case we can't set the culture to something we're expecting, make sure the first option is selected by default
            if (languageBox.SelectedIndex == -1) languageBox.SelectedIndex = 0;
        }

        private void SetupKeyboardSettingsBox()
        {
            var directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Blindspot\Keyboard Layouts");
            var keyboardFiles = Directory.GetFiles(directoryPath, "*.txt");
            Dictionary<string, string> fileAndPath = new Dictionary<string, string>();
            foreach (string keyboardFile in keyboardFiles)
            {
                int startIndex = keyboardFile.LastIndexOf('\\') + 1;
                fileAndPath.Add(keyboardFile.Substring(startIndex, keyboardFile.Length - startIndex - 4), keyboardFile);
            }
            // bind this dictionary of strings to the box
            keyboardStyleBox.DataSource = new BindingSource(fileAndPath, null);
            keyboardStyleBox.DisplayMember = "Key";
            keyboardStyleBox.ValueMember = "Value";
            var screenReader = OutputManager.Instance.ScreenReader;
            string screenReaderName = screenReader.ScreenReaderName.ToLower();
            if ((screenReaderName == "jfw" || screenReaderName == "jaws" || screenReaderName == "jaws for windows")
                && fileAndPath.ContainsKey("Modern"))
            {
                keyboardStyleBox.SelectedValue = fileAndPath["Modern"];
            }
            else if (fileAndPath.ContainsKey("Standard"))
            {
                keyboardStyleBox.SelectedValue = fileAndPath["Standard"];
            }
        }

        private string GetKeyboardDescription()
        {
            string notFound = StringStore.NoDescriptionAvailable;
            if (keyboardDescriptions == null) return notFound;
            var selectedKeyboardLayout = ((KeyValuePair<string, string>)keyboardStyleBox.SelectedItem).Key;
            var layoutElement = keyboardDescriptions.Root.Elements().FirstOrDefault(l => l.Attribute("name").Value == selectedKeyboardLayout);
            if (layoutElement == null) return notFound;
            var descriptionElement = layoutElement.Elements().FirstOrDefault(l => l.Attribute("language").Value == CurrentUICulture.TwoLetterISOLanguageName);
            if (descriptionElement == null) return notFound;
            return descriptionElement.Value;
        }

        private void skipButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            if (currentStep == 0) return;
            stepBoxes[currentStep].Visible = false;
            stepBoxes[--currentStep].Visible = true;
            if (currentStep == 0) backButton.Enabled = false;
            if (currentStep < stepBoxes.Count - 1)
            {
                if (!nextButton.Enabled) nextButton.Enabled = true;
                saveButton.Enabled = false;
            }
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            if (currentStep == stepBoxes.Count - 1) return;
            stepBoxes[currentStep].Visible = false;
            stepBoxes[++currentStep].Visible = true;
            if (currentStep > 0 && !backButton.Enabled) backButton.Enabled = true;
            if (currentStep == stepBoxes.Count - 1)
            {
                nextButton.Enabled = false;
                saveButton.Enabled = true;
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            var selectedKeyboardLayoutPath = keyboardStyleBox.SelectedValue as string;
            if (!keyboardStyleNoChangeBox.Checked && !String.IsNullOrEmpty(selectedKeyboardLayoutPath))
            {
                var targetDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Blindspot\Settings");
                File.Copy(selectedKeyboardLayoutPath, Path.Combine(targetDirectoryPath, "hotkeys.txt"), true);
            }
            UserSettings.Instance.UILanguageCode = (int)languageBox.SelectedValue;
            UserSettings.Instance.KeyboardLayoutName = keyboardStyleBox.Text;
            UserSettings.Instance.ScreenReaderOutput = screenReaderOutputBox.Checked;
            UserSettings.Instance.GraphicalOutput = graphicalOutputBox.Checked;
            UserSettings.Instance.DontShowFirstTimeWizard = true;
            UserSettings.Save();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void keyboardStyleBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            keyboardDescriptionBox.Text = GetKeyboardDescription();
        }

        private void keyboardStyleNoChangeBox_CheckedChanged(object sender, EventArgs e)
        {
            keyboardStyleBox.Enabled = !keyboardStyleNoChangeBox.Checked;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

    }
}
