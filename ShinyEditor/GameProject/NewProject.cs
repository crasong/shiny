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
                    OnPropertyChanged(nameof(ProjectPath));
                }
            }
        }

        private ObservableCollection<ProjectTemplate> _projectTemplates = new ObservableCollection<ProjectTemplate>();
        public ReadOnlyObservableCollection<ProjectTemplate> ProjectTemplates
        {
            get;
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
                RenderTargetBitmap bmp = new RenderTargetBitmap(50, 500, 120, 96, PixelFormats.Pbgra32);
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
