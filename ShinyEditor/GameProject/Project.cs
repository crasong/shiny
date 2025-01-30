using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ShinyEditor.GameProject
{
    [DataContract(Name ="Game")]
    public class Project : ViewModelBase
    {
        public static string Extension { get; } = ".shiny";
        [DataMember]
        public string Name { get; private set; }
        [DataMember]
        public string FolderPath { get; private set; }
        public string FullPath => $"{FolderPath}{Name}{Extension}";

        [DataMember(Name ="Scenes")]
        private ObservableCollection<Scene> _scenes = new ObservableCollection<Scene>();
        public ReadOnlyObservableCollection<Scene> Scenes { get; }

        public Project(string name, string folderPath)
        {
            Name = name;
            FolderPath = folderPath;

            _scenes.Add(new Scene(this, "Default Scene"));
        }
    }
}
