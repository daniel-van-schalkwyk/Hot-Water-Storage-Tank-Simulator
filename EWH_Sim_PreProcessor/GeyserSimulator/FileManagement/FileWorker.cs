using FileManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;

namespace GeyserSimulator.FileManagement
{
    public class FileWorker : IFileWorker
    {
        /// <summary>
        /// A logging object that is used to report logs to the console and logbook
        /// </summary>
        private readonly ILogger _logger;

        // Constructor with logger
        public FileWorker(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public FileWorker()
        {
            _logger = Logger.None;
        }

        /// <summary>
        /// A function used to create a director if not exists
        /// </summary>
        /// <param name="path">Path to the directory that needs to be created</param>
        public bool CreateDirectory(string path)
        {
            // Check if the directory exists - if not, create it
            try
            {
                if (!Directory.Exists(path) || !File.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                else
                {
                    _logger.Warning("Directory path already exists");
                }
            }
            catch (IOException e)
            {
                _logger.Error("IOException occured: {Ex}", e.Message);
                return false;
            }
            catch (Exception e)
            {
                _logger.Error("Exception occured: {Ex}", e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the size of the file (in bytes)
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The size of the file in bytes</returns>
        public long GetFileLength(string filePath)
        {
            return new FileInfo(filePath).Length;
        }
        
        /// <summary>
        /// Sets the file attribute of the file at the provided filePath 
        /// </summary>
        /// <param name="filePath">URL to the file</param>
        /// <param name="attribute">Attribute that needs to be assigned to the file</param>
        public void SetAttribute(string filePath, FileAttributes attribute)
        {
            // Check if file exists
            if (Exists(filePath))
            {
                File.SetAttributes(filePath, attribute);
            }
            else
            {
                _logger.Warning("Attribute for {FilePath} could not be set: Filepath does not exist", filePath);
            }
        }

        /// <summary>
        /// A wrapped method to encapsulate Renaming functionality from VB library 
        /// </summary>
        /// <param name="oldFilePath">The old file name path</param>
        /// <param name="newFilePath">The new file name</param>
        /// <param name="overwrite">A flag indicating whether the dest file needs to be overwritten if exists</param>
        /// <param name="deleteOldFile">A flag to say whether old file needs to be deleted</param>
        public void RenameFile(string oldFilePath, string newFilePath, bool overwrite = true, bool deleteOldFile = true)
        {
            try
            {
                File.Move(oldFilePath, newFilePath, overwrite);
                if (deleteOldFile)
                {
                    // Delete the original
                    DeleteFile(oldFilePath);
                }
            }
            catch (Exception e)
            {
                _logger.Error("An exception occurred: {Message}", e.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Reads the contents of a file located at the provided filepath assuming it contains JSON data
        /// </summary>
        /// <param name="filePath">The file path to the JSON file</param>
        /// <param name="settings">The JsonSerializerSettings to use</param>
        /// <returns>The contents of the JSON file as a JObject</returns>
        public JObject? ReadJson(string filePath, JsonSerializerSettings? settings = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }
            try
            {
                // Generate the serialiser settings
                JsonSerializer serializer = settings != null
                    ? JsonSerializer.Create(settings)
                    : JsonSerializer.CreateDefault();
                
                using (StreamReader sr = new(filePath))
                {
                    using (JsonTextReader reader = new(sr))
                    {
                        // Use JToken.Load for streaming parsing without fully loading into memory
                        JToken token = JToken.Load(reader);
                        
                        // Close the file
                        sr.Close();

                        // Convert JToken to JObject with custom serialization settings
                        return token.ToObject<JObject>(serializer);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                throw new JsonSerializationException("Error reading and deserializing JSON", ex);
            }
        }

        /// <summary>
        /// Reads the contents of a file located at the provided filepath assuming it contains JSON data
        /// </summary>
        /// <param name="filePath">The file path to the JSON file</param>
        /// <param name="settings">The JsonSerializerSettings to use</param>
        /// <returns>The contents of the JSON file as a JObject</returns>
        public JObject? ReadJson2(string filePath, JsonSerializerSettings? settings = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }
            try
            {
                // Generate the serialiser settings
                JsonSerializer serializer = settings != null
                    ? JsonSerializer.Create(settings)
                    : JsonSerializer.CreateDefault();
                using (StreamReader sr = new(filePath))
                {
                    using (JsonTextReader reader = new(sr))
                    {
                        return serializer.Deserialize<JObject>(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                _logger.Fatal("JSON content of {File} could not be ingested: Exception occurred: {Message}", filePath, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// A method that is used to write JSON data to provided file path in a memory optimised manner
        /// </summary>
        /// <param name="filePath">The file path to the JSON file</param>
        /// <param name="jsonObject">The JSON contents needed to be written to file</param>
        /// <param name="settings">The JsonSerializerSettings to use</param>
        /// <exception cref="ArgumentException">Is thrown when filePath is null</exception>
        public void WriteJson(string filePath, JToken jsonObject, JsonSerializerSettings? settings = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            try
            {
                using (StreamWriter sw = new(filePath))
                {
                    using (JsonTextWriter writer = new(sw))
                    {
                        JsonSerializer serializer = settings != null
                            ? JsonSerializer.Create(settings)
                            : JsonSerializer.CreateDefault();
                        serializer.Serialize(writer, jsonObject);

                        // Write the end of the line
                        sw.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                _logger.Fatal("JSON contents could not be written to {File}: Exception occurred: {Message}", filePath,
                    ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Reads the contents of a file located at the provided filepath 
        /// </summary>
        /// <param name="path">The file path to the file</param>
        /// <returns>The contents of the file</returns>
        public string ReadToEnd(string path)
        {
            string outString;
            using (StreamReader sR = new(path))
            { 
                outString = sR.ReadToEnd();
                sR.Close();
                sR.Close();
                sR.Dispose();
            }
            
            return outString;
        }

        /// <summary>
        /// Creates the file at the provided path
        /// </summary>
        /// <param name="path">The filepath of the file that needs to be created - needs to include file extension</param>
        public void CreateFile(string path)
        {
            // Check if the directory exists - if not, create it
            try
            {
                if (!DirectoryExists(Path.GetDirectoryName(path)))
                {
                    _logger.Warning("Warning: Directory of {Path}, does not exist - Creating it...", path);
                    CreateDirectory(path);
                }
            }
            catch (IOException e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }
            catch (Exception e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }

            // Try creating file and close it
            try
            {
                File.Create(path).Close();
            }
            catch (Exception e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Deletes a specified file
        /// </summary>
        /// <param name="path">File path of the file that needs to be deleted</param>
        public void DeleteFile(string path)
        {
            try
            {
                // Delete file
                File.Delete(path);
                _logger.Information("File removed: {File}", path);
            }
            catch (Exception e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Copies a file from one filepath to a destination filepath
        /// </summary>
        /// <param name="sourceFilePath">The original file that needs to be copied</param>
        /// <param name="destFilePath">The destination of the copied file</param>
        /// <param name="overwrite">A flag indicating whether the destination file contents can be overwritten (true)</param>
        public void CopyFile(string sourceFilePath, string destFilePath, bool overwrite)
        {
            try
            {
                // Copy the file
                File.Copy(sourceFilePath, destFilePath, overwrite);
                _logger.Information("File copied from {File} to {FileDest}", sourceFilePath, destFilePath);
            }
            catch (Exception e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the information object of a file located at the provided file path
        /// </summary>
        /// <param name="path">File path of the file </param>
        /// <returns>A FileInfo object with all appropriate file information</returns>
        public FileInfo GetFileInfo(string path)
        {
            try
            {
                // Get file information
                return new FileInfo(path);
            }
            catch (Exception e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }

        }

        /// <summary>
        /// Writes the provided contents into the specified file path.
        /// </summary>
        /// <param name="path">The file path of the file. If the path does not exist, the file will be created</param>
        /// <param name="contents">Contents that need to be written to the file</param>
        /// <param name="append">A flag indicating whether the file needs to be overwritten (false), or appended (true)</param>
        public void Write(string? path, string? contents, bool append = false)
        {
            if (path is null || contents is null)
            {
                _logger.Error("Error: filepath and/or contents is null when attempting to write to file\n" +
                              "path provided => {Path}\n", path);
                return;
            }
            // Create the file if it does not exist
            if (!Exists(path))
            {
                CreateFile(path);
            }

            // Write to the file
            try
            {
                using (StreamWriter sw = new(path, append))
                {
                    sw.Write(contents);
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
            }
            catch (UnauthorizedAccessException e)
            {
                _logger.Error("Unauthorized access to file or directory : {Ex}", e.Message);
            }
            catch (FileNotFoundException e)
            {
                _logger.Error("File not found: Path provided = {Path} : {Ex}", path, e.Message);
            }
            catch (ArgumentException e)
            {
                _logger.Error("Path not valid or not supported: Path provided = {Path} : {Ex}", path, e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                _logger.Error("Directory not found : Path provided = {Path} : {Ex}", path, e.Message);
            }
            catch (IOException e)
            {
                _logger.Error("An unknown IO exception occured : Path provided = {Path} : {Ex}", path, e.Message);
            }
            catch (Exception e)
            {
                _logger.Error("An unknown exception occured : Path provided = {Path} : {Ex}", path, e.Message);
            }
        }

        /// <summary>
        /// Reads all of the contents of the file 
        /// </summary>
        /// <param name="filePath">URL of the file</param>
        /// <returns>The contents of the file</returns>
        public string ReadAllText(string filePath)
        {
            string outString;
            try
            {
                using (StreamReader sR = new(filePath))
                {
                    outString = sR.ReadToEnd();
                    sR.Close();
                    sR.Dispose();
                }
                
            }
            catch (Exception e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }
            return outString;
        }

        /// <summary>
        /// Reads the contents of a file separated into chunks that are separated by a newline character ("\n")
        /// </summary>
        /// <param name="filePath">The filepath of the file </param>
        /// <returns>A string array of the file data separated by a newline character ("\n")</returns>
        public string[] ReadAllLines(string filePath)
        {
            string[] outString;
            using (StreamReader reader = new(filePath))
            {
                outString = reader.ReadToEnd().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                reader.Close();
                reader.Dispose();
            }
            return outString;
        }

        /// <summary>
        /// Checks if the provided file path exists in the system directory
        /// </summary>
        /// <param name="filePath">The filepath of the file</param>
        /// <returns>A flag indicating the existence of the file</returns>
        public bool Exists(string? filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// Extracts the file name from the provided path
        /// </summary>
        /// <param name="filepath">The filepath of the file</param>
        /// <returns>The file name extracted from the path</returns>
        public string GetFileNameFromPath(string filepath)
        {
            try
            {
                return Path.GetFileName(filepath);
            }
            catch (Exception e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Writes all data to the provided path
        /// </summary>
        /// <param name="filePath">The filepath of the file</param>
        /// <param name="data">Contents that need to be written to the file</param>
        public void WriteAllText(string filePath, string data)
        {
            try
            {
                using (StreamWriter writer = new(filePath, false))
                {
                    writer.Write(data);
                }
            }
            catch (Exception e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }
        }


        /// <summary>
        /// Checks to see if a directory exists
        /// </summary>
        /// <param name="directoryPath">Directory that needs to be checked</param>
        /// <returns>A flag indicating if the directory does exist</returns>
        public bool DirectoryExists(string? directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        /// <summary>
        /// Gets the list of files from a directory 
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <returns>A string array of files within the specified directory</returns>
        /// <exception cref="ArgumentNullException">Is thrown when provided path is null</exception>
        /// <exception cref="DirectoryNotFoundException">Is thrown when directory cannot be found</exception>
        public string[] GetFilesFromDirectory(string? directoryPath)
        {
            // Check if string is null
            if (directoryPath == null)
            {
                throw new ArgumentNullException(directoryPath, "Directory path not provided");
            }

            // Check if directory exists
            if (!DirectoryExists(directoryPath))
            {
                throw new DirectoryNotFoundException();
            }

            // Get the files within the directory
            try
            {
                return Directory.GetFiles(directoryPath);
            }
            catch (Exception e)
            {
                _logger.Error("{Ex}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// A file path validator method
        /// </summary>
        /// <param name="filePath">The file path the the file</param>
        /// <param name="extension">The extension type that the validator needs to check for</param>
        /// <returns>A flag indicating if the provided file path is valid</returns>
        /// <exception cref="FileNotFoundException">Is thrown when provided file can not be found</exception>
        public bool ValidateFilePathParameter(string filePath, string extension)
        {
            // Check if provided file path is valid
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.Error("The file path is invalid");
                return false;
            }

            // If extension is missing the period, add it
            if (!extension.StartsWith('.'))
            {
                extension = '.' + extension;
            }

            // If filepath does not include extension, add it
            if (!filePath.EndsWith(extension, StringComparison.InvariantCulture))
            {
                filePath += extension;
            }

            // if file does not exist, throw exception
            if (!Exists(filePath))
            {
                throw new FileNotFoundException("The provided JSON file path does not exist", filePath);
            }

            return true;
        }
    }
}