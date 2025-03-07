using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

// Last update: 07/03/2025                

class CopyFolder
{
    public string SourceFolder { get; set; }
    public string DestinationFolder { get; set; }       // this will be outsourced anyway to a pre defined folder, no need to verify anything here.
    public bool ValidSource { get; set; }
    public CopyFolder(string sourceFolder, string DestinationFolder)
    {
        this.DestinationFolder = DestinationFolder;
        this.SourceFolder = sourceFolder;
        perform_validation_check();
    }
    public void perform_validation_check()
    {
        if (IsValidFolderPath())     // Check whether or not it is even relevant 
        {
            FixSourceFolderPath();          // Fix minor mistakes **before** checking existence
            if (IsSourceFolderExisting())
            {
                //  Console.WriteLine("Source folder is valid");
                ValidSource = true;
            }
            else
            {
                //   Console.WriteLine("Source folder is invalid, does not exist.");
                ValidSource = false;
            }
        }
        else
        {
            ValidSource = false;
            //  Console.WriteLine("Source folder is invalid");
        }
    }
    public virtual bool IsSourceFolderExisting()
    {
        try
        {
            // Validate and normalize the path
            if (string.IsNullOrWhiteSpace(SourceFolder)) return false;
            string fullPath = Path.GetFullPath(SourceFolder.Trim());

            // Check if the directory exists
            if (!Directory.Exists(fullPath)) return false;

            // Additional validation: Ensure it's a real directory (not a broken symlink)
            var dirInfo = new DirectoryInfo(fullPath);
            if ((dirInfo.Attributes & FileAttributes.Directory) == 0) return false;

            // Avoid unnecessary security checks (can throw exceptions on system folders)
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is PathTooLongException || ex is NotSupportedException)
        {
            Console.WriteLine($"Error checking directory: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return false;
        }
    }
    public virtual bool IsDestinationFolderExisting(string USB_Destination, int num)
    {
        try
        {
            string full_name = USB_Destination + num.ToString();
            string fullPath = Path.GetFullPath(full_name.Trim());

            // Check if the directory exists
            if (!Directory.Exists(fullPath)) return true;       // doesnt't exist, we can create one now
            else return false;                                  // already in use
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is PathTooLongException || ex is NotSupportedException)
        {
            Console.WriteLine($"Error checking directory: {ex.Message}");
            return false;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return false;
        }
    }
    public virtual void FixSourceFolderPath()
    {
        SourceFolder = SourceFolder.Trim();

        // Replace all types of slashes with the correct separator
        SourceFolder = SourceFolder.Replace('/', '\\');

        // Remove redundant backslashes
        while (SourceFolder.Contains("\\\\"))
        {
            SourceFolder = SourceFolder.Replace("\\\\", "\\");
        }

        // Ensure absolute path
        try
        {
            SourceFolder = Path.GetFullPath(SourceFolder);
        }
        catch (Exception)
        {
            Console.WriteLine("Invalid path detected.");
        }

    }
    public virtual bool IsValidFolderPath()     // dead on arrival, 
    {
        if (string.IsNullOrWhiteSpace(SourceFolder)) return false;

        //check if there are any illegal characters
        char[] invalidChars = { ':', '*', '?', '"', '<', '>', '|' };

        foreach(char c in SourceFolder)
        {
            if(invalidChars.Contains(c))
            {
                if(c==':' && SourceFolder[1]==':' && SourceFolder[0]== 'C')
                {
                    // ignore special case
                }
                else
                {
                    return false;
                }
            }
        }
        // Check if path is too long
        if (SourceFolder.Length > 260) return false;

        // Extract folder name (last part of the path)
        string folderName = Path.GetFileName(SourceFolder.TrimEnd(Path.DirectorySeparatorChar));

        // Check for reserved names
        string[] reservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                               "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        if (reservedNames.Contains(folderName, StringComparer.OrdinalIgnoreCase)) return false;

        return true;
    }
    public virtual void StartProcess_CopyFolder(bool key, int num,string USB_destination_folder)
    {
        if(ValidSource)     
        {
            if(key)
            {
                Console.WriteLine(" Source folder valid, we can stary the copy process");
            }
            else
            {
                Console.WriteLine(" Source folder valid, we can stary the copy process in dry mode");
            }
            CopyDirectory("C:\\Users\\User\\Desktop\\target_folder", USB_destination_folder + num.ToString(), key);
        }
        else
        {
            Console.WriteLine("Source folder not valid, can't copy anyhting");
        }
    }
    protected virtual void CopyDirectory(string mySourceFolder,string myDestinationFolder ,bool key)
    {
        if (!Directory.Exists(mySourceFolder))  // unlikely to happen, just in case
        {
            Console.WriteLine("Source directory does not exist.");
            return;
        }

        string[] allowed_extensions = { "txt", "jpg", "png" };      // some reserved extension for example purposes

        // Create destination directory if it doesn't exist
        try
        {
            // Check if the directory exists before attempting creation
            if (!Directory.Exists(myDestinationFolder))
            {
                Directory.CreateDirectory(myDestinationFolder);
                Console.WriteLine($"Created destination directory: {myDestinationFolder}");
            }
            else
            {
                Console.WriteLine($"Destination directory already exists: {myDestinationFolder}");
            }
        }
        catch (Exception e)     // In case the user gives a junk input
        {
            Console.WriteLine($"Error: {e.Message}");
        }
        // copy all files (that have the right extension)
        foreach (string file in Directory.GetFiles(mySourceFolder))
        {
            string destFile = Path.Combine(myDestinationFolder, Path.GetFileName(file));
            string extension = Path.GetExtension(file).ToLower().TrimStart('.');     // Get extension without leading dot
            if (allowed_extensions.Contains(extension))     // 'contains' basically just checks if our string array has anything like the that matches the 'extension' string
            {
                if (key == true)   // This gives us the control over dry mode 
                {
                    File.Copy(file, destFile, true); // Corrected parameters, copy from file to destination file
                    Console.WriteLine($"Copied {file} -> {destFile}");
                }
                else
                {
                    Console.WriteLine($"Dry run: {file} -> {destFile} (Not copied)");
                }
            }
        }
        // copy all subdirectories recursively 
        foreach (string subdir in Directory.GetDirectories(mySourceFolder))
        {
            string destSubDir = Path.Combine(myDestinationFolder, Path.GetFileName(subdir));
            CopyDirectory(subdir, destSubDir, key);
        }
    }
    public virtual void StartProcess_OrganizeFiles(bool key)
    {
        if (ValidSource)
        {
            if (key)
            {
                Console.WriteLine(" Source folder valid, we can stary the copy process");
            }
            else
            {
                Console.WriteLine(" Source folder valid, we can stary the copy process in dry mode");
            }
            OrganizeFiles(SourceFolder, key);
        }
        else
        {
            Console.WriteLine("Source folder not valid, can't copy anyhting");
        }
    }
    protected virtual void OrganizeFiles(string mySourceFolder, bool key)       
    {
        string main_destination = "C:\\Users\\User\\Desktop\\target_folder";
        string[] myDestinationFolders = {"C:\\Users\\User\\Desktop\\target_folder\\text_files" 
        ,"C:\\Users\\User\\Desktop\\target_folder\\PNG_files","C:\\Users\\User\\Desktop\\target_folder\\JPG_files"};        // we can add more later...
        string[] allowed_extensions = { "txt", "png", "jpg" };      // some reserved extension for example purposes
        if (!Directory.Exists(mySourceFolder))  // unlikely to happen, just in case
        {
            Console.WriteLine("Source directory does not exist.");
            return;
        }


        try
        {
            // Check if the directory exists before attempting creation
            if (!Directory.Exists(main_destination))
            {
                Directory.CreateDirectory(main_destination);
                Console.WriteLine($"Created destination directory: {main_destination}");
            }
            else
            {
                // no need to write this each and every time, gets annoying
                // Console.WriteLine($"Destination directory already exists: {main_destination}");
            }
        }
        catch(Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");

        }
        foreach (string current_destination_folder in myDestinationFolders)     // generate all sub directories
        {
            // Create destination directory if it doesn't exist
            try
            {
                // Check if the directory exists before attempting creation
                if (!Directory.Exists(current_destination_folder))
                {
                    Directory.CreateDirectory(current_destination_folder);
                    Console.WriteLine($"Created destination directory: {current_destination_folder}");
                }
                else
                {
                    // no need to write this each and every time, gets annoying
                   // Console.WriteLine($"Destination directory already exists: {current_destination_folder}");
                }
            }
            catch (Exception e)     // In case the user gives a junk input
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        // copy all files (that have the right extension)
        int counter = 0;        // just to control the current allowed extension, 
        foreach (string current_destination_folder in myDestinationFolders)
        {
            string current_allowed_extension=allowed_extensions[counter];
            foreach (string file in Directory.GetFiles(mySourceFolder))
            {
                string destFile = Path.Combine(current_destination_folder, Path.GetFileName(file));
                string extension = Path.GetExtension(file).ToLower().TrimStart('.');     // Get extension without leading dot
                if (extension==current_allowed_extension)     
                {
                    if (key == true)   // This gives us the control over dry mode 
                    {

                        File.Copy(file, destFile, true); // Corrected parameters, copy from file to destination file
                        Console.WriteLine($"Copied {file} -> {destFile}");
                    }
                    else
                    {
                        Console.WriteLine($"Dry run: {file} -> {destFile} (Not copied)");
                    }
                }
            }
            counter++;
        }
        // copy all files accordingly recursively 
        foreach (string subdir in Directory.GetDirectories(mySourceFolder))
        {
            OrganizeFiles(subdir, key);
        }

    }
}


public class HelloWorld
{
    public static void Main(string[] args)
    {
        int max_amount_of_files = 1000;
        Console.WriteLine("This script can run in dry or active mode");
        Console.WriteLine("");
        Console.WriteLine("If you wish it to run it in active mode press 1");
        Console.WriteLine("");
        Console.WriteLine("If you wish it to run it in dry mode press 2");
        Console.WriteLine("");
        Console.WriteLine("Press any key to continue..");
        Console.WriteLine("");
        Console.ReadKey();
        Console.Clear();
        int number;
        bool key = false;
        while (true)
        {
            Console.WriteLine("Enter number:");
            Console.WriteLine("");
            string read_input = Console.ReadLine();
            Console.WriteLine("");
            if (int.TryParse(read_input, out int result))
            {
                number = Convert.ToInt32(read_input);
                if(number==1)
                {
                    key = true;
                    Console.WriteLine("Active mode selected..");
                    Console.WriteLine("");
                    Console.WriteLine("Press any key to contunue...");
                    Console.WriteLine("");
                    Console.ReadKey();
                    Console.Clear();
                    break;
                }
                else if(number==2)
                {
                    key = false;
                    Console.WriteLine("Dry mode selected..");
                    Console.WriteLine("");
                    Console.WriteLine("Press any key to contunue...");
                    Console.WriteLine("");
                    Console.ReadKey();
                    Console.Clear();
                    break;
                }
                else
                {
                    Console.WriteLine("wrong numberical input, try again..");
                    Console.WriteLine("");
                    Console.WriteLine("Press any key to contunue...");
                }
            }
            else
            {
                Console.WriteLine("You didn't even enter a number..  try again");
                Console.WriteLine("");
                Console.WriteLine("Press any key to continue");

            }
            Console.WriteLine("");
            Console.ReadKey();
            Console.Clear();
        }


        string forbidden_directory = "C:\\Users\\User\\Desktop\\target_folder";         // as the name implies, this is reserved for the copy process 
        string source_directory;
        string read_command;
        Console.Clear();
        CopyFolder a = new CopyFolder("INVALID", "INVALID");
        while(true)
        {
            Console.WriteLine("Please enter the directory you wish to use. If you wish to close the software, type \"end\".");
            Console.WriteLine("");
            source_directory = Console.ReadLine();
            a.SourceFolder = source_directory;
            a.perform_validation_check();
            Console.Clear();
            if (source_directory == "end")
            {
                break;
            }
            else if(string.IsNullOrEmpty(source_directory))    // just in case the user fucks up somehow
            {
                Console.WriteLine("You didn't write anything/ null value assigned");
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
            }
            else if (source_directory.Replace("\\", "") == forbidden_directory.Replace("\\", ""))    // string result = input.Replace("X", ""); // Removes all 'X' characters
            {
                Console.WriteLine("Folder reserved for organizing files, don't touch! ");
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
            }
            else if (a.ValidSource)
            {
                Console.WriteLine("The folder you entered is a valid folder. The folder you will be using is:  ");
                Console.WriteLine(a.SourceFolder);
                Console.WriteLine("Type \"yes\" if you wish to continue. Type \"no\" if you wish to enter a different source directory  ");
                Console.WriteLine("");
                read_command = Console.ReadLine();
                if (read_command == "yes")
                {
                    Console.WriteLine("We can start the process");
                    a.StartProcess_OrganizeFiles(key);
                    Console.WriteLine("");
                    Console.WriteLine("Press any key to continue");
                    Console.ReadKey();
                    var usbDrive = DriveInfo.GetDrives()
                        .FirstOrDefault(d => d.DriveType == DriveType.Removable && d.IsReady);
                    if (usbDrive != null)      //found USB drive
                    {
                        Console.Clear();
                        Console.Write("USB device detected: ");
                        Console.WriteLine(usbDrive.Name);
                        Console.WriteLine("");
                        Console.WriteLine("Do you wish to copy the generated folder to the USB too? [ \"yes\" / \"no\"  ]");
                        Console.WriteLine("");
                        read_command = Console.ReadLine();
                        Console.Clear();

                        if (read_command == "yes")
                        {
                            Console.WriteLine("Beginning process...");
                            a.SourceFolder = "C:\\Users\\User\\Desktop\\target_folder";
                            int i = 0;
                            for (; i < max_amount_of_files; i++)
                            {
                                if (a.IsDestinationFolderExisting(usbDrive.Name, i)) break;
                            }
                            a.StartProcess_CopyFolder(key, i, usbDrive.Name);
                        }
                        else
                        {
                            Console.WriteLine("Operation cancelled");
                        }
                        Console.WriteLine("");
                        Console.WriteLine("Press any key to coninue..");
                        Console.ReadKey();
                        Console.Clear();
                    }
                    else                // didn't find USB drive
                    {
                        // Not much to do here...
                    }
                }
                else if (read_command == "no")
                {
                    Console.WriteLine("Process aborted, press any key to continue");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine("Invalid input, process aborted, press any key to continue");
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("The source directory you entered is invalid, please try again...");
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
            }
            Console.Clear();
        }
        Console.Clear();
        Console.WriteLine("We are done.");
    }
}

