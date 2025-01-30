using ShinyEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShinyEditor.GameProject
{
    [DataContract]
    public class ProjectTemplate
    {
        [DataMember]
        public string ProjectType { get; set; }
        [DataMember]
        public string ProjectFile { get; set; }
        [DataMember]
        public List<string> Folders { get; set; }

        public byte[] Icon { get; set; }
        public byte[] Screenshot { get; set; }
        public string IconFilePath { get; set; }
        public string ScreenshotFilePath { get; set; }
        public string ProjectFilePath { get; set; }
    }

    class NewProject : ViewModelBase
    {
        // TODO: Get the path from the installation location
        private readonly string _templatePath = @"..\..\ShinyEditor\ProjectTemplates\";

        private string _projectName = "NewProject";
        public string ProjectName 
        {
            get => _projectName;
            set 
            {
                if (_projectName != value)
                {
                    _projectName = value;
                    ValidateProjectPath();
                    OnPropertyChanged(nameof(ProjectName));
                }
            }
        }

        private string _projectPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\ShinyEditor\Projects\";
        public string ProjectPath
        {
            get => _projectPath;
            set
            {
                if (_projectPath != value)
                {
                    _projectPath = value;
                    ValidateProjectPath();
                    OnPropertyChanged(nameof(ProjectPath));
                }
            }
        }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (_isValid != value)
                {
                    _isValid = value;
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        private string _errorMsg;
        public string ErrorMsg
        {
            get => _errorMsg;
            set
            {
                if (_errorMsg != value)
                {
                    _errorMsg = value;
                    OnPropertyChanged(nameof(ErrorMsg));
                }
            }
        }

        private ObservableCollection<ProjectTemplate> _projectTemplates = new ObservableCollection<ProjectTemplate>();
        public ReadOnlyObservableCollection<ProjectTemplate> ProjectTemplates
        {
            get;
        }

        private bool ValidateProjectPath()
        {
            var path = ProjectPath;
            if (!Path.EndsInDirectorySeparator(path))
            {
                path.Append(Path.DirectorySeparatorChar);
            }
            path += $@"{ProjectName}\";

            IsValid = false;
            if (string.IsNullOrWhiteSpace(ProjectName.Trim()))
            {
                ErrorMsg = "Type in a project name.";
            }
            else if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                ErrorMsg = "Invalid character(s) used in project name.";
            }
            else if (string.IsNullOrWhiteSpace(ProjectPath.Trim()))
            {
                ErrorMsg = "Select a valid project folder.";
            }
            else if (ProjectPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                ErrorMsg = "Invalid character(s) used in project path";
            }
            else if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
            {
                ErrorMsg = "Selected project folder already exists and is not empty";
            }
            else
            {
                ErrorMsg = string.Empty;
                IsValid = true;
            }
            return IsValid;
        }

        public string CreateProject(ProjectTemplate template)
        {
            ValidateProjectPath();
            if(!IsValid)
            {
                return string.Empty;
            }

            if (!Path.EndsInDirectorySeparator(ProjectPath))
            {
                ProjectPath.Append(Path.DirectorySeparatorChar);
            }
            var path = $@"{ProjectPath}{ProjectName}\";

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                foreach (var folder in template.Folders)
                {
                    var folderPath = Path.Combine(Path.GetDirectoryName(path), folder);
                    Directory.CreateDirectory(Path.GetFullPath(folderPath));
                }
                var dirInfo = new DirectoryInfo(path + @".Shiny\");
                dirInfo.Attributes |= FileAttributes.Hidden;
                File.Copy(template.IconFilePath, Path.GetFullPath(Path.Combine(dirInfo.FullName, "Icon.png")));
                File.Copy(template.ScreenshotFilePath, Path.GetFullPath(Path.Combine(dirInfo.FullName, "Screenshot.png")));

                var projectXml = File.ReadAllText(template.ProjectFilePath);
                projectXml = string.Format(projectXml, ProjectName, path);
                var projectPath = Path.GetFullPath(Path.Combine(path, $"{ProjectName}{Project.Extension}"));
                File.WriteAllText(projectPath, projectXml);

                return path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // TODO: Log Error
                return string.Empty;
            }
        }

        public NewProject()
        {
            ProjectTemplates = new ReadOnlyObservableCollection<ProjectTemplate>(_projectTemplates);
            try
            {
                GenerateEmptyProject();
                var templatesFiles = Directory.GetFiles(_templatePath, "template.xml", SearchOption.AllDirectories);
                Debug.Assert(templatesFiles.Any());
                foreach (var file in templatesFiles)
                {
                    var template = Serializer.FromFileXML<ProjectTemplate>(file);
                    template.IconFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), "Icon.png"));
                    template.Icon = File.ReadAllBytes(template.IconFilePath); // How to make this Async compatible? Is it worth it?
                    template.ScreenshotFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), "Screenshot.png"));
                    template.Screenshot = File.ReadAllBytes(template.ScreenshotFilePath); // How to make this Async compatible? Is it worth it?
                    template.ProjectFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), template.ProjectFile));
                    _projectTemplates.Add(template);
                }
                ValidateProjectPath();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // TODO: Log Error
            }
        }

        public void GenerateEmptyProject()
        {
            var emptyPath = Path.Combine(_templatePath, "EmptyProject");
            var templatePath = $@"{emptyPath}\template.xml";
            // 1. Create Temple XML
            var template = new ProjectTemplate()
            {
                ProjectType = "Empty Project",
                ProjectFile = "project.shiny",
                Folders = new List<string> { ".shiny", "Content", "GameCode" }
            };
            Serializer.ToFileXML(template, templatePath);

            var project = new Project(@"{0}", @"{1}");
            var projectPath = Path.GetFullPath(Path.Combine(emptyPath, template.ProjectFile));
            Serializer.ToFileXML(project, projectPath);

            // 2. Create Thumbnail
            {
                // Text to render on the image
                FormattedText text = new FormattedText("Empty",
                    new System.Globalization.CultureInfo("en-US"),
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Verdana"),
                    12,
                    Brushes.DarkSlateBlue,
                    1.25
                 );
                text.TextAlignment = System.Windows.TextAlignment.Left;

                // The Visual to use as the source of the RenderTargetBitmap
                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();
                drawingContext.DrawEllipse(Brushes.Gold, null, new System.Windows.Point(0, 00), 20, 15);
                drawingContext.DrawText(text, new System.Windows.Point(0, 0));
                drawingContext.Close();

                // Render the Visual to the RenderTargetBitmap
                RenderTargetBitmap bmp = new RenderTargetBitmap(50, 50, 120, 96, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                // Encode the thumbnail to a png file
                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(bmp));
                using (Stream stm = File.Create($@"{emptyPath}\Icon.png"))
                {
                    png.Save(stm);
                }
            }


            // 3. Create Screenshot
            {
                // The Visual to use as the source of the RenderTargetBitmap
                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();
                drawingContext.DrawRectangle(Brushes.AntiqueWhite, null, new System.Windows.Rect(0, 0, 400, 250));
                drawingContext.DrawEllipse(Brushes.DarkOliveGreen, null, new System.Windows.Point(0, 0), 100, 75);
                drawingContext.DrawEllipse(Brushes.DarkMagenta, null, new System.Windows.Point(200, 125), 50, 50);
                drawingContext.Close();

                // Render the Visual to the RenderTargetBitmap
                RenderTargetBitmap bmp = new RenderTargetBitmap(400, 250, 120, 96, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                // Encode the thumbnail to a png file
                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(bmp));
                using (Stream stm = File.Create($@"{emptyPath}\Screenshot.png"))
                {
                    png.Save(stm);
                }
            }
        }
    }
}
