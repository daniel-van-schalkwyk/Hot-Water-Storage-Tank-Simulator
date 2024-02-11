using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FileManagement
{
    /// <summary>
    /// AN interface that is used to wrap useful I/O operations for better testability 
    /// </summary>
    public interface IFileWorker
    {
        /// <summary>
        /// A function used to create a director if not exists
        /// </summary>
        /// <param name="path">Path to the directory that needs to be created</param>
        bool CreateDirectory(string path);

        /// <summary>
        /// Reads the contents of a file located at the provided filepath assuming it contains JSON data
        /// </summary>
        /// <param name="filePath">The file path to the JSON file</param>
        /// <param name="settings">The JsonSerializerSettings to use</param>
        /// <returns>The contents of the JSON file as a JObject</returns>
        public JObject? ReadJson(string filePath, JsonSerializerSettings? settings = null);

        /// <summary>
        /// A wrapped method to encapsulate Renaming functionality from VB library 
        /// </summary>
        /// <param name="oldFilePath">The old file name path</param>
        /// <param name="newFilePath">The new file name</param>
        /// <param name="overwrite">A flag indicating whether the dest file needs to be overwritten if exists</param>
        /// <param name="deleteOldFile">A flag to say whether old file needs to be deleted</param>
        public void RenameFile(string oldFilePath, string newFilePath, bool overwrite = true, bool deleteOldFile = true);
        
        /// <summary>
        /// Gets the size of the file (in bytes)
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The size of the file in bytes</returns>
        public long GetFileLength(string filePath);
        
        /// <summary>
        /// Reads the contents of a file located at the provided filepath assuming it contains JSON data
        /// </summary>
        /// <param name="filePath">The file path to the JSON file</param>
        /// <param name="settings">The JsonSerializerSettings to use</param>
        /// <returns>The contents of the JSON file as a JObject</returns>
        public JObject? ReadJson2(string filePath, JsonSerializerSettings? settings = null);

        /// <summary>
        /// A method that is used to write JSON data to provided file path in a memory optimised manner
        /// </summary>
        /// <param name="filePath">The file path to the JSON file</param>
        /// <param name="jsonObject">The JSON contents needed to be written to file</param>
        /// <param name="settings">The JsonSerializerSettings to use</param>
        /// <exception cref="ArgumentException">Is thrown when filePath is null</exception>
        public void WriteJson(string filePath, JToken jsonObject, JsonSerializerSettings? settings = null);

        /// <summary>
        /// Sets the file attribute of the file at the provided filePath 
        /// </summary>
        /// <param name="filePath">URL to the file</param>
        /// <param name="attribute">Attribute that needs to be assigned to the file</param>
        void SetAttribute(string filePath, FileAttributes attribute);

        /// <summary>
        /// Reads the contents of a file located at the provided filepath 
        /// </summary>
        /// <param name="path">The file path to the file</param>
        /// <returns>The contents of the file</returns>
        string ReadToEnd(string path);

        /// <summary>
        /// Creates the file at the provided path
        /// </summary>
        /// <param name="path">The filepath of the file that needs to be created - needs to include file extension</param>
        void CreateFile(string path);

        /// <summary>
        /// Deletes a specified file
        /// </summary>
        /// <param name="path">File path of the file that needs to be deleted</param>
        void DeleteFile(string path);

        /// <summary>
        /// Copies a file from one filepath to a destination filepath
        /// </summary>
        /// <param name="sourceFilePath">The original file that needs to be copied</param>
        /// <param name="destFilePath">The destination of the copied file</param>
        /// <param name="overwrite">A flag indicating whether the destination file contents can be overwritten (true)</param>
        void CopyFile(string sourceFilePath, string destFilePath, bool overwrite);

        /// <summary>
        /// Gets the information object of a file located at the provided file path
        /// </summary>
        /// <param name="path">File path of the file </param>
        /// <returns>A FileInfo object with all appropriate file information</returns>
        FileInfo GetFileInfo(string path);

        /// <summary>
        /// Writes the provided contents into the specified file path.
        /// </summary>
        /// <param name="path">The file path of the file. If the path does not exist, the file will be created</param>
        /// <param name="contents">Contents that need to be written to the file</param>
        /// <param name="append">A flag indicating whether the file needs to be overwritten (false), or appended (true)</param>
        void Write(string? path, string? contents, bool append = false);

        /// <summary>
        /// Reads all of the contents of the file 
        /// </summary>
        /// <param name="filePath">URL of the file</param>
        /// <returns>The contents of the file</returns>
        string ReadAllText(string filePath);

        /// <summary>
        /// Reads the contents of a file separated into chunks that are separated by a newline character ("\n")
        /// </summary>
        /// <param name="filePath">The filepath of the file </param>
        /// <returns>A string array of the file data separated by a newline character ("\n")</returns>
        string[] ReadAllLines(string filePath);

        /// <summary>
        /// Checks if the provided file path exists in the system directory
        /// </summary>
        /// <param name="filePath">The filepath of the file</param>
        /// <returns>A flag indicating the existence of the file</returns>
        bool Exists(string? filePath);

        /// <summary>
        /// Extracts the file name from the provided path
        /// </summary>
        /// <param name="filepath">The filepath of the file</param>
        /// <returns>The file name extracted from the path</returns>
        string GetFileNameFromPath(string filepath);

        /// <summary>
        /// Writes all data to the provided path
        /// </summary>
        /// <param name="filePath">The filepath of the file</param>
        /// <param name="data">Contents that need to be written to the file</param>
        void WriteAllText(string filePath, string data);

        /// <summary>
        /// Checks to see if a directory exists
        /// </summary>
        /// <param name="directoryPath">Directory that needs to be checked</param>
        /// <returns>A flag indicating if the directory does exist</returns>
        bool DirectoryExists(string? directoryPath);

        /// <summary>
        /// Gets the list of files from a directory 
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <returns>A string array of files within the specified directory</returns>
        /// <exception cref="ArgumentNullException">Is thrown when provided path is null</exception>
        /// <exception cref="DirectoryNotFoundException">Is thrown when directory cannot be found</exception>
        string[] GetFilesFromDirectory(string? directoryPath);

        /// <summary>
        /// A file path validator method
        /// </summary>
        /// <param name="filePath">The file path the the file</param>
        /// <param name="extension">The extension type that the validator needs to check for</param>
        /// <returns>A flag indicating if the provided file path is valid</returns>
        /// <exception cref="FileNotFoundException">Is thrown when provided file can not be found</exception>
        bool ValidateFilePathParameter(string filePath, string extension);
    }
}