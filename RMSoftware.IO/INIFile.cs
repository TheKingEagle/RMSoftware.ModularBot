using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
namespace RMSoftware.IO
{
    /// <summary>
    /// Provides a configuration system that reads and writes in the INI File language.
    /// Supports reading from internet resources.
    /// </summary>
    public class INIFile
    {
        #region internal
        string FilePath;
        List<string> lines = new List<string>();
        bool busy = false;
        bool _GeneratedNewFile = false;
        bool FromInternet = false;
        bool Streamed = false;
        /// <summary>
        /// Returns the local filename and location.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        /// <returns></returns>
        public string GetFileName()
        {
            if (FromInternet)
            {
                throw new InvalidOperationException("Attempted to return a local file while using an internet resource.");
            }
            return FilePath;
        }
        #endregion

        #region public
        public List<INICategory> Categories = new List<INICategory>();
        /// <summary>
        /// Returns true if a new file had to be created.
        /// </summary>
        public bool GeneratedNewFile { get { return _GeneratedNewFile; } }
        #endregion

        public INIFile(string pathOrURL)
        {
            _GeneratedNewFile = false;
            FromInternet = (pathOrURL.StartsWith("http://") || pathOrURL.StartsWith("https://"));
            if (!FromInternet)
            {
                FilePath = pathOrURL;
                VerifyFile(pathOrURL);
                readFromFile(pathOrURL);
            }
            if (FromInternet)
            {
                readFromInternet(pathOrURL);
            }

            ProcessLines();
            
        }


        
        public INIFile(byte[] rawData)
        {

            Streamed = true;
            using (MemoryStream dataStream = new MemoryStream(rawData))
            {
                using (StreamReader sr = new StreamReader(dataStream))
                {
                    string line = "";
                    while (true)
                    {
                        line = sr.ReadLine();
                        if (line != null)
                        {
                            if (!line.StartsWith("#") && !line.StartsWith(";"))
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    lines.Add(line);
                                }
                            }
                        }
                        if (sr.EndOfStream)
                        {
                            break;
                        }
                    }
                }

            }
            ProcessLines();

        }

        #region Public Methods
        /// <summary>
        /// Creates a new INI entry in an existing category. If the category doesn't exist, an argument exception is thrown.
        /// If the entry exists in that category, an argument exception is thrown.
        /// </summary>
        /// <param name="category">The name of the category to place entry (Enter it without the brackets '[]' please.)</param>
        /// <param name="entry">The name of the new entry</param>
        /// <param name="entryValue">The value for the entry.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void CreateEntry(string category, string entry, object entryValue)
        {
            if (FromInternet || Streamed)
            {
                throw (new InvalidOperationException("You Cannot modify stream"));
            }
            if (string.IsNullOrWhiteSpace(category))
            {
                throw (new ArgumentNullException("The category must be contain at least one non-whitespace character."));
            }
            if (string.IsNullOrWhiteSpace(entry))
            {
                throw (new ArgumentNullException("The entry name must be contain at least one non-whitespace character."));
            }
            INICategory cat = GetCategoryByName(category);
            if (cat == null)
            {
                throw (new ArgumentException("The category doesn't exist. Please check your spelling and casing."));
            }
            foreach (INIEntry item in cat.Entries)
            {
                if (item.Name == entry)
                {
                    throw (new ArgumentException("The entry you specified already exists."));
                }
            }
            cat.Entries.Add(new INIEntry(entry, entryValue.ToString()));

        }

        /// <summary>
        /// Creates a new INI Category
        /// </summary>
        /// <param name="category">The name of the new category. (Enter it without the brackets '[]' please.)</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void CreateCategory(string category)
        {
            if (FromInternet || Streamed)
            {
                throw (new InvalidOperationException("You Cannot modify stream"));
            }
            if (string.IsNullOrWhiteSpace(category))
            {
                throw (new ArgumentNullException("The category must be contain at least one non-whitespace character."));
            }
            foreach (INICategory item in Categories)
            {
                if (item.Name == category)
                {
                    throw (new ArgumentException("That category already exists"));
                }
            }
            Categories.Add(new INICategory(category));
        }

        /// <summary>
        /// Deletes INI entry in an existing category. If the category doesn't exist, an argument exception is thrown.
        /// If the entry Doesn't exists in that category, an argument exception is thrown.
        /// </summary>
        /// <param name="category">The name of the category to remove entry from (Enter it without the brackets '[]' please.)</param>
        /// <param name="entry">The name of the entry</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void DeleteEntry(string category, string entry)
        {
            if (FromInternet || Streamed)
            {
                throw (new InvalidOperationException("You Cannot modify stream"));
            }
            if (string.IsNullOrWhiteSpace(category))
            {
                throw (new ArgumentNullException("The category must be contain at least one non-whitespace character."));
            }
            if (string.IsNullOrWhiteSpace(entry))
            {
                throw (new ArgumentNullException("The entry name must be contain at least one non-whitespace character."));
            }
            INICategory cat = GetCategoryByName(category);
            if (cat == null)
            {
                throw (new ArgumentException("The category doesn't exist. Please check your spelling and casing."));
            }
            cat.Entries.Remove(cat.GetEntryByName(entry));

        }

        /// <summary>
        /// Deletes an INI Category
        /// </summary>
        /// <param name="category">The name of the category to delete. (Enter it without the brackets '[]' please.)</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void DeleteCategory(string category)
        {
            if (FromInternet || Streamed)
            {
                throw (new InvalidOperationException("You Cannot modify stream"));
            }
            if (string.IsNullOrWhiteSpace(category))
            {
                throw (new ArgumentNullException("The category must be contain at least one non-whitespace character."));
            }
            Categories.Remove(GetCategoryByName(category));
        }

        /// <summary>
        /// Writes the configuration to disk.
        /// </summary>
        public void SaveConfiguration()
        {
            if (FromInternet || Streamed)
            {
                throw (new InvalidOperationException("Internet or streamed configurations cannot be saved. Read Only."));
            }
            if (busy)
            {
                return;
            }
            busy = true;
            VerifyFile(FilePath);
            int index = 0;
            using (FileStream fs = new FileStream(FilePath,FileMode.Truncate,FileAccess.Write))
            {
                using(StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("#File Generated by the RMSoftware.IO Library.");
                    sw.WriteLine("#Creation Timestamp: {0}", DateTime.Now.ToString());
                    sw.WriteLine();
                    foreach (INICategory item in Categories)
                    {
                        if (index > 0)
                        {
                            sw.WriteLine();
                        }
                        sw.WriteLine(item.ToString());
                        index++;
                        foreach (INIEntry entry in item.Entries)
                        {
                            sw.WriteLine(entry.ToString());
                        }
                    }
                }
            }
            busy = false;
        }


        /// <summary>
        /// Checks for the existence of a specified category. if none is found, returns false.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public bool CheckForCategory(string category)
        {
            if (GetCategoryByName(category) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        public INICategory GetCategoryByName(string name)
        {
            foreach (INICategory item in Categories)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }
            return null; //instead of errors.
            //throw (new ArgumentException("The name provided did not match any categories available."));
        }
        #endregion

        #region Private Methods
        private void VerifyFile(string path)
        {
            string pathfull = Path.GetFullPath(path);
            if (!Directory.Exists(Path.GetDirectoryName(pathfull))) //The directory does not exist. Impossible for the file to exist.
            {
                Console.WriteLine("DIRECTORY NOT FOUND. " + Path.GetDirectoryName(path));

                Directory.CreateDirectory(Path.GetDirectoryName(pathfull));

                Console.WriteLine("FILE NOT FOUND. " + Path.GetFileName(path));
                FileStream fs = File.Create(pathfull);
                fs.Close();
                _GeneratedNewFile = true;
            }
            else//The directory exists
            {
                Console.WriteLine("DIRECTORY FOUND. " + Path.GetDirectoryName(path));
                if (!File.Exists(pathfull)) //and the file does not exist.
                {
                    Console.WriteLine("FILE NOT FOUND. " + Path.GetFileName(path));
                    FileStream fs = File.Create(pathfull);
                    fs.Close();
                    _GeneratedNewFile = true;
                }
                else
                {
                    Console.WriteLine("FILE FOUND. " + Path.GetFileName(path));
                }
            }
        }
        void readFromFile(string fromPath)
        {
            
            using (FileStream fs = new FileStream(fromPath, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    string line = "";
                    while (true)
                    {
                        line = sr.ReadLine();
                        if (line != null)
                        {
                            if (!line.StartsWith("#") && !line.StartsWith(";"))
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    lines.Add(line);
                                }
                            }
                        }
                        if (sr.EndOfStream)
                        {
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reads from internet resource
        /// </summary>
        /// <exception cref="WebException">Thrown if we can't connect.</exception>
        /// <param name="URL"></param>
        void readFromInternet(string URL)
        {
            try
            {
                WebRequest iniRequest = WebRequest.Create(URL);
                using (Stream Response = iniRequest.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(Response))
                    {
                        string line = "";
                        while (true)
                        {
                            line = sr.ReadLine();
                            if (line != null)
                            {
                                if (!line.StartsWith("#"))
                                {
                                    if (!string.IsNullOrWhiteSpace(line))
                                    {
                                        lines.Add(line);
                                    }
                                }
                            }
                            if (sr.EndOfStream)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (WebException exc)
            {
                
                throw exc;
            }
            
        }

        void ProcessLines()
        {
            int catIndex = -1;
            if (lines.Count == 0)
            {
                return;
            }

            foreach (string line in lines)
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string name = line.Replace("[", "").Replace("]", "");
                    Categories.Add(new INICategory(name));
                    catIndex++;
                }
                else
                {
                    string[] splittedString = line.Split('=');
                    string name = splittedString[0];
                    string v = splittedString[1];
                    if (splittedString.Length > 2)
                    {
                        for (int i = 2; i < splittedString.Length; i++)
                        {
                            v += "=" + splittedString[i];
                        }
                    }
                    Categories[catIndex].Entries.Add(new INIEntry(name, v));
                }
            }
        }
        #endregion

    }
    /// <summary>
    /// Category within INI file. appears as: [Category1]
    /// </summary>
    public class INICategory
    {
        public string Name {get;set;}
        public List<INIEntry> Entries = new List<INIEntry>();
        /// <summary>
        /// Returns how this would look inside an INI file.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "["+Name+"]";
        }
        /// <summary>
        /// Creates a new category with several objects.
        /// </summary>
        /// <param name="name">The name of the category</param>
        /// <param name="entries">A collection of entries in a category. </param>
        public INICategory(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Checks for the existence of an entry. if not found, returns false.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool CheckForEntry(string entry)
        {
            if (GetEntryByName(entry) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public INIEntry GetEntryByName(string name)
        {
            foreach (INIEntry item in Entries)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }
            //throw (new ArgumentException("The name provided did not match any entries available."));
            return null;//rather than errors;
        }
 
    }
    /// <summary>
    /// Entry inside a category appears as Entry1=hello
    /// </summary>
    public class INIEntry
    {
        /// <summary>
        /// Get and set the name for the entry
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Get and set the value of the entry.
        /// </summary>
        string Value {get;set;}
        /// <summary>
        /// Returns how this would look inside an INI file.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name + "=" + Value;
        }

        public int GetAsInteger()
        {
            try
            {
                return int.Parse(Value);
            }
            catch (Exception)
            {
                
                throw;
            }
        }
        public double GetAsDouble()
        {
            try
            {
                return double.Parse(Value);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public long GetAslong()
        {
            try
            {
                return long.Parse(Value);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public ulong GetAsUlong()
        {
            try
            {
                return ulong.Parse(Value);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public bool GetAsBool()
        {
            try
            {
                return bool.Parse(Value);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public byte GetAsByte()
        {
            try
            {
                return byte.Parse(Value);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public string GetAsString()
        {
            return Value;
        }

        public void SetValue(int value)
        {
            Value = value.ToString();
        }
        public void SetValue(double value)
        {
            Value = value.ToString();
        }
        public void SetValue(long value)
        {
            Value = value.ToString();
        }
        public void SetValue(bool value)
        {
            Value = value.ToString();
        }
        public void SetValue(byte value)
        {
            Value = value.ToString();
        }
        public void SetValue(string value)
        {
            Value = value;
        }
        /// <summary>
        /// Creates an INI Entry.
        /// </summary>
        /// <param name="name">The name for the entry</param>
        /// <param name="value">The value of the entry.</param>
        public INIEntry(string name, string value)
        {
            Name = name; 
            Value = value;
        }
    }
}
