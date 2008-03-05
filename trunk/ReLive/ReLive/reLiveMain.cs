using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using Google.GData.Photos;
using Google.GData.Extensions.MediaRss;
using System.Runtime.InteropServices;

namespace ReLive
{
    public partial class reLiveMain : Form
    {
        private String googleAuthToken = null;
        private String user = null;
        private PicasaService picasaService = new PicasaService("ReLive");
        private PicasaFeed picasaFeed = null;
        private List<PicasaEntry> albumList = new List<PicasaEntry>();
        MapBrowser mapWindow = new MapBrowser();
        String userPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString();

        //file browser view settings
        private const int LV_VIEW_ICON = 0x0000;
        private const int LVM_SETVIEW = 0x108E;
        private const string ListViewClassName = "SysListView32";
        private static readonly HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern bool EnumChildWindows(HandleRef hwndParent, EnumChildrenCallback lpEnumFunc, HandleRef lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(HandleRef hWnd, uint Msg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint RealGetWindowClass(IntPtr hwnd, [Out] StringBuilder pszType, uint cchType);
        private delegate bool EnumChildrenCallback(IntPtr hwnd, IntPtr lParam);
        private HandleRef listViewHandle;

        public reLiveMain()
        {
            InitializeComponent();
        }

        private void directoryBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                explorerText.Text = fbd.SelectedPath;
                fileBrowser.Url = new Uri(explorerText.Text);
            }
        }
          
        private bool checkAlbumExists(string albumName)
        {
            bool albumExists = false;
            AlbumQuery query = new AlbumQuery(PicasaQuery.CreatePicasaUri(this.user));

            PicasaFeed feed = picasaService.Query(query);

            foreach (PicasaEntry entry in feed.Entries)
            {
                if (entry.Title.Text.Equals(albumName)) albumExists = true;
            }
            return albumExists;
        }
        
        public bool checkFileExists(string fileName, string albumName)
        {
            bool fileExists = false;

            PhotoQuery query = new PhotoQuery(PicasaQuery.CreatePicasaUri(this.user, albumName));
            PicasaFeed feed = picasaService.Query(query);

            foreach (PicasaEntry entry in feed.Entries)
            {
                if(entry.Title.Text.Equals(fileName)) fileExists = true;
            }
            return fileExists;
        }

        public void createNewAlbum(string albumName, string desc)
        {
            if(!checkAlbumExists(albumName))
            {
                Uri feedUri = new Uri(this.picasaFeed.Post);
                AlbumEntry newEntry = new AlbumEntry();
                newEntry.Title.Text = albumName;
                newEntry.Summary.Text = desc;
                //newEntry.Published = ???

                PicasaEntry createdEntry = (PicasaEntry)picasaService.Insert(feedUri, newEntry);
            }
        }
        
        private void uploadDir_Click(object sender, EventArgs e)
        {
            if(this.googleAuthToken == null)
                login();
            if (this.googleAuthToken == null) //if service check fails, token set to null so need to check again
                return;
                
            if (explorerText.Text != null)
            {
                DirectoryInfo dir = new DirectoryInfo(explorerText.Text);
                FileInfo[] jpgFiles = dir.GetFiles("*.jpg");
                DateTime currTime = DateTime.Now;
                string currDate = currTime.ToString("yyyyMMdd");
                string desc = "Album Created " + currDate;
                Uri postUri = new Uri(PicasaQuery.CreatePicasaUri(this.user, currDate));

                uploadProgress.BringToFront();
                uploadProgress.Visible = true;
                uploadDir.Visible = false;
                uploadProgress.Step = 100 / (jpgFiles.Length + 1); //set size of progress
                uploadProgress.PerformStep();

                createNewAlbum(currDate, desc);        
                
                foreach (FileInfo file in jpgFiles)
                {
                    string fileStr = file.FullName;

                    uploadProgress.PerformStep();
                    if (!checkFileExists(file.Name, currDate))
                    {
                        FileStream fileStream = file.OpenRead();
                        PicasaEntry entry = this.picasaService.Insert(postUri, fileStream, "image/jpeg", fileStr) as PicasaEntry;
                    }
                }
                MessageBox.Show("Uploaded Album: " + currDate + " Successfully!");

                uploadDir.Visible = true;
                uploadProgress.Visible = false;
                uploadProgress.SendToBack();
                uploadProgress.Value = 0;
                UpdateAlbumFeed();
            }
            else
            {
                MessageBox.Show("Please select a directory");
            }
        }

        private void login()
        {
            if (this.googleAuthToken == null)
            {
                GoogleLogin loginDialog = new GoogleLogin(new PicasaService("reLive"));
                loginDialog.ShowDialog();

                this.googleAuthToken = loginDialog.AuthenticationToken;
                this.user = loginDialog.User;

                if (this.googleAuthToken == null)
                    MessageBox.Show("You will not be able to access your web albums without logging in!");
                else
                {
                    picasaService.SetAuthenticationToken(loginDialog.AuthenticationToken);
                    try
                    {
                        UpdateAlbumFeed();
                    }
                    catch (Google.GData.Client.GDataRequestException)
                    {
                        MessageBox.Show("You need to add the Picasaweb Service:\nLogin through your web browser and accept the terms of service.");
                        System.Diagnostics.Process.Start("www.picasaweb.google.com");
                        this.googleAuthToken = null;
                        login();
                    }
                }
            }
        }

        private void reLiveMain_Load(object sender, EventArgs e)
        {
            login();
            Directory.CreateDirectory(@userPictures + "\\reLive");
            fileBrowser.Navigate(userPictures + "\\reLive");
            //set file browser to view large icons
            FindListViewHandle();
            SendMessage(this.listViewHandle, LVM_SETVIEW, LV_VIEW_ICON, 0);

            //load previous config from SD card
            loadCurrentConfig();
        }
        public void loadCurrentConfig()
        {
            string memCardPath = findSDPath();
            if (memCardPath == "")
                return;

            Control[] configArray = { delayBox, distanceBox, faceCheck, haloCheck, latBox, lngBox, haloDistanceBox };
            StreamReader sr = File.OpenText(memCardPath + "\\config.txt");
            string input = sr.ReadLine();
            string[] inputArray = input.Split(',');
            for (int value = 0; value < inputArray.Length; value++ )
            {
                if (value == 2 || value == 3) //special cases for checkboxes
                {
                    ((CheckBox)configArray[value]).Checked = inputArray[value] == "True";
                    if (value == 3 && inputArray[value] == "False")
                        return;
                }
                configArray[value].Text = inputArray[value];
            }
        }

        public void UpdateAlbumFeed()
        {
            AlbumQuery query = new AlbumQuery();

            this.albumList.Clear();
            albumCalendar.BoldedDates = null;
            this.AlbumPicture.Image = null;
            this.mapLinkLabel.Hide();

            query.Uri = new Uri(PicasaQuery.CreatePicasaUri(this.user));
            this.picasaFeed = this.picasaService.Query(query);

            if (this.picasaFeed != null && this.picasaFeed.Entries.Count > 0)
            {
                foreach (PicasaEntry entry in this.picasaFeed.Entries)
                {
                    albumList.Add(entry);
                    albumCalendar.AddBoldedDate(entry.Published); //adds album dates to calendar as bold entries
                }
            }
            this.albumCalendar.UpdateBoldedDates();
            calendarUpdate();
        }

        private void calendarUpdate()
        {
            this.AlbumPicture.Image = null;
            this.mapLinkLabel.Hide();
            //this.AlbumInspector.SelectedObject = null;

            foreach (PicasaEntry entry in this.albumList)
            {
                if (this.albumCalendar.SelectionStart.ToShortDateString().Equals(entry.Published.ToShortDateString()))
                    setSelection(entry);
            }
        }

        private void albumCalendar_DateChanged(object sender, DateRangeEventArgs e)
        {
            calendarUpdate();
        }

        private void setSelection(PicasaEntry entry)
        {
            this.Cursor = Cursors.WaitCursor;
            MediaThumbnail thumb = entry.Media.Thumbnails[0];
            Stream stream = this.picasaService.Query(new Uri(thumb.Attributes["url"] as string));
            this.AlbumPicture.Image = new Bitmap(stream);
            //this.AlbumInspector.SelectedObject = new AlbumAccessor(entry);
            this.Cursor = Cursors.Default;
            //enable map link only when date selected
            this.mapLinkLabel.Show();
            //enable changing of map browser url temporarily
            mapWindow.albumMap.AllowNavigation = true;
            mapWindow.albumMap.Navigate("http://picasaweb.google.com/" + this.user + "/" + entry.getPhotoExtensionValue("name") + "/photo#map");
        }

        private void mapLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            mapWindow.ShowDialog();
        }

        private void fileBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            fileBrowser.Focus();
            explorerText.Text = fileBrowser.Url.LocalPath.ToString();
        }

        //file browser view stuff
        private void FindListViewHandle()
        {
            this.listViewHandle = NullHandleRef;

            EnumChildrenCallback lpEnumFunc = new EnumChildrenCallback(EnumChildren);
            EnumChildWindows(new HandleRef(this.fileBrowser, this.fileBrowser.Handle), lpEnumFunc, NullHandleRef);
        }

        private bool EnumChildren(IntPtr hwnd, IntPtr lparam)
        {
            StringBuilder sb = new StringBuilder(100);
            RealGetWindowClass(hwnd, sb, 100);
            if (sb.ToString() == ListViewClassName) // is this a windows list view?
            {
                // this is a windows list view control
                this.listViewHandle = new HandleRef(null, hwnd);
            }
            return true;
        }

        private void explorerText_Enter(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                explorerText.Text = explorerText.Text;
                fileBrowser.Url = new Uri(this.explorerText.Text);
            }
        }

        private void loginLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.googleAuthToken = null;
            login();
        }

        private void picasaLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://picasaweb.google.com/"); 
        }

        private string findSDPath()
        {
            string memCardPath = "";
            int rootNum = 0;
            DriveInfo[] allDrives = DriveInfo.GetDrives();  //get a list of all drives
            foreach (DriveInfo drvInfo in allDrives)        //loop through all drives
            {
                DirectoryInfo di = drvInfo.RootDirectory;
                if (drvInfo.DriveType.Equals(DriveType.Removable) && drvInfo.IsReady)
                {
                    memCardPath = di.FullName;
                }
                rootNum++;
            }
            return memCardPath;
        }

        private void fileCopy(string srcdir, string destdir, bool recursive)
        {
            DirectoryInfo dir;
            DirectoryInfo ddir;
            FileInfo[] files;
            FileInfo[] destFiles;
            DirectoryInfo[] dirs;
            string tmppath;
            bool fileExists = false;

            //determine if the destination directory exists, if not create it
            if (!Directory.Exists(destdir))
            {
                Directory.CreateDirectory(destdir);
            }

            dir = new DirectoryInfo(srcdir);
            ddir = new DirectoryInfo(destdir);

            //if the source dir doesn't exist, throw
            if (!dir.Exists)
            {
                throw new ArgumentException("source dir doesn't exist -> " + srcdir);
            }

            //get all files in the current dir
            files = dir.GetFiles();
            destFiles = ddir.GetFiles();

            //loop through each file
            foreach (FileInfo file in files)
            {
                
                foreach (FileInfo destFile in destFiles)
                {
                    if (file.Name.Equals(destFile.Name))
                    {
                        //MessageBox.Show(destFile.Name + " exists..skipping.. Please delete to upload.");
                        fileExists = true;
                    }
                }
                
                if (!fileExists)
                {
                    //create the path to where this file should be in destdir
                    tmppath = Path.Combine(destdir, file.Name);

                    MessageBox.Show("Copying " + tmppath);
                    //copy file to dest dir
                    file.CopyTo(tmppath, false);
                }
                else fileExists = false;
            }

            //cleanup
            destFiles = null;
            files = null;

            //if not recursive, all work is done
            if (!recursive)
            {
                return;
            }

            //otherwise, get dirs
            dirs = dir.GetDirectories();

            //loop through each sub directory in the current dir
            foreach (DirectoryInfo subdir in dirs)
            {
                //create the path to the directory in destdir
                tmppath = Path.Combine(destdir, subdir.Name);

                //recursively call this function over and over again
                //with each new dir.
                fileCopy(subdir.FullName, tmppath, recursive);
            }

            //cleanup
            dirs = null;
            dir = null;
        }

        private void copySubDirs(string flashRoot, string path)
        {
            //String msg = "Copying Subdirectories: ";
            string[] subDirs = Directory.GetDirectories(flashRoot);
            /*
            foreach (string subDir in subDirs)
            {
                msg = msg + subDir + "\n";
            }
            MessageBox.Show(msg);
             */
            fileCopy(flashRoot, path, true);  //make third parameter true for recursive copy
        }

        private void retrieveSD_Click(object sender, EventArgs e)
        {
            string memCardPath = findSDPath();
            string defPath = @userPictures + "\\reLive";

            if (memCardPath == "")
                MessageBox.Show("Sorry, but no SD card was detected in the drive.\nPlease insert your memory card and try again.");
            else
            {
                MessageBox.Show("Card Drive detected to be: " + memCardPath + "\nCopying contents to: " + defPath);
                copySubDirs(memCardPath, defPath);
            }
        }

        private void distanceBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Double.Parse(distanceBox.Text + "0");
            }
            catch (FormatException)
            {
                distanceBox.Text = "";
            }
        }

        private void haloDistanceBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Double.Parse(haloDistanceBox.Text + "0");
            }
            catch (FormatException)
            {
                haloDistanceBox.Text = "";
            }
        }

        private void zipBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Int32.Parse(zipBox.Text);
            }
            catch (FormatException)
            {
                zipBox.Text = "";
            }
        }

        private void geoCode_Click(object sender, EventArgs e)
        {
            //c# geocoding example
            string lat = "";
            string lng = "";
            string address = streetBox.Text + ", " + cityBox.Text + ", " + stateBox.Text + ", " + zipBox.Text;
 
            try
            {
                System.Net.WebClient client = new System.Net.WebClient();
                string page = client.DownloadString("http://maps.google.com/maps?q=" + address);
                int begin = page.IndexOf("markers:");
                string str = page.Substring(begin);
                int end = str.IndexOf(",image:");
                str = str.Substring(0, end);

                //Parse out Latitude
                lat = str.Substring(str.IndexOf(",lat:") + 5);
                lat = lat.Substring(0, lat.IndexOf(",lng:"));
                //Parse out Longitude
                lng = str.Substring(str.IndexOf(",lng:") + 5);
            }

            catch (Exception ex)
            {
                MessageBox.Show("An Error Occured Loading Geocode!\nCheck that a valid address has been entered.", "An Error Occured Loading Geocode!");
            }

            latBox.Text = lat;
            lngBox.Text = lng;
        }

        //validate that all camera settings have been set
        private bool validateSettings()
        {
            string memCardPath = findSDPath();
            string message = "";

            if (memCardPath == "")
            {
                MessageBox.Show("Sorry, but no SD card was detected in the drive.\nPlease insert your memory card and try again.");
                return false;
            }
            if(delayBox.Text == "")
            {
                message += "Please specify a minimum time between pictures!\n";
            }
            if(distanceBox.Text == "")
            {
                message += "Please specify a minimum distance!\n";
            }
            if ((latBox.Text == "" || lngBox.Text == "") && haloCheck.Checked)
            {
                message += "Search for a GPS location, or disable the location halo.";
            }
            if (message == "")
                return true;

            MessageBox.Show(message);
            return false;
        }
        private void writeConfig_Click(object sender, EventArgs e)
        {
            if (!validateSettings())
                return;

            string memCardPath = findSDPath();

            double distance = 0;
            double range = 0;

            String userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            TextWriter config = new StreamWriter(findSDPath() + "\\config.txt", false);
            
            config.WriteLine(delayBox.Text + "," + distance + "," + faceCheck.Checked + "," +
                haloCheck.Checked + "," + latBox.Text + ",-" + lngBox.Text + "," + range);
            config.Close();

            MessageBox.Show("Config written to " + memCardPath + "config.txt");
        }

        private void haloCheck_CheckedChanged(object sender, EventArgs e)
        {
            haloSearchGroup.Visible = !haloSearchGroup.Visible;
            gpsGroup.Visible = !gpsGroup.Visible;
        }

        private void viewHalo_Click(object sender, EventArgs e)
        {
            if ((latBox.Text != "") && (lngBox.Text != ""))
                System.Diagnostics.Process.Start("http://maps.google.com/maps?q=" + latBox.Text + ",-" + lngBox.Text + "&t=h");
            else
                MessageBox.Show("Search for a location first!");
        }

        
    }
}